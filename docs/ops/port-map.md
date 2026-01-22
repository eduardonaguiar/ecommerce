# Port Map (Local Infrastructure)

| Service | Port | Purpose | Notes |
| --- | --- | --- | --- |
| Kafka (external) | 29092 | Kafka broker listener | `PLAINTEXT_HOST` for local tooling |
| Zookeeper | 2181 | Kafka coordination | Internal metadata for Kafka |
| RabbitMQ | 5672 | AMQP | Default broker port |
| RabbitMQ Management UI | 15672 | UI | http://localhost:15672 |
| Redis | 6379 | Cache | Redis server |
| Postgres | 5432 | Relational DB | Databases: `orders`, `payments`, `inventory` |
| MongoDB | 27017 | Catalog DB | Database: `catalog` |
| Elasticsearch | 9200 | Search API | REST API |
| Elasticsearch (transport) | 9300 | Internal transport | Cluster communication |
| Kibana | 5601 | UI | http://localhost:5601 |

## Defaults
- Credentials for local development are set to `ecommerce` in the compose file.
- Update credentials and ports as needed for production deployments.
