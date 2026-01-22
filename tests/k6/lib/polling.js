import { sleep } from 'k6';

export function pollUntil(fetchFn, predicate, timeoutMs, intervalMs) {
  const start = Date.now();
  let lastResult = null;

  while (Date.now() - start < timeoutMs) {
    lastResult = fetchFn();
    if (predicate(lastResult)) {
      return lastResult;
    }

    sleep(intervalMs / 1000);
  }

  return lastResult;
}
