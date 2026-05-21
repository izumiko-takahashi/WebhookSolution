namespace WebhookProcessor.Models;

public class OutboxEvent
{
    public long Id { get; set; }
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

    public EventStatus Status { get; set; } = EventStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? LockedUntil { get; set; }
}

public enum EventStatus
{
    Pending = 0,
    Processing = 1,
    Done = 2,
    Failed = 3,
    DeadLetter = 4
}
