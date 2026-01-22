/*
 * Scenario: Cart Update Semantics
 * Preconditions:
 * - Gateway is reachable and authenticated (JWT provided).
 * - Catalog has at least one product (or admin upsert is available).
 * Expected final state:
 * - Cart reflects add/update/remove operations.
 * - Invalid quantities are rejected.
 */
import { check } from 'k6';
import { createApiClient } from './lib/api_client.js';
import { makeCorrelationId } from './lib/correlation.js';
import { requireBearerToken } from './lib/auth.js';
import { checkStatus, checkJson, checkCartItems } from './lib/assertions.js';
import { buildProductPayload, buildCartItem } from './lib/data_factory.js';

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

export default function () {
  requireBearerToken();
  const correlationId = makeCorrelationId();
  console.log(`correlationId=${correlationId}`);

  const api = createApiClient({ baseUrl: gatewayBaseUrl, correlationId });

  // GIVEN a shopper views the cart
  const cartId = `cart-${correlationId}`;
  const emptyCartRes = api.get(`/cart/carts/${cartId}`);
  checkStatus(emptyCartRes, 200, 'fetch empty cart');
  checkJson(emptyCartRes, 'fetch empty cart');

  // AND a product is available
  const productsRes = api.get('/catalog/products');
  checkStatus(productsRes, 200, 'browse products');

  const products = productsRes.json();
  let product = Array.isArray(products) && products.length > 0 ? products[0] : null;

  if (!product) {
    const productPayload = buildProductPayload({
      productId: `k6-${correlationId}`,
      name: 'Functional Cart Product'
    });
    const upsertRes = api.post('/catalog/admin/products', productPayload);
    checkStatus(upsertRes, 200, 'upsert catalog product');
    product = upsertRes.json();
  }

  // WHEN the shopper adds the product to the cart
  const addItemRes = api.post(`/cart/carts/${cartId}/items`, buildCartItem(product.id, 2));
  checkStatus(addItemRes, 200, 'add item to cart');
  checkCartItems(addItemRes.json(), [{ productId: product.id, quantity: 2 }], 'cart after add');

  // AND updates quantity
  const updateItemRes = api.post(`/cart/carts/${cartId}/items`, buildCartItem(product.id, 3));
  checkStatus(updateItemRes, 200, 'update cart quantity');
  checkCartItems(updateItemRes.json(), [{ productId: product.id, quantity: 3 }], 'cart after update');

  // AND removes the item via quantity zero
  const removeItemRes = api.post(`/cart/carts/${cartId}/items`, buildCartItem(product.id, 0));
  checkStatus(removeItemRes, 200, 'remove cart item with zero');
  checkCartItems(removeItemRes.json(), [], 'cart after remove');

  // THEN invalid quantities are rejected
  const invalidQuantityRes = api.post(`/cart/carts/${cartId}/items`, buildCartItem(product.id, -1));
  checkStatus(invalidQuantityRes, 400, 'reject negative quantity');
  check(invalidQuantityRes, {
    'reject negative quantity with error body': () => invalidQuantityRes.json().error !== undefined
  });
}
