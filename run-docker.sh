#!/usr/bin/env bash
# Chạy API trong Docker ở http://localhost:5080 (dữ liệu InMemory, reset khi khởi động lại).
# Dùng khi Windows Smart App Control chặn chạy DLL tự build. Máy ảo Android gọi http://10.0.2.2:5080
docker rm -f tranthienquan-api >/dev/null 2>&1
MSYS_NO_PATHCONV=1 docker run -d --name tranthienquan-api -p 5080:8080 \
  -v C:/dev/tranthienquan/backend:/mnt:ro -v tt-nuget:/root/.nuget/packages \
  -e ASPNETCORE_ENVIRONMENT=Testing -e TestDbName=dev \
  mcr.microsoft.com/dotnet/sdk:8.0 bash -c "
    mkdir -p /src && tar -C /mnt --exclude=bin --exclude=obj -cf - . | tar -C /src -xf -
    cd /src/TranThienQuan.Api && dotnet build -v q -nologo && dotnet bin/Debug/net8.0/TranThienQuan.Api.dll --urls http://0.0.0.0:8080"
