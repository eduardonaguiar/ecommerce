import { check } from 'k6';
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

  const paymentPayload = buildPaymentRequest(order.id, { forceOutcome: 'failure' });
  const paymentRes = post(`${config.paymentsBaseUrl}/payments`, paymentPayload);
  checkStatus(paymentRes, 200, 'process payment');

  const paymentAttempt = paymentRes.json();
  checkPaymentAttempt(paymentAttempt, { status: 'FAILURE', effective: false }, 'payment failure');

  const finalOrderRes = pollUntil(
    () => get(`${config.ordersBaseUrl}/orders/${order.id}`),
    (response) => response && response.status === 200 && response.json().paymentStatus === 'PROCESSED',
    config.pollTimeoutMs,
    config.pollIntervalMs
  );

  checkStatus(finalOrderRes, 200, 'fetch order');
  const finalOrder = finalOrderRes.json();
  checkOrderState(finalOrder, { paymentStatus: 'PROCESSED' }, 'order after payment failure');
}
