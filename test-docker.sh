#!/usr/bin/env bash
# Chạy test backend trong container Linux (.NET 8) vì Smart App Control chặn DLL test trên Windows.
# Dùng: dtest.sh [filter]
FILTER=${1:-}
MSYS_NO_PATHCONV=1 docker run --rm -v C:/dev/tranthienquan/backend:/mnt:ro -v tt-nuget:/root/.nuget/packages \
  mcr.microsoft.com/dotnet/sdk:8.0 bash -c "
    mkdir -p /src && tar -C /mnt --exclude=bin --exclude=obj -cf - . | tar -C /src -xf -
    cd /src && dotnet --version && dotnet build TranThienQuan.Api.Tests -v q -nologo 2>&1 | grep -E ' error |rror\(s\)' ;
    dotnet TranThienQuan.Api.Tests/bin/Debug/net8.0/TranThienQuan.Api.Tests.dll ${FILTER:+-class \"$FILTER\"}"
