# Gateway TLS (Development)

## Overview
The gateway is configured to serve HTTPS in development. A local certificate is mounted into the container and used by Kestrel to terminate TLS on port `8443`.

## Generate a Development Certificate
From the repo root, generate and export a dev certificate:

```bash
dotnet dev-certs https -ep services/gateway/certs/gateway-dev.pfx -p devpassword
```

This file is intentionally ignored by git.

## Compose Wiring
`infra/compose/docker-compose.yml` mounts the certificate directory and configures Kestrel via environment variables:

- `ASPNETCORE_Kestrel__Certificates__Default__Path=/https/gateway-dev.pfx`
- `ASPNETCORE_Kestrel__Certificates__Default__Password=devpassword`
- `ASPNETCORE_URLS=http://0.0.0.0:8080;https://0.0.0.0:8443`

## Validation Steps
1. Start the gateway:
   ```bash
   docker compose -f infra/compose/docker-compose.yml up --build gateway
   ```
2. Confirm TLS is active:
   ```bash
   curl -k https://localhost:8443/health/ready
   ```
   Expect a `200 OK` response.
