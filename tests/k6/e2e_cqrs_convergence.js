import { check } from 'k6';
import config from './lib/config.js';
import { post, get } from './lib/http_client.js';
import { pollUntil } from './lib/polling.js';
import { checkStatus, checkOrderState } from './lib/assertions.js';
import { buildOrderRequest, buildPaymentRequest } from './lib/data_builders.js';

export const options = {
  vus: 1,
  iterations: 1,
  thresholds: {
    checks: ['rate>0.95'],
    http_req_failed: ['rate<0.1']
  }
};

function buildQueryOrderUrl(orderId) {
  const base = config.queryBaseUrl.replace(/\/$/, '');
  const path = config.queryOrderPath.startsWith('/') ? config.queryOrderPath : `/${config.queryOrderPath}`;
  return `${base}${path}/${orderId}`;
}

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

  const confirmedOrderRes = pollUntil(
    () => get(`${config.ordersBaseUrl}/orders/${order.id}`),
    (response) => response && response.status === 200 && response.json().status === 'CONFIRMED',
    config.pollTimeoutMs,
    config.pollIntervalMs
  );

  checkStatus(confirmedOrderRes, 200, 'fetch write model');
  const writeOrder = confirmedOrderRes.json();
  checkOrderState(writeOrder, { status: 'CONFIRMED' }, 'write model');

  const queryOrderRes = pollUntil(
    () => get(buildQueryOrderUrl(order.id)),
    (response) => response && response.status === 200 && response.json().status === writeOrder.status,
    config.pollTimeoutMs,
    config.pollIntervalMs
  );

  checkStatus(queryOrderRes, 200, 'fetch query model');
  const queryOrder = queryOrderRes.json();
  checkOrderState(queryOrder, { status: writeOrder.status }, 'query model');
}
