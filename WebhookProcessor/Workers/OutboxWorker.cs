using Microsoft.EntityFrameworkCore;
using WebhookProcessor.Data;
using WebhookProcessor.Models;
using WebhookProcessor.Services;

namespace WebhookProcessor.Workers;

/// <summary>
/// Procesa eventos del outbox uno a la vez.
/// Usa un "soft lock" en DB para que múltiples instancias del servicio
/// no agarren el mismo evento (útil en escenarios de escalado horizontal).
/// </summary>
public class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    private const int MaxRetries   = 3;
    private const int LockMinutes  = 10;   // tiempo máximo que un evento puede estar "en proceso"
    private const int PollSeconds  = 15;   // cuánto esperar si no hay eventos pendientes

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxWorker iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessNextEventAsync(stoppingToken);

                // Si no había nada, espera antes de volver a preguntar
                if (!processed)
                    await Task.Delay(TimeSpan.FromSeconds(PollSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error inesperado en OutboxWorker. Reintentando en 30s.");
                await Task.Delay(30_000, stoppingToken);
            }
        }

        logger.LogInformation("OutboxWorker detenido.");
    }

    private async Task<bool> ProcessNextEventAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var erp = scope.ServiceProvider.GetRequiredService<IErpService>();
        var crm = scope.ServiceProvider.GetRequiredService<ICrmService>();

        // Toma el siguiente evento pendiente y lo "bloquea" atómicamente
        var now = DateTime.UtcNow;
        var evt = await db.OutboxEvents
            .Where(e =>
                e.Status == EventStatus.Pending &&
                (e.LockedUntil == null || e.LockedUntil < now))
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (evt is null) return false;

        // Soft lock: evita que otra instancia tome este mismo evento
        evt.Status      = EventStatus.Processing;
        evt.LockedUntil = now.AddMinutes(LockMinutes);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Procesando evento {Id} | VIN: {Vin} | Intento {Retry}",
            evt.Id, evt.Vin, evt.RetryCount + 1);

        try
        {
            // Llama ERP y CRM en paralelo (si son independientes entre sí)
            await Task.WhenAll(
                erp.CreateContractAsync(evt, ct),
                crm.CreateUserAccountAsync(evt, ct)
            );

            evt.Status      = EventStatus.Done;
            evt.ProcessedAt = DateTime.UtcNow;
            evt.ErrorMessage = null;

            logger.LogInformation("Evento {Id} procesado OK.", evt.Id);
        }
        catch (Exception ex)
        {
            evt.RetryCount++;
            evt.ErrorMessage = ex.Message;
            evt.LockedUntil  = null; // libera el lock para el próximo ciclo

            if (evt.RetryCount >= MaxRetries)
            {
                evt.Status = EventStatus.DeadLetter;
                logger.LogError(
                    ex,
                    "Evento {Id} enviado a dead-letter después de {Max} intentos. VIN: {Vin}",
                    evt.Id, MaxRetries, evt.Vin);
            }
            else
            {
                evt.Status = EventStatus.Pending;
                logger.LogWarning(
                    ex,
                    "Evento {Id} falló (intento {Retry}/{Max}). Se reintentará.",
                    evt.Id, evt.RetryCount, MaxRetries);
            }
        }
        finally
        {
            await db.SaveChangesAsync(CancellationToken.None); // no cancelar el guardado final
        }

        return true;
    }
}
