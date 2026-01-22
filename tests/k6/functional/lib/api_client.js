import http from 'k6/http';
import { getAuthHeaders } from './auth.js';

function normalizeBaseUrl(baseUrl) {
  return baseUrl.replace(/\/$/, '');
}

function normalizePath(path) {
  if (!path) {
    return '';
  }
  return path.startsWith('/') ? path : `/${path}`;
}

export function createApiClient({ baseUrl, correlationId }) {
  const normalizedBase = normalizeBaseUrl(baseUrl);

  function buildHeaders(additionalHeaders = {}) {
    return {
      'Content-Type': 'application/json',
      'X-Correlation-Id': correlationId,
      ...getAuthHeaders(),
      ...additionalHeaders
    };
  }

  function request(method, path, body, params = {}) {
    const url = `${normalizedBase}${normalizePath(path)}`;
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

  return {
    get: (path, params = {}) => request('GET', path, null, params),
    post: (path, body, params = {}) => request('POST', path, body, params),
    del: (path, params = {}) => request('DELETE', path, null, params),
    request
  };
}

export function buildQueryOrderPath(orderId) {
  const queryPath = __ENV.QUERY_ORDER_PATH || '/orders';
  const normalizedPath = normalizePath(queryPath);
  return `${normalizedPath}/${orderId}`;
}
