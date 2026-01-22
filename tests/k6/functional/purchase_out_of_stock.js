/*
 * Scenario: Out-of-Stock Flow
 * Preconditions:
 * - Gateway is reachable and authenticated (JWT provided).
 * - Inventory default product has a finite stock quantity.
 * Expected final state:
 * - Order is CANCELLED after stock reservation fails.
 * - Payment is not finalized (no payment attempt is made).
 */
import { check } from 'k6';
import { createApiClient } from './lib/api_client.js';
import { makeCorrelationId } from './lib/correlation.js';
import { requireBearerToken } from './lib/auth.js';
import { pollUntil } from './lib/polling.js';
import { checkStatus, checkOrderState } from './lib/assertions.js';
import { buildOrderRequest } from './lib/data_factory.js';

export const options = {
  vus: 1,
  iterations: 1,
  insecureSkipTLSVerify: true,
  thresholds: {
    checks: ['rate>0.95'],
    http_req_failed: ['rate<0.1']
  }
};

const gatewayBaseUrl = __ENV.GATEWAY_BASE_URL || 'https://localhost:8443';
const pollTimeoutMs = Number(__ENV.POLL_TIMEOUT_MS || 60000);
const pollIntervalMs = Number(__ENV.POLL_INTERVAL_MS || 500);
const inventoryDrainCount = Number(__ENV.INVENTORY_DRAIN_COUNT || 100);
const inventoryProductId = __ENV.INVENTORY_PRODUCT_ID || 'default';

export default function () {
  requireBearerToken();
  const correlationId = makeCorrelationId();
  console.log(`correlationId=${correlationId}`);

  const api = createApiClient({ baseUrl: gatewayBaseUrl, correlationId });

  // GIVEN the inventory is drained by creating multiple orders
  for (let i = 0; i < inventoryDrainCount; i += 1) {
    const createOrderRes = api.post('/orders/orders', buildOrderRequest({ customerId: `drain-${correlationId}-${i}` }));
    checkStatus(createOrderRes, 202, `drain order ${i + 1}`);
  }

  const drainedInventoryRes = pollUntil(
    () => api.get(`/inventory/inventory/${inventoryProductId}`),
    (response) => response && response.status === 200 && response.json().availableQuantity <= 0,
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(drainedInventoryRes, 200, 'inventory drained');
  const drainedInventory = drainedInventoryRes.json();
  check(drainedInventory, {
    'inventory available <= 0': () => drainedInventory.availableQuantity <= 0
  });

  // WHEN a new order is created
  const failureOrderRes = api.post('/orders/orders', buildOrderRequest({ customerId: `cust-${correlationId}` }));
  checkStatus(failureOrderRes, 202, 'create out-of-stock order');
  const failureOrder = failureOrderRes.json();

  // THEN the order is cancelled due to stock failure
  const finalOrderRes = pollUntil(
    () => api.get(`/orders/orders/${failureOrder.id}`),
    (response) => response && response.status === 200 && response.json().status === 'CANCELLED',
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(finalOrderRes, 200, 'fetch out-of-stock order');
  const finalOrder = finalOrderRes.json();
  checkOrderState(finalOrder, { status: 'CANCELLED' }, 'out-of-stock order');
  check(finalOrder, {
    'payment not finalized': () => {
      const paymentStatus = String(finalOrder.paymentStatus || '').toUpperCase();
      return paymentStatus !== 'PROCESSED' && paymentStatus !== 'PAID';
    }
  });
}
