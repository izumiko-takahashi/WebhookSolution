// FakeErp/Program.cs
// Simula el ERP de contratos. Levanta en http://localhost:5100

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5100");

var app = builder.Build();

var contracts = new List<object>();

// Simula latencia real (~2s) y falla aleatoria 20% del tiempo
app.MapPost("/api/contracts", async (ContractRequest req) =>
{
    await Task.Delay(Random.Shared.Next(1000, 3000)); // 1-3s de latencia

    if (Random.Shared.NextDouble() < 0.2) // 20% de fallo
    {
        Console.WriteLine($"[ERP] ❌ FALLO simulado para VIN {req.Vin}");
        return Results.StatusCode(503);
    }

    var contract = new
    {
        ContractId  = Guid.NewGuid().ToString(),
        req.Vin,
        req.Placa,
        req.FechaInicio,
        req.FechaFinal,
        CreatedAt   = DateTime.UtcNow
    };

    contracts.Add(contract);
    Console.WriteLine($"[ERP] ✅ Contrato creado | VIN: {req.Vin} | Total contratos: {contracts.Count}");

    return Results.Ok(contract);
});

app.MapGet("/api/contracts", () => Results.Ok(contracts));

app.MapDelete("/api/contracts", () =>
{
    contracts.Clear();
    return Results.Ok(new { message = "Contratos borrados." });
});

Console.WriteLine("🏭 Fake ERP corriendo en http://localhost:5100");
app.Run();

record ContractRequest(
    string Vin,
    string Placa,
    string Dispositivo,
    DateTime FechaInicio,
    DateTime FechaFinal,
    string EventoId,
    int Orden);
