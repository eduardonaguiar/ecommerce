import { makeCorrelationId } from './correlation.js';

const correlationId = __ENV.CORRELATION_ID || makeCorrelationId();

const config = {
  correlationId,
  ordersBaseUrl: __ENV.ORDERS_BASE_URL || 'http://localhost:8085',
  paymentsBaseUrl: __ENV.PAYMENTS_BASE_URL || 'http://localhost:8086',
  inventoryBaseUrl: __ENV.INVENTORY_BASE_URL || 'http://localhost:8087',
  catalogBaseUrl: __ENV.CATALOG_BASE_URL || 'http://localhost:8083',
  gatewayBaseUrl: __ENV.GATEWAY_BASE_URL || 'https://localhost:8443',
  queryBaseUrl: __ENV.QUERY_BASE_URL || __ENV.ORDERS_BASE_URL || 'http://localhost:8085',
  queryOrderPath: __ENV.QUERY_ORDER_PATH || '/orders',
  pollIntervalMs: Number(__ENV.POLL_INTERVAL_MS || 500),
  pollTimeoutMs: Number(__ENV.POLL_TIMEOUT_MS || 60000),
  inventoryDrainCount: Number(__ENV.INVENTORY_DRAIN_COUNT || 100)
};

export default config;
