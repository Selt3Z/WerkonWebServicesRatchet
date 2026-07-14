FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["WerkonWebServicesRatchet/WerkonWebServicesRatchet.csproj", "WerkonWebServicesRatchet/"]
RUN dotnet restore "WerkonWebServicesRatchet/WerkonWebServicesRatchet.csproj"

COPY ["WerkonWebServicesRatchet/", "WerkonWebServicesRatchet/"]
WORKDIR /src/WerkonWebServicesRatchet
RUN dotnet publish "WerkonWebServicesRatchet.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WerkonWebServicesRatchet.dll"]
