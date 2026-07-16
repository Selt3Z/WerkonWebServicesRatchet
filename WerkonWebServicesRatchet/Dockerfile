FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["WerkonWebServicesRatchet/WerkonWebServicesRatchet.csproj", "WerkonWebServicesRatchet/"]
RUN dotnet restore "WerkonWebServicesRatchet/WerkonWebServicesRatchet.csproj"

COPY ["WerkonWebServicesRatchet/", "WerkonWebServicesRatchet/"]
WORKDIR /src/WerkonWebServicesRatchet
RUN dotnet publish "WerkonWebServicesRatchet.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN set -eux; \
    ok=0; \
    for i in 1 2 3 4 5; do \
      if apt-get update \
        && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
            curl postgresql-client gzip bash; then \
        ok=1; \
        break; \
      fi; \
      echo "apt attempt $i failed, retrying..."; \
      sleep 10; \
    done; \
    test "$ok" -eq 1; \
    command -v curl; \
    command -v psql; \
    command -v gunzip; \
    rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WerkonWebServicesRatchet.dll"]
