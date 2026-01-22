# Gateway JWT Validation

## Overview
The gateway enforces JWT validation for all proxied traffic. Authentication is **deny-by-default**; every route requires a valid bearer token unless explicitly marked `AllowAnonymous` (currently only the health checks). The gateway uses the standard `Authorization: Bearer <token>` header and validates issuer/audience metadata based on configuration.

## Configuration
Update the following settings to point to your identity provider:

- `Jwt:Authority`: The OIDC authority (issuer base URL).
- `Jwt:Issuer`: Expected token issuer (typically matches the authority).
- `Jwt:Audience`: The expected API audience value.
- `Jwt:RequireHttpsMetadata`: Set to `true` in production.

In Docker Compose, these are mapped via environment variables:

```yaml
Jwt__Authority: https://auth.local
Jwt__Issuer: https://auth.local
Jwt__Audience: ecommerce-api
Jwt__RequireHttpsMetadata: "false"
```

## Validation Steps
1. Start the stack with the gateway and downstream services:
   ```bash
   docker compose -f infra/compose/docker-compose.yml up --build gateway catalog cart orders payments query
   ```
2. Validate that unauthenticated access is denied:
   ```bash
   curl -k -i https://localhost:8443/catalog/
   ```
   Expect a `401 Unauthorized` response.
3. Validate authenticated access by supplying a JWT issued by your IdP:
   ```bash
   curl -k -H "Authorization: Bearer <jwt>" https://localhost:8443/catalog/
   ```

## OpenAPI
The gateway exposes its OpenAPI document at `https://localhost:8443/swagger/v1/swagger.json` and the Swagger UI at `https://localhost:8443/swagger`. Authentication is required unless you adjust the fallback policy in code.
