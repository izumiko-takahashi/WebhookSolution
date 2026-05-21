# Fake ERP + CRM — guía de testing

## Puertos

| Servicio          | URL                          |
|-------------------|------------------------------|
| WebhookProcessor  | http://localhost:5000        |
| Fake ERP          | http://localhost:5100        |
| Fake CRM          | http://localhost:5200        |

---

## Levantar los 3 servicios

Abre **3 terminales** distintas:

**Terminal 1 — Fake ERP**
```bash
cd FakeErp
dotnet run
```

**Terminal 2 — Fake CRM**
```bash
cd FakeCrm
dotnet run
```

**Terminal 3 — WebhookProcessor**
```bash
cd WebhookProcessor
# En appsettings.json asegúrate de tener:
# "ErpBaseUrl": "http://localhost:5100"
# "CrmBaseUrl": "http://localhost:5200"
dotnet run
```

---

## Correr el test

```powershell
# Test con 50 objetos (default)
./test-webhook.ps1

# Test con cantidad custom
./test-webhook.ps1 -Count 10
```

El script envía el payload, luego hace polling cada 5 s mostrando cuántos
contratos y cuentas se han creado, hasta que ambos lleguen al total esperado.

---

## Endpoints útiles para inspección manual

```bash
# Ver contratos creados en el ERP
curl http://localhost:5100/api/contracts

# Ver usuarios creados en el CRM
curl http://localhost:5200/api/users

# Limpiar datos para re-correr el test
curl -X DELETE http://localhost:5100/api/contracts
curl -X DELETE http://localhost:5200/api/users
```

---

## Comportamiento de los fakes

| Fake | Latencia simulada | Fallo aleatorio |
|------|-------------------|-----------------|
| ERP  | 1 – 3 s           | 20 % de requests|
| CRM  | 0.5 – 2 s         | 15 % de requests|

Los fallos permiten verificar que el sistema de reintentos del `OutboxWorker`
funciona correctamente — los eventos fallidos deben volver a `Pending` y
procesarse en el siguiente ciclo.
