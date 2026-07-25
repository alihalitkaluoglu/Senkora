# Senkora Kurulum Scripti - Windows PowerShell
Write-Host "=== SENKORA KURULUM BASLADI ===" -ForegroundColor Cyan

# 1. Versiyon kontrolleri
Write-Host "`n[1/6] Versiyon kontrolleri..." -ForegroundColor Yellow
dotnet --version
node --version
docker --version
git --version

# 2. Docker servisleri
Write-Host "`n[2/6] Docker servisleri baslatiliyor..." -ForegroundColor Yellow
Set-Location docker
docker compose up -d
Set-Location ..

# 3. .env dosyasi
Write-Host "`n[3/6] .env dosyasi kontrol ediliyor..." -ForegroundColor Yellow
if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host ".env dosyasi olusturuldu. Lutfen duzenleyin." -ForegroundColor Red
}

# 4. NuGet geri yukleme
Write-Host "`n[4/6] NuGet paketleri yukleniyor..." -ForegroundColor Yellow
dotnet restore Senkora.sln

# 5. Build
Write-Host "`n[5/6] Solution derleniyor..." -ForegroundColor Yellow
dotnet build Senkora.sln --configuration Release --no-restore

# 6. Portal
Write-Host "`n[6/6] Portal bagimliliklari yukleniyor..." -ForegroundColor Yellow
Set-Location portal
npm install
Set-Location ..

Write-Host "`n=== KURULUM TAMAMLANDI ===" -ForegroundColor Green
Write-Host "API: https://localhost:5001" -ForegroundColor Cyan
Write-Host "Swagger: https://localhost:5001/swagger" -ForegroundColor Cyan
Write-Host "Portal: http://localhost:3000" -ForegroundColor Cyan
Write-Host "Seq Logs: http://localhost:8080" -ForegroundColor Cyan
