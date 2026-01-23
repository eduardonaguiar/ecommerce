FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY services/inventory/src/Inventory/Inventory.csproj src/Inventory/
RUN dotnet restore src/Inventory/Inventory.csproj --configfile /src/NuGet.Config
RUN test -f /src/src/Inventory/obj/project.assets.json \
    && ! grep -nE 'C:\\\\|Program Files' /src/src/Inventory/obj/project.assets.json \
    && ! grep -n "fallbackPackageFolders" /src/src/Inventory/obj/project.assets.json

COPY services/inventory/src/Inventory/ src/Inventory/
WORKDIR /src/src/Inventory
RUN dotnet publish /src/src/Inventory/Inventory.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Inventory.dll"]
