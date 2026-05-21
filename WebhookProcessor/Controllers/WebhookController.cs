using Microsoft.AspNetCore.Mvc;
using WebhookProcessor.Data;
using WebhookProcessor.Models;

namespace WebhookProcessor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController(AppDbContext db, ILogger<WebhookController> logger) : ControllerBase
{
    /// <summary>
    /// Recibe el JSON con hasta 50 objetos y los encola de inmediato.
    /// No procesa nada aquí — responde 202 en milisegundos.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Receive(
        [FromBody] WebhookPayload payload,
        CancellationToken ct)
    {
        if (payload.Items is not { Count: > 0 })
            return BadRequest("Payload vacío.");

        var events = payload.Items.Select(item => new OutboxEvent
        {
            Vin         = item.Vin,
            Placa       = item.Placa,
            Dispositivo = item.Dispositivo,
            FechaEnvio  = item.FechaEnvio,
            FechaInicio = item.FechaInicio,
            FechaFinal  = item.FechaFinal,
            EventoId    = item.EventoId,
            Evento      = item.Evento,
            Orden       = item.Orden,
            Nombre      = item.Nombre,
            Apellidos   = item.Apellidos,
            Correo      = item.Correo,
            Status      = EventStatus.Pending,
            CreatedAt   = DateTime.UtcNow
        }).ToList();

        // Una sola escritura en lote — operación rápida aunque haya procesos pesados en la DB
        await db.OutboxEvents.AddRangeAsync(events, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Encolados {Count} eventos del webhook.", events.Count);

        return Accepted(new { enqueued = events.Count });
    }
}
