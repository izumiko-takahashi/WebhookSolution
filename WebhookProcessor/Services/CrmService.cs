using WebhookProcessor.Models;

namespace WebhookProcessor.Services;

public interface ICrmService
{
    /// <summary>Crea la cuenta de usuario en el CRM / app móvil. Lanza excepción si falla.</summary>
    Task CreateUserAccountAsync(OutboxEvent evt, CancellationToken ct);
}

public class CrmService(HttpClient http, ILogger<CrmService> logger) : ICrmService
{
    public async Task CreateUserAccountAsync(OutboxEvent evt, CancellationToken ct)
    {
        var payload = new
        {
            evt.Nombre,
            evt.Apellidos,
            evt.Correo,
            evt.Vin
        };

        var response = await http.PostAsJsonAsync("/api/users", payload, ct);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("Cuenta creada en CRM para {Correo}.", evt.Correo);
    }
}
