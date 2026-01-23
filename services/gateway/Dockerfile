FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY services/gateway/src/Gateway/Gateway.csproj src/Gateway/
RUN dotnet restore src/Gateway/Gateway.csproj --configfile /src/NuGet.Config
RUN test -f /src/src/Gateway/obj/project.assets.json \
    && ! grep -nE 'C:\\\\|Program Files' /src/src/Gateway/obj/project.assets.json \
    && ! grep -n "fallbackPackageFolders" /src/src/Gateway/obj/project.assets.json

COPY services/gateway/src/Gateway/ src/Gateway/
WORKDIR /src/src/Gateway
RUN dotnet publish /src/src/Gateway/Gateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080 8443
ENTRYPOINT ["dotnet", "Gateway.dll"]
