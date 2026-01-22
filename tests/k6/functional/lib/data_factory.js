import { makeCorrelationId } from './correlation.js';

export function buildProductPayload({ productId, name, category, price, currency } = {}) {
  return {
    id: productId,
    name: name || 'Functional Test Product',
    category: category || 'testing',
    price: price || 25.5,
    currency: currency || 'USD',
    description: 'Functional test product for k6',
    imageUrl: 'https://example.com/product.png'
  };
}

export function buildCartItem(productId, quantity) {
  return {
    productId,
    quantity
  };
}

export function buildOrderRequest({ amount, currency, customerId } = {}) {
  return {
    amount: amount || 50,
    currency: currency || 'USD',
    customerId: customerId || `cust-${makeCorrelationId()}`
  };
}

export function buildPaymentRequest(orderId, { amount, currency, forceOutcome } = {}) {
  return {
    orderId,
    amount: amount || 50,
    currency: currency || 'USD',
    forceOutcome
  };
}
