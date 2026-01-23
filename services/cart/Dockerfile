FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY services/cart/src/Cart/Cart.csproj src/Cart/
RUN dotnet restore src/Cart/Cart.csproj --configfile /src/NuGet.Config
RUN test -f /src/src/Cart/obj/project.assets.json \
    && ! grep -nE 'C:\\\\|Program Files' /src/src/Cart/obj/project.assets.json \
    && ! grep -n "fallbackPackageFolders" /src/src/Cart/obj/project.assets.json

COPY services/cart/src/Cart/ src/Cart/
WORKDIR /src/src/Cart
RUN dotnet publish /src/src/Cart/Cart.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cart.dll"]
