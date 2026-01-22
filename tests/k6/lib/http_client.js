import http from 'k6/http';
import config from './config.js';
import { getAuthHeaders } from './auth.js';

function buildHeaders(additionalHeaders = {}) {
  return {
    'Content-Type': 'application/json',
    'X-Correlation-Id': config.correlationId,
    ...getAuthHeaders(),
    ...additionalHeaders
  };
}

export function request(method, url, body, params = {}) {
  const headers = buildHeaders(params.headers);
  const requestParams = {
    ...params,
    headers
  };

  if (body === undefined || body === null) {
    return http.request(method, url, null, requestParams);
  }

  return http.request(method, url, JSON.stringify(body), requestParams);
}

export function get(url, params = {}) {
  return request('GET', url, null, params);
}

export function post(url, body, params = {}) {
  return request('POST', url, body, params);
}
