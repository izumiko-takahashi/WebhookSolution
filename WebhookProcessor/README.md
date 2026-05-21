# WebhookProcessor — Outbox Pattern (.NET 8)

## Problema resuelto

El webhook recibía 50 objetos y los procesaba sincrónicamente (5 min c/u = ~250 min de exposición).
Durante las horas pico (3–5 am), otros procesos en SQL Server causaban timeouts y pérdida de datos.

**Solución**: Patrón Transactional Outbox.
- El webhook solo escribe en la DB (operación < 1 s) y responde HTTP 202 de inmediato.
- Un `BackgroundService` procesa cada evento de forma independiente con reintentos automáticos.

## Estructura

```
WebhookProcessor/
├── Controllers/
│   └── WebhookController.cs   # Recibe y encola, nada más
├── Workers/
│   └── OutboxWorker.cs        # Procesa 1 evento a la vez con soft lock
├── Services/
│   ├── ErpService.cs          # Llama al ERP para crear contratos
│   └── CrmService.cs          # Llama al CRM para crear cuentas
├── Models/
│   ├── OutboxEvent.cs         # Entidad con campos de control (Status, RetryCount, LockedUntil)
│   └── WebhookPayload.cs      # DTO del JSON entrante
├── Data/
│   └── AppDbContext.cs        # EF Core con índice optimizado
├── migration.sql              # Script SQL directo (alternativa a EF Migrations)
└── appsettings.json
```

## Setup rápido

### 1. Configurar conexión
Edita `appsettings.json`:
```json
{
  "ConnectionStrings": { "Default": "Server=...;Database=WebhookProcessor;..." },
  "ErpBaseUrl": "https://tu-erp.com",
  "CrmBaseUrl": "https://tu-crm.com"
}
```

### 2. Crear tabla (elige una opción)

**Opción A — EF Migrations:**
```bash
dotnet ef migrations add InitOutbox
dotnet ef database update
```

**Opción B — Script SQL directo:**
```bash
# Ejecuta migration.sql en tu SQL Server
sqlcmd -S localhost -d WebhookProcessor -i migration.sql
```

### 3. Correr
```bash
dotnet run
```

## Flujo de estados de un evento

```
Pending → Processing → Done
                    ↘ Failed (RetryCount < 3) → Pending (reintento)
                    ↘ DeadLetter (RetryCount >= 3)
```

## Monitoreo

```sql
-- Ver resumen por estado
SELECT * FROM vw_OutboxSummary;

-- Ver eventos fallidos
SELECT Id, Vin, Correo, RetryCount, ErrorMessage
FROM OutboxEvents
WHERE Status IN (3, 4)  -- Failed o DeadLetter
ORDER BY CreatedAt DESC;
```

## Consideraciones para producción

- **Reprocesar dead-letters**: cambia `Status = 0` y `RetryCount = 0` en los registros que quieras reintentar.
- **Escalado horizontal**: el `LockedUntil` evita doble procesamiento si corres múltiples instancias.
- **Alertas**: agrega una alerta cuando `Status = 4` (DeadLetter) para revisión manual.
- **Retención**: archiva o borra eventos `Done` con más de N días para no crecer la tabla indefinidamente.
