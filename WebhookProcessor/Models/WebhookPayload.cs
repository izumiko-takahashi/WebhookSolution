namespace WebhookProcessor.Models;

public class WebhookPayload
{
    public List<WebhookItem> Items { get; set; } = new();
}

public class WebhookItem
{
    public string Vin { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public string Dispositivo { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFinal { get; set; }
    public string EventoId { get; set; } = string.Empty;
    public string Evento { get; set; } = string.Empty;
    public int Orden { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
}
