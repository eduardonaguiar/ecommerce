FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY services/notifications-api/src/NotificationsApi/NotificationsApi.csproj src/NotificationsApi/
RUN dotnet restore src/NotificationsApi/NotificationsApi.csproj --configfile /src/NuGet.Config
RUN test -f /src/src/NotificationsApi/obj/project.assets.json \
    && ! grep -nE 'C:\\\\|Program Files' /src/src/NotificationsApi/obj/project.assets.json \
    && ! grep -n "fallbackPackageFolders" /src/src/NotificationsApi/obj/project.assets.json

COPY services/notifications-api/src/NotificationsApi/ src/NotificationsApi/
WORKDIR /src/src/NotificationsApi
RUN dotnet publish /src/src/NotificationsApi/NotificationsApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationsApi.dll"]
