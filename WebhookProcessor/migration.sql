-- Ejecuta este script en tu SQL Server si no usas EF Migrations
CREATE TABLE OutboxEvents (
    Id           BIGINT IDENTITY(1,1) PRIMARY KEY,
    Vin          NVARCHAR(50)   NOT NULL,
    Placa        NVARCHAR(20)   NOT NULL,
    Dispositivo  NVARCHAR(100)  NOT NULL,
    FechaEnvio   DATETIME2      NOT NULL,
    FechaInicio  DATETIME2      NOT NULL,
    FechaFinal   DATETIME2      NOT NULL,
    EventoId     NVARCHAR(50)   NOT NULL,
    Evento       NVARCHAR(200)  NOT NULL,
    Orden        INT            NOT NULL,
    Nombre       NVARCHAR(100)  NOT NULL,
    Apellidos    NVARCHAR(100)  NOT NULL,
    Correo       NVARCHAR(200)  NOT NULL,

    -- Columnas de control del outbox
    Status       INT            NOT NULL DEFAULT 0,  -- 0 Pending, 1 Processing, 2 Done, 3 Failed, 4 DeadLetter
    RetryCount   INT            NOT NULL DEFAULT 0,
    ErrorMessage NVARCHAR(2000) NULL,
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    ProcessedAt  DATETIME2      NULL,
    LockedUntil  DATETIME2      NULL
);

-- Índice clave para el worker: busca Pending no bloqueados sin escanear toda la tabla
CREATE INDEX IX_OutboxEvents_Status_LockedUntil
    ON OutboxEvents (Status, LockedUntil)
    INCLUDE (CreatedAt);

-- Vista para monitoreo rápido
CREATE VIEW vw_OutboxSummary AS
SELECT
    Status,
    COUNT(*)          AS Total,
    MIN(CreatedAt)    AS MasAntiguo,
    MAX(RetryCount)   AS MaxReintentos
FROM OutboxEvents
GROUP BY Status;
