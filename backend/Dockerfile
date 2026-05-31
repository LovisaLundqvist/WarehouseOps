FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY WarehouseOps.sln ./
COPY WarehouseOps.Api/WarehouseOps.Api.csproj WarehouseOps.Api/
COPY WarehouseOps.Application/WarehouseOps.Application.csproj WarehouseOps.Application/
COPY WarehouseOps.Domain/WarehouseOps.Domain.csproj WarehouseOps.Domain/
COPY WarehouseOps.Infrastructure/WarehouseOps.Infrastructure.csproj WarehouseOps.Infrastructure/
COPY WarehouseOps.Tests/WarehouseOps.Tests.csproj WarehouseOps.Tests/

RUN dotnet restore WarehouseOps.sln

COPY . .

RUN dotnet publish WarehouseOps.Api/WarehouseOps.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WarehouseOps.Api.dll"]
