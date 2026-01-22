/*
 * Scenario: Payment Failure Flow
 * Preconditions:
 * - Gateway is reachable and authenticated (JWT provided).
 * - Inventory has available stock for reservations.
 * Expected final state:
 * - Payment attempt is FAILURE.
 * - Order reaches CANCELLED.
 * - Inventory reservation is released.
 * - Query/read model reflects CANCELLED.
 */
import { check } from 'k6';
import { createApiClient, buildQueryOrderPath } from './lib/api_client.js';
import { makeCorrelationId } from './lib/correlation.js';
import { requireBearerToken } from './lib/auth.js';
import { pollUntil } from './lib/polling.js';
import { checkStatus, checkOrderState, checkPaymentAttempt } from './lib/assertions.js';
import { buildOrderRequest, buildPaymentRequest } from './lib/data_factory.js';

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
const queryBaseUrl = __ENV.QUERY_BASE_URL || `${gatewayBaseUrl}/query`;
const pollTimeoutMs = Number(__ENV.POLL_TIMEOUT_MS || 60000);
const pollIntervalMs = Number(__ENV.POLL_INTERVAL_MS || 500);
const inventoryProductId = __ENV.INVENTORY_PRODUCT_ID || 'default';

export default function () {
  requireBearerToken();
  const correlationId = makeCorrelationId();
  console.log(`correlationId=${correlationId}`);

  const api = createApiClient({ baseUrl: gatewayBaseUrl, correlationId });
  const queryApi = createApiClient({ baseUrl: queryBaseUrl, correlationId });

  // GIVEN a new order is created
  const inventoryBeforeRes = api.get(`/inventory/inventory/${inventoryProductId}`);
  checkStatus(inventoryBeforeRes, 200, 'fetch inventory snapshot');
  const inventoryBefore = inventoryBeforeRes.json();

  const orderPayload = buildOrderRequest({ customerId: `cust-${correlationId}` });
  const createOrderRes = api.post('/orders/orders', orderPayload);
  checkStatus(createOrderRes, 202, 'create order');

  const order = createOrderRes.json();
  check(order, { 'order id returned': () => !!order.id });

  // WHEN payment fails deterministically
  const paymentPayload = buildPaymentRequest(order.id, {
    amount: orderPayload.amount,
    currency: orderPayload.currency,
    forceOutcome: 'failure'
  });
  const paymentRes = api.post('/payments/payments', paymentPayload);
  checkStatus(paymentRes, 200, 'process payment failure');
  checkPaymentAttempt(paymentRes.json(), { status: 'FAILURE', effective: false }, 'payment failure');

  // THEN the order is eventually CANCELLED
  const cancelledOrderRes = pollUntil(
    () => api.get(`/orders/orders/${order.id}`),
    (response) => response && response.status === 200 && response.json().status === 'CANCELLED',
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(cancelledOrderRes, 200, 'fetch cancelled order');
  checkOrderState(cancelledOrderRes.json(), { status: 'CANCELLED' }, 'cancelled order');

  // AND inventory reservation is released
  const inventoryAfterRes = pollUntil(
    () => api.get(`/inventory/inventory/${inventoryProductId}`),
    (response) => {
      if (!response || response.status !== 200) {
        return false;
      }
      const current = response.json();
      return current.availableQuantity >= inventoryBefore.availableQuantity;
    },
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(inventoryAfterRes, 200, 'inventory released');
  const inventoryAfter = inventoryAfterRes.json();
  check(inventoryAfter, {
    'inventory availability restored': () => inventoryAfter.availableQuantity >= inventoryBefore.availableQuantity
  });

  // AND the query/read model converges to CANCELLED
  const queryOrderRes = pollUntil(
    () => queryApi.get(buildQueryOrderPath(order.id)),
    (response) => response && response.status === 200 && response.json().status === 'CANCELLED',
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(queryOrderRes, 200, 'fetch query order');
  checkOrderState(queryOrderRes.json(), { status: 'CANCELLED' }, 'query order');
}
