import { check } from 'k6';

export function checkStatus(response, expectedStatus, label) {
  const expected = Array.isArray(expectedStatus) ? expectedStatus : [expectedStatus];
  return check(response, {
    [`${label} status is ${expected.join(' or ')}`]: () => expected.includes(response.status)
  });
}

export function checkJson(response, label) {
  return check(response, {
    [`${label} returns json`]: () => response && response.headers['Content-Type'] && response.headers['Content-Type'].includes('application/json')
  });
}

export function checkOrderState(order, expectations, label) {
  const normalize = (value) => (typeof value === 'string' ? value.toUpperCase() : value);
  const normalized = {
    status: normalize(order.status),
    stockStatus: normalize(order.stockStatus),
    paymentStatus: normalize(order.paymentStatus)
  };
  return check(order, {
    [`${label} status`]: () => !expectations.status || normalized.status === normalize(expectations.status),
    [`${label} stockStatus`]: () => !expectations.stockStatus || normalized.stockStatus === normalize(expectations.stockStatus),
    [`${label} paymentStatus`]: () => !expectations.paymentStatus || normalized.paymentStatus === normalize(expectations.paymentStatus)
  });
}

export function checkPaymentAttempt(attempt, expectations, label) {
  return check(attempt, {
    [`${label} payment status`]: () => attempt.status === expectations.status,
    [`${label} payment effective`]: () => attempt.effective === expectations.effective
  });
}

export function checkCartItems(cart, expectedItems, label) {
  return check(cart, {
    [`${label} cart id`]: () => !!cart.cartId,
    [`${label} items match`]: () => {
      if (!Array.isArray(cart.items)) {
        return false;
      }
      if (cart.items.length !== expectedItems.length) {
        return false;
      }
      return expectedItems.every((expected) =>
        cart.items.some((item) => item.productId === expected.productId && item.quantity === expected.quantity)
      );
    }
  });
}
