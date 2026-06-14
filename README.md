# Mini SCADA

Distribuirani sistem za prikupljanje i obradu podataka od senzora temperature.

---

## Preduslovi

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker](https://docs.docker.com/get-docker/)
- `dotnet-ef` alat: `dotnet tool install --global dotnet-ef --version 8.0.0`

---

## Pokretanje projekta

### 1. Kloniraj repozitorijum

```bash
git clone git@github.com:ljubogdan/mini-scada.git
cd mini-scada
```

### 2. Kreiraj konfiguracione fajlove sa tajnama

Ovi fajlovi nisu na GitHub-u

**`src/IngestionService/appsettings.Development.json`**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=miniscada..." na diskordu je
  },
  "AesKey": na diskordu je 
}
```

**`src/SensorClient/appsettings.Development.json`**
```json
{
  "ServerUrl": "http://localhost:5000",
  "AesKey": na diskordu je
}
```

> AES ključ mora biti isti u oba fajla.

### 3. Pokreni sve service (Docker)

```bash
sudo docker compose up --build
```

### 4. Primeni migracije (samo prvi put ili kad se promene modeli)

```bash
  dotnet ef database update \
  --project src/Shared/Shared.csproj \
  --startup-project src/IngestionService/IngestionService.csproj
```

### 5. Pokreni senzor (novi terminal)

```bash
dotnet run --project src/SensorClient/SensorClient.csproj
```

Ili direktno iz foldera sa projektom:

```
  dotnet run -- \
  Sensor:Id=44444444-4444-4444-4444-444444444444 \
  Sensor:Name=Pera
```

Na isti način se u komandnoj liniji mogu proslediti svi ostali parametri senzora.

---

## Demo na 2 mašine

Na mašini klijenta promeni `ServerUrl` u `appsettings.Development.json`:
```json
"ServerUrl": "http://<IP_SERVERA>:5000"
```

---

## Testiranje

### Simulacija replay napada
Pritisni `Enter` u terminalu gde radi SensorClient - šalje istu poruku ponovo. Server treba da vrati `409 Conflict`.

### Blokiranje senzora (30 sekundi)
```bash
curl -X POST http://localhost:5000/api/sensors/00000000-0000-0000-0000-000000000001/block
```

---

## Arhitektura

| Servis | Status | Opis |
|--------|--------|------|
| IngestionService | Završen (Student 1) | Prima podatke od senzora, upisuje u bazu |
| SensorClient | Završen (Student 1) | Simulira senzore, šalje šifrovane poruke |
| ConsensusService | Student 2 | BFT konsenzus svaki minut |
| NotificationService | Završen (Student 3) | SignalR alarmi u realnom vremenu |

---

## Šta je ostalo

**Student 2:**
- `src/ConsensusService/` - implementirati BFT algoritam
- Fault tolerance - praćenje 5 aktivnih senzora, zamena neaktivnih
- Reports API - `/api/reports` za istorijske podatke

**Student 3:**
- Kubernetes manifesti

---

## Bezbednosne mere

- **AES-256** enkripcija svaке poruke
- **RSA-2048** digitalni potpis (senzor potpisuje, server verifikuje)
- **Anti-replay** zaštita - MessageId mora rasti, poruke starije od 30s se odbijaju
- Tajne (lozinke, ključevi) nisu u repozitorijumu
