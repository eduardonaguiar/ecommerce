import { check, sleep } from 'k6';
import config from './lib/config.js';
import { post, get } from './lib/http_client.js';
import { pollUntil } from './lib/polling.js';
import { checkStatus, checkOrderState, checkPaymentAttempt } from './lib/assertions.js';
import { buildOrderRequest, buildPaymentRequest } from './lib/data_builders.js';

export const options = {
  vus: 1,
  iterations: 1,
  thresholds: {
    checks: ['rate>0.95'],
    http_req_failed: ['rate<0.1']
  }
};

export default function () {
  console.log(`correlationId=${config.correlationId}`);

  const orderPayload = buildOrderRequest();
  const createOrderRes = post(`${config.ordersBaseUrl}/orders`, orderPayload);
  checkStatus(createOrderRes, 202, 'create order');

  const order = createOrderRes.json();
  check(order, { 'order id returned': () => !!order.id });

  const paymentPayload = buildPaymentRequest(order.id, { forceOutcome: 'success' });
  const paymentRes = post(`${config.paymentsBaseUrl}/payments`, paymentPayload);
  checkStatus(paymentRes, 200, 'process payment');

  const paymentAttempt = paymentRes.json();
  checkPaymentAttempt(paymentAttempt, { status: 'SUCCESS', effective: true }, 'payment');

  const finalOrderRes = pollUntil(
    () => get(`${config.ordersBaseUrl}/orders/${order.id}`),
    (response) => response && response.status === 200 && response.json().status === 'CONFIRMED',
    config.pollTimeoutMs,
    config.pollIntervalMs
  );

  checkStatus(finalOrderRes, 200, 'fetch order');
  const finalOrder = finalOrderRes.json();
  checkOrderState(finalOrder, { status: 'CONFIRMED', stockStatus: 'RESERVED', paymentStatus: 'PROCESSED' }, 'final order');

  sleep(1);
}
