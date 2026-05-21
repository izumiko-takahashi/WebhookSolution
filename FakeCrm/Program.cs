// FakeCrm/Program.cs
// Simula el CRM de cuentas de usuario. Levanta en http://localhost:5200

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5200");

var app = builder.Build();

var users = new List<object>();

// Simula latencia real (~1s) y falla aleatoria 15% del tiempo
app.MapPost("/api/users", async (UserRequest req) =>
{
    await Task.Delay(Random.Shared.Next(500, 2000)); // 0.5-2s de latencia

    if (Random.Shared.NextDouble() < 0.15) // 15% de fallo
    {
        Console.WriteLine($"[CRM] ❌ FALLO simulado para {req.Correo}");
        return Results.StatusCode(503);
    }

    if (users.Any(u => ((dynamic)u).Correo == req.Correo))
    {
        Console.WriteLine($"[CRM] ⚠️  Usuario duplicado: {req.Correo}");
        return Results.Conflict(new { error = "El correo ya existe." });
    }

    var user = new
    {
        UserId    = Guid.NewGuid().ToString(),
        req.Nombre,
        req.Apellidos,
        req.Correo,
        req.Vin,
        CreatedAt = DateTime.UtcNow
    };

    users.Add(user);
    Console.WriteLine($"[CRM] ✅ Cuenta creada | {req.Correo} | Total usuarios: {users.Count}");

    return Results.Ok(user);
});

app.MapGet("/api/users", () => Results.Ok(users));

app.MapDelete("/api/users", () =>
{
    users.Clear();
    return Results.Ok(new { message = "Usuarios borrados." });
});

Console.WriteLine("📇 Fake CRM corriendo en http://localhost:5200");
app.Run();

record UserRequest(
    string Nombre,
    string Apellidos,
    string Correo,
    string Vin);
