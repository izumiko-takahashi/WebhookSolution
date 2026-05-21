#!/usr/bin/env pwsh
# test-webhook.ps1
# Envía un batch de 50 objetos al webhook y monitorea el procesamiento.
# Uso: ./test-webhook.ps1
# Uso con tamaño custom: ./test-webhook.ps1 -Count 10

param([int]$Count = 50)

$WebhookUrl  = "http://localhost:5000/api/webhook"
$ErpUrl      = "http://localhost:5100/api/contracts"
$CrmUrl      = "http://localhost:5200/api/users"

function Write-Color($msg, $color = "White") {
    Write-Host $msg -ForegroundColor $color
}

# ── Genera payload ────────────────────────────────────────────────────────────
$items = 1..$Count | ForEach-Object {
    @{
        vin         = "VIN{0:D6}" -f $_
        placa       = "ABC{0:D3}" -f $_
        dispositivo = "GPS-{0:D4}" -f (Get-Random -Max 9999)
        fechaEnvio  = (Get-Date).ToString("o")
        fechaInicio = (Get-Date).AddDays(-1).ToString("o")
        fechaFinal  = (Get-Date).AddDays(30).ToString("o")
        eventoId    = [guid]::NewGuid().ToString()
        evento      = "ACTIVACION"
        orden       = $_
        nombre      = "Usuario$_"
        apellidos   = "Apellido$_"
        correo      = "usuario$_@test.com"
    }
}

$payload = @{ items = $items } | ConvertTo-Json -Depth 5

# ── Envía al webhook ──────────────────────────────────────────────────────────
Write-Color "`n📤 Enviando $Count objetos al webhook..." Cyan
try {
    $resp = Invoke-RestMethod -Uri $WebhookUrl -Method Post `
        -Body $payload -ContentType "application/json"
    Write-Color "✅ Webhook respondió 202 — encolados: $($resp.enqueued)" Green
} catch {
    Write-Color "❌ Error al llamar al webhook: $_" Red
    exit 1
}

# ── Monitorea hasta que no queden pendientes ──────────────────────────────────
Write-Color "`n⏳ Monitoreando procesamiento (Ctrl+C para detener)..." Yellow
$startTime = Get-Date

do {
    Start-Sleep -Seconds 5

    $erp = try { Invoke-RestMethod $ErpUrl } catch { @() }
    $crm = try { Invoke-RestMethod $CrmUrl } catch { @() }

    $elapsed = [math]::Round(((Get-Date) - $startTime).TotalSeconds)
    Write-Color "[$elapsed s] ERP contratos: $($erp.Count)  |  CRM usuarios: $($crm.Count)" White

} while ($erp.Count -lt $Count -or $crm.Count -lt $Count)

$totalSec = [math]::Round(((Get-Date) - $startTime).TotalSeconds)
Write-Color "`n🎉 Todos los eventos procesados en $totalSec segundos." Green

# ── Resumen final ─────────────────────────────────────────────────────────────
Write-Color "`n── Resumen ──────────────────────────────────────────" Cyan
Write-Color "  Contratos ERP : $($erp.Count) / $Count" White
Write-Color "  Cuentas CRM   : $($crm.Count) / $Count" White
Write-Color "─────────────────────────────────────────────────────" Cyan
