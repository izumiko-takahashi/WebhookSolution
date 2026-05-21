using Microsoft.EntityFrameworkCore;
using WebhookProcessor.Data;
using WebhookProcessor.Services;
using WebhookProcessor.Workers;

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.CommandTimeout(60)));

// ── Servicios externos (ajusta BaseAddress en appsettings) ───────────────────
builder.Services.AddHttpClient<IErpService, ErpService>(c =>
    c.BaseAddress = new Uri(builder.Configuration["ErpBaseUrl"]!));

builder.Services.AddHttpClient<ICrmService, CrmService>(c =>
    c.BaseAddress = new Uri(builder.Configuration["CrmBaseUrl"]!));

// ── Worker ───────────────────────────────────────────────────────────────────
builder.Services.AddHostedService<OutboxWorker>();

// ── API ──────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplica migraciones automáticamente al arrancar (útil en contenedores)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
