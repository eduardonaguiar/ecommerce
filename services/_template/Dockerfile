FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY services/_template/src/ServiceTemplate/ServiceTemplate.csproj src/ServiceTemplate/
RUN dotnet restore src/ServiceTemplate/ServiceTemplate.csproj --configfile /src/NuGet.Config
RUN test -f /src/src/ServiceTemplate/obj/project.assets.json \
    && ! grep -nE 'C:\\\\|Program Files' /src/src/ServiceTemplate/obj/project.assets.json \
    && ! grep -n "fallbackPackageFolders" /src/src/ServiceTemplate/obj/project.assets.json

COPY services/_template/src/ServiceTemplate/ src/ServiceTemplate/
WORKDIR /src/src/ServiceTemplate
RUN dotnet publish /src/src/ServiceTemplate/ServiceTemplate.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ServiceTemplate.dll"]
