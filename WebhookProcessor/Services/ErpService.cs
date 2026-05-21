using WebhookProcessor.Models;

namespace WebhookProcessor.Services;

public interface IErpService
{
    /// <summary>Crea el contrato en el ERP. Lanza excepción si falla.</summary>
    Task CreateContractAsync(OutboxEvent evt, CancellationToken ct);
}

public class ErpService(HttpClient http, ILogger<ErpService> logger) : IErpService
{
    public async Task CreateContractAsync(OutboxEvent evt, CancellationToken ct)
    {
        // Reemplaza con la llamada real a tu ERP
        var payload = new
        {
            evt.Vin,
            evt.Placa,
            evt.Dispositivo,
            evt.FechaInicio,
            evt.FechaFinal,
            evt.EventoId,
            evt.Orden
        };

        var response = await http.PostAsJsonAsync("/api/contracts", payload, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Contrato creado en ERP para VIN {Vin}.", evt.Vin);
    }
}
