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
  return check(order, {
    [`${label} status`]: () => !expectations.status || order.status === expectations.status,
    [`${label} stockStatus`]: () => !expectations.stockStatus || order.stockStatus === expectations.stockStatus,
    [`${label} paymentStatus`]: () => !expectations.paymentStatus || order.paymentStatus === expectations.paymentStatus
  });
}

export function checkPaymentAttempt(attempt, expectations, label) {
  return check(attempt, {
    [`${label} payment status`]: () => attempt.status === expectations.status,
    [`${label} payment effective`]: () => attempt.effective === expectations.effective
  });
}
