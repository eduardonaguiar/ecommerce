import crypto from 'k6/crypto';
import encoding from 'k6/encoding';

function randomSuffix() {
  return encoding.hexEncode(crypto.randomBytes(4));
}

export function buildOrderRequest(overrides = {}) {
  return {
    amount: overrides.amount || 42.5,
    currency: overrides.currency || 'USD',
    customerId: overrides.customerId || `cust-${randomSuffix()}`
  };
}

export function buildPaymentRequest(orderId, overrides = {}) {
  return {
    orderId,
    amount: overrides.amount || 42.5,
    currency: overrides.currency || 'USD',
    forceOutcome: overrides.forceOutcome
  };
}

export function buildProductRequest(overrides = {}) {
  const suffix = randomSuffix();
  return {
    id: overrides.id,
    name: overrides.name || `Test Product ${suffix}`,
    category: overrides.category || 'k6-seed',
    price: overrides.price || 19.99,
    currency: overrides.currency || 'USD',
    description: overrides.description || 'Seeded by k6 test data script.',
    imageUrl: overrides.imageUrl || `https://picsum.photos/seed/${suffix}/400/400`
  };
}
