/*
 * Scenario: Successful Purchase Flow
 * Preconditions:
 * - Gateway is reachable and authenticated (JWT provided).
 * - Inventory default product has available stock.
 * Expected final state:
 * - Order reaches CONFIRMED.
 * - Inventory shows reduced availability/reserved quantity.
 * - Query/read model reflects CONFIRMED.
 */
import { check } from 'k6';
import { createApiClient, buildQueryOrderPath } from './lib/api_client.js';
import { makeCorrelationId } from './lib/correlation.js';
import { requireBearerToken } from './lib/auth.js';
import { pollUntil } from './lib/polling.js';
import { checkStatus, checkJson, checkOrderState, checkPaymentAttempt, checkCartItems } from './lib/assertions.js';
import { buildProductPayload, buildCartItem, buildOrderRequest, buildPaymentRequest } from './lib/data_factory.js';

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

  // GIVEN a shopper browses the catalog
  const productsRes = api.get('/catalog/products');
  checkStatus(productsRes, 200, 'browse products');
  checkJson(productsRes, 'browse products');

  const products = productsRes.json();
  let product = Array.isArray(products) && products.length > 0 ? products[0] : null;

  if (!product) {
    const productPayload = buildProductPayload({
      productId: `k6-${correlationId}`,
      name: 'Functional Test Sneakers'
    });
    const upsertRes = api.post('/catalog/admin/products', productPayload);
    checkStatus(upsertRes, 200, 'upsert catalog product');
    product = upsertRes.json();
  }

  // WHEN the shopper adds the product to the cart
  const cartId = `cart-${correlationId}`;
  const addItemRes = api.post(`/cart/carts/${cartId}/items`, buildCartItem(product.id, 1));
  checkStatus(addItemRes, 200, 'add item to cart');
  checkCartItems(addItemRes.json(), [{ productId: product.id, quantity: 1 }], 'cart after add');

  // AND the shopper places an order
  const inventoryBeforeRes = api.get(`/inventory/inventory/${inventoryProductId}`);
  checkStatus(inventoryBeforeRes, 200, 'fetch inventory snapshot');
  const inventoryBefore = inventoryBeforeRes.json();

  const orderPayload = buildOrderRequest({
    amount: product.price || 50,
    currency: product.currency || 'USD',
    customerId: `cust-${correlationId}`
  });
  const createOrderRes = api.post('/orders/orders', orderPayload);
  checkStatus(createOrderRes, 202, 'create order');

  const order = createOrderRes.json();
  check(order, { 'order id returned': () => !!order.id });

  // AND payment succeeds
  const paymentPayload = buildPaymentRequest(order.id, {
    amount: orderPayload.amount,
    currency: orderPayload.currency,
    forceOutcome: 'success'
  });
  const paymentRes = api.post('/payments/payments', paymentPayload);
  checkStatus(paymentRes, 200, 'process payment');
  checkPaymentAttempt(paymentRes.json(), { status: 'SUCCESS', effective: true }, 'payment success');

  // THEN the order becomes CONFIRMED in the write model
  const confirmedOrderRes = pollUntil(
    () => api.get(`/orders/orders/${order.id}`),
    (response) => response && response.status === 200 && response.json().status === 'CONFIRMED',
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(confirmedOrderRes, 200, 'fetch confirmed order');
  const confirmedOrder = confirmedOrderRes.json();
  checkOrderState(confirmedOrder, { status: 'CONFIRMED', stockStatus: 'RESERVED' }, 'confirmed order');

  // AND inventory reflects a reservation/commit
  const inventoryAfterRes = pollUntil(
    () => api.get(`/inventory/inventory/${inventoryProductId}`),
    (response) => {
      if (!response || response.status !== 200) {
        return false;
      }
      const current = response.json();
      return current.availableQuantity < inventoryBefore.availableQuantity
        || current.reservedQuantity > inventoryBefore.reservedQuantity;
    },
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(inventoryAfterRes, 200, 'inventory reduced');
  const inventoryAfter = inventoryAfterRes.json();
  check(inventoryAfter, {
    'inventory available reduced or reserved increased': () =>
      inventoryAfter.availableQuantity < inventoryBefore.availableQuantity
      || inventoryAfter.reservedQuantity > inventoryBefore.reservedQuantity
  });

  // AND the query/read model converges
  const queryOrderRes = pollUntil(
    () => queryApi.get(buildQueryOrderPath(order.id)),
    (response) => response && response.status === 200 && response.json().status === 'CONFIRMED',
    pollTimeoutMs,
    pollIntervalMs
  );

  checkStatus(queryOrderRes, 200, 'fetch query order');
  checkOrderState(queryOrderRes.json(), { status: 'CONFIRMED' }, 'query order');
}
