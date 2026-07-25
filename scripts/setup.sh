#!/bin/bash
set -e
echo "=== SENKORA KURULUM BASLADI ==="

echo "[1/6] Versiyon kontrolleri..."
dotnet --version
node --version
docker --version
git --version

echo "[2/6] Docker servisleri baslatiliyor..."
cd docker && docker compose up -d && cd ..

echo "[3/6] .env dosyasi kontrol ediliyor..."
if [ ! -f ".env" ]; then
    cp .env.example .env
    echo "UYARI: .env dosyasi olusturuldu. Lutfen duzenleyin."
fi

echo "[4/6] NuGet paketleri yukleniyor..."
dotnet restore Senkora.sln

echo "[5/6] Solution derleniyor..."
dotnet build Senkora.sln --configuration Release --no-restore

echo "[6/6] Portal bagimliliklari yukleniyor..."
cd portal && npm install && cd ..

echo "=== KURULUM TAMAMLANDI ==="
echo "API:     https://localhost:5001"
echo "Swagger: https://localhost:5001/swagger"
echo "Portal:  http://localhost:3000"
echo "Seq:     http://localhost:8080"
