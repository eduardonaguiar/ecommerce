FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY services/orders/src/Orders/Orders.csproj src/Orders/
RUN dotnet restore src/Orders/Orders.csproj --configfile /src/NuGet.Config
RUN test -f /src/src/Orders/obj/project.assets.json \
    && ! grep -nE 'C:\\\\|Program Files' /src/src/Orders/obj/project.assets.json \
    && ! grep -n "fallbackPackageFolders" /src/src/Orders/obj/project.assets.json

COPY services/orders/src/Orders/ src/Orders/
WORKDIR /src/src/Orders
RUN dotnet publish /src/src/Orders/Orders.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Orders.dll"]
