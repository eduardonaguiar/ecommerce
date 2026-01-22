import http from 'k6/http';
import { check } from 'k6';
import config from './lib/config.js';
import { getBearerToken } from './lib/auth.js';
import { checkStatus } from './lib/assertions.js';

export const options = {
  vus: 1,
  iterations: 1,
  insecureSkipTLSVerify: true,
  thresholds: {
    checks: ['rate>0.95'],
    http_req_failed: ['rate<0.1']
  }
};

export default function () {
  console.log(`correlationId=${config.correlationId}`);

  const healthRes = http.get(`${config.gatewayBaseUrl}/health/ready`, {
    headers: {
      'X-Correlation-Id': config.correlationId
    }
  });
  checkStatus(healthRes, 200, 'gateway tls health');

  const noAuthRes = http.get(`${config.gatewayBaseUrl}/catalog/products`, {
    headers: {
      'X-Correlation-Id': config.correlationId
    }
  });
  checkStatus(noAuthRes, [401, 403], 'gateway rejects missing jwt');

  const token = getBearerToken();
  if (token) {
    const authRes = http.get(`${config.gatewayBaseUrl}/catalog/products`, {
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Correlation-Id': config.correlationId
      }
    });

    checkStatus(authRes, 200, 'gateway allows jwt');
    check(authRes, {
      'gateway response json': () => authRes.headers['Content-Type'] && authRes.headers['Content-Type'].includes('application/json')
    });
  } else {
    console.warn('No E2E_JWT or JWT_HS256_SECRET provided; skipping authenticated gateway check.');
  }
}
