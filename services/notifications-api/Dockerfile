FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/NotificationsApi/NotificationsApi.csproj src/NotificationsApi/
RUN dotnet restore src/NotificationsApi/NotificationsApi.csproj

COPY src/NotificationsApi/ src/NotificationsApi/
WORKDIR /src/src/NotificationsApi
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NotificationsApi.dll"]
