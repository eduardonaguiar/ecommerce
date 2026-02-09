FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config ./
COPY services/notifications-worker/src/NotificationsWorker/NotificationsWorker.csproj src/NotificationsWorker/
RUN dotnet restore src/NotificationsWorker/NotificationsWorker.csproj --configfile /src/NuGet.Config
RUN test -f /src/src/NotificationsWorker/obj/project.assets.json \
    && ! grep -nE 'C:\\\\|Program Files' /src/src/NotificationsWorker/obj/project.assets.json \
    && ! grep -n "fallbackPackageFolders" /src/src/NotificationsWorker/obj/project.assets.json

COPY services/notifications-worker/src/NotificationsWorker/ src/NotificationsWorker/
WORKDIR /src/src/NotificationsWorker
RUN dotnet publish /src/src/NotificationsWorker/NotificationsWorker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationsWorker.dll"]
