/*
 * Scenario: Notification Side Effects (Observable)
 * Preconditions:
 * - Gateway is reachable and authenticated (JWT provided).
 * - Notifications API is reachable on the public base URL.
 * Expected final state:
 * - Order reaches a terminal state (CONFIRMED).
 * - Notifications API remains healthy and reachable as the observable signal.
 */
import { check } from 'k6';
import { createApiClient } from './lib/api_client.js';
import { makeCorrelationId } from './lib/correlation.js';
import { requireBearerToken } from './lib/auth.js';
import { pollUntil } from './lib/polling.js';
import { checkStatus, checkOrderState, checkPaymentAttempt, checkJson } from './lib/assertions.js';
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
const notificationsBaseUrl = __ENV.NOTIFICATIONS_BASE_URL || 'http://localhost:8089';
const pollTimeoutMs = Number(__ENV.POLL_TIMEOUT_MS || 60000);
const pollIntervalMs = Number(__ENV.POLL_INTERVAL_MS || 500);

export default function () {
  requireBearerToken();
  const correlationId = makeCorrelationId();
  console.log(`correlationId=${correlationId}`);

  const api = createApiClient({ baseUrl: gatewayBaseUrl, correlationId });
  const notificationsApi = createApiClient({ baseUrl: notificationsBaseUrl, correlationId });

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

  // THEN the order reaches CONFIRMED
  const confirmedOrderRes = pollUntil(
    () => api.get(`/orders/orders/${order.id}`),
    (response) => response && response.status === 200 && response.json().status === 'CONFIRMED',
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(confirmedOrderRes, 200, 'fetch confirmed order');
  checkOrderState(confirmedOrderRes.json(), { status: 'CONFIRMED' }, 'confirmed order');

  // AND a public notification signal remains healthy/reachable
  const notificationsRes = notificationsApi.get('/');
  checkStatus(notificationsRes, 200, 'notifications api reachable');
  checkJson(notificationsRes, 'notifications api');
  check(notificationsRes.json(), {
    'notifications api status ok': (body) => body.status === 'ok'
  });
}
