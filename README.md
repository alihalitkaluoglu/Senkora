# Senkora — Enterprise Integration Platform

WooCommerce × Logo ERP entegrasyon platformu.

## Hizli Baslangic

### 1. On Kosullar
- .NET 8 SDK
- Node.js 20 LTS
- Docker Desktop
- Git

### 2. Depoyu Klonla
```bash
git clone <repo-url>
cd senkora
```

### 3. Docker Servislerini Baslat
```bash
cd docker
docker compose up -d
cd ..
```

### 4. Environment Dosyasini Olustur
```bash
cp .env.example .env
# .env dosyasini duzenle
```

### 5. API'yi Baslat
```bash
cd src/Senkora.Api
dotnet run
```

### 6. Portali Baslat
```bash
cd portal
npm install
npm run dev
```

## Erisim Adresleri

| Servis | Adres |
|---|---|
| API | https://localhost:5001 |
| Swagger | https://localhost:5001/swagger |
| Hangfire | https://localhost:5001/hangfire |
| Portal | http://localhost:3000 |
| Seq Logs | http://localhost:8080 |
| SQL Server | localhost:1433 |
| Redis | localhost:6379 |

## Mimari

Clean Architecture + CQRS + DDD + Multi-Tenant SaaS

```
Senkora.Domain          -> Entities, ValueObjects, Interfaces
Senkora.Application     -> CQRS Handlers, DTOs, Validators
Senkora.Infrastructure  -> EF Core, Logo REST, WooCommerce, Hangfire
Senkora.Api             -> Controllers, Middleware, SignalR
Senkora.Worker          -> Background Jobs
```
