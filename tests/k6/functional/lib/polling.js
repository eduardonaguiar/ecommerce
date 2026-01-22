import { sleep } from 'k6';

export function pollUntil(requestFn, predicate, timeoutMs, intervalMs) {
  const start = Date.now();
  let response = null;

  while (Date.now() - start < timeoutMs) {
    response = requestFn();
    if (predicate(response)) {
      return response;
    }
    sleep(intervalMs / 1000);
  }

  return response;
}
