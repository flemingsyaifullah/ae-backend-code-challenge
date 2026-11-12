FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/AeShipManagement.Api/AeShipManagement.Api.csproj src/AeShipManagement.Api/
COPY src/AeShipManagement.Application/AeShipManagement.Application.csproj src/AeShipManagement.Application/
COPY src/AeShipManagement.Infrastructure/AeShipManagement.Infrastructure.csproj src/AeShipManagement.Infrastructure/
RUN dotnet restore src/AeShipManagement.Api/AeShipManagement.Api.csproj

COPY src/ src/
RUN dotnet publish src/AeShipManagement.Api/AeShipManagement.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "AeShipManagement.Api.dll"]
