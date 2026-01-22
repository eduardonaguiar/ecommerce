# Catalog API

## Overview
The Catalog service provides read-only product and category endpoints backed by MongoDB. It also includes a minimal admin upsert endpoint that publishes `catalog.product.upserted` events to Kafka for downstream consumers.

## Base URL
- Direct: `http://localhost:8083`
- Gateway: `http://localhost:8080/catalog`

## Endpoints

### GET /products
Returns all products.

**Response**
```json
[
  {
    "id": "string",
    "name": "string",
    "category": "string",
    "price": 0,
    "currency": "USD",
    "description": "string",
    "imageUrl": "string",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
]
```

### GET /products/{id}
Returns a single product by id.

**Response**
```json
{
  "id": "string",
  "name": "string",
  "category": "string",
  "price": 0,
  "currency": "USD",
  "description": "string",
  "imageUrl": "string",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### GET /categories
Returns the distinct list of categories.

**Response**
```json
["string"]
```

### POST /admin/products
Upserts a product and emits a `catalog.product.upserted` event. This endpoint is the minimal mechanism for producing catalog events.

**Request**
```json
{
  "id": "optional-string",
  "name": "string",
  "category": "string",
  "price": 0,
  "currency": "USD",
  "description": "string",
  "imageUrl": "string"
}
```

**Response**
```json
{
  "id": "string",
  "name": "string",
  "category": "string",
  "price": 0,
  "currency": "USD",
  "description": "string",
  "imageUrl": "string",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

## Configuration
Environment variables used by the service:

| Variable | Default | Description |
| --- | --- | --- |
| `MONGO_CONNECTION_STRING` | `mongodb://ecommerce:ecommerce@mongodb:27017/catalog?authSource=admin` | Mongo connection string. |
| `MONGO_DATABASE` | `catalog` | Mongo database name. |
| `KAFKA_BOOTSTRAP_SERVERS` | `kafka:9092` | Kafka bootstrap servers. |
| `KAFKA_TOPIC` | `catalog.product-upserted` | Topic for product upsert events. |

## Validation steps (Docker Compose)
1. Start dependencies and the catalog service:
   ```bash
   docker compose -f infra/compose/docker-compose.yml up -d mongodb kafka catalog
   ```
2. Upsert a product (emits an event):
   ```bash
   curl -X POST http://localhost:8083/admin/products \
     -H "Content-Type: application/json" \
     -d '{"name":"Running Shoes","category":"Footwear","price":89.99,"currency":"USD"}'
   ```
3. Read products and categories:
   ```bash
   curl http://localhost:8083/products
   curl http://localhost:8083/categories
   ```
