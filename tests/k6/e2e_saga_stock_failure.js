import { check } from 'k6';
import config from './lib/config.js';
import { post, get } from './lib/http_client.js';
import { pollUntil } from './lib/polling.js';
import { checkStatus, checkOrderState } from './lib/assertions.js';
import { buildOrderRequest } from './lib/data_builders.js';

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

  for (let i = 0; i < config.inventoryDrainCount; i += 1) {
    const createOrderRes = post(`${config.ordersBaseUrl}/orders`, buildOrderRequest());
    checkStatus(createOrderRes, 202, `drain order ${i + 1}`);
  }

  const drainedInventory = pollUntil(
    () => get(`${config.inventoryBaseUrl}/inventory/default`),
    (response) => response && response.status === 200 && response.json().availableQuantity <= 0,
    config.pollTimeoutMs,
    config.pollIntervalMs
  );

  checkStatus(drainedInventory, 200, 'inventory drained');
  const inventorySnapshot = drainedInventory.json();
  check(inventorySnapshot, {
    'inventory available <= 0': () => inventorySnapshot.availableQuantity <= 0
  });

  const failureOrderRes = post(`${config.ordersBaseUrl}/orders`, buildOrderRequest());
  checkStatus(failureOrderRes, 202, 'create failure order');
  const failureOrder = failureOrderRes.json();

  const finalOrderRes = pollUntil(
    () => get(`${config.ordersBaseUrl}/orders/${failureOrder.id}`),
    (response) => response && response.status === 200 && response.json().status === 'CANCELLED',
    config.pollTimeoutMs,
    config.pollIntervalMs
  );

  checkStatus(finalOrderRes, 200, 'fetch failure order');
  const finalOrder = finalOrderRes.json();
  checkOrderState(finalOrder, { status: 'CANCELLED', stockStatus: 'FAILED' }, 'final order');
}
