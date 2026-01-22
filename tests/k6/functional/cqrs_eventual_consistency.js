/*
 * Scenario: CQRS Convergence (Functional Perspective)
 * Preconditions:
 * - Gateway is reachable and authenticated (JWT provided).
 * Expected final state:
 * - Write model reaches CONFIRMED.
 * - Query model eventually matches the write model.
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

export default function () {
  requireBearerToken();
  const correlationId = makeCorrelationId();
  console.log(`correlationId=${correlationId}`);

  const api = createApiClient({ baseUrl: gatewayBaseUrl, correlationId });
  const queryApi = createApiClient({ baseUrl: queryBaseUrl, correlationId });

  // GIVEN an order is created
  const orderPayload = buildOrderRequest({ customerId: `cust-${correlationId}` });
  const createOrderRes = api.post('/orders/orders', orderPayload);
  checkStatus(createOrderRes, 202, 'create order');

  const order = createOrderRes.json();
  check(order, { 'order id returned': () => !!order.id });

  // WHEN payment succeeds
  const paymentPayload = buildPaymentRequest(order.id, {
    amount: orderPayload.amount,
    currency: orderPayload.currency,
    forceOutcome: 'success'
  });
  const paymentRes = api.post('/payments/payments', paymentPayload);
  checkStatus(paymentRes, 200, 'process payment');
  checkPaymentAttempt(paymentRes.json(), { status: 'SUCCESS', effective: true }, 'payment success');

  // THEN the write model reaches CONFIRMED
  const confirmedOrderRes = pollUntil(
    () => api.get(`/orders/orders/${order.id}`),
    (response) => response && response.status === 200 && response.json().status === 'CONFIRMED',
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(confirmedOrderRes, 200, 'fetch write model');
  const confirmedOrder = confirmedOrderRes.json();
  checkOrderState(confirmedOrder, { status: 'CONFIRMED' }, 'write model');

  // AND the query model converges to the same state
  const queryOrderRes = pollUntil(
    () => queryApi.get(buildQueryOrderPath(order.id)),
    (response) => response && response.status === 200 && response.json().status === confirmedOrder.status,
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(queryOrderRes, 200, 'fetch query model');
  checkOrderState(queryOrderRes.json(), { status: confirmedOrder.status }, 'query model');
}
