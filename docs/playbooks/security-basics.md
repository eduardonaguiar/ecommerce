# Security Basics (JWT + TLS)

This playbook validates gateway TLS termination and JWT enforcement.

## 1) Ensure the gateway TLS certificate exists

```bash
dotnet dev-certs https -ep services/gateway/certs/gateway-dev.pfx -p devpassword
```

## 2) Run the security test

```bash
make e2e-security
```

## 3) Validate manual checks (optional)

```bash
curl -k https://localhost:8443/health/ready
curl -k -i https://localhost:8443/catalog/products
```

If you have a valid JWT:

```bash
curl -k -H "Authorization: Bearer <jwt>" https://localhost:8443/catalog/products
```

## Expected outcomes

- `/health/ready` responds over HTTPS.
- Requests without a JWT return `401` or `403`.
- Authenticated requests succeed when a valid token is provided.
