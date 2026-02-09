FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY services/payments/src/Payments/Payments.csproj src/Payments/
RUN dotnet restore src/Payments/Payments.csproj --configfile /src/NuGet.Config
RUN test -f /src/src/Payments/obj/project.assets.json \
    && ! grep -nE 'C:\\\\|Program Files' /src/src/Payments/obj/project.assets.json \
    && ! grep -n "fallbackPackageFolders" /src/src/Payments/obj/project.assets.json

COPY services/payments/src/Payments/ src/Payments/
WORKDIR /src/src/Payments
RUN dotnet publish /src/src/Payments/Payments.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Payments.dll"]
