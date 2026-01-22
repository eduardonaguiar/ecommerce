import encoding from 'k6/encoding';
import crypto from 'k6/crypto';

function base64UrlEncode(data) {
  return encoding.b64encode(JSON.stringify(data), 'rawurl');
}

export function buildDevToken() {
  const secret = __ENV.JWT_HS256_SECRET;
  if (!secret) {
    return null;
  }

  const now = Math.floor(Date.now() / 1000);
  const issuer = __ENV.JWT_ISSUER || 'https://auth.local';
  const audience = __ENV.JWT_AUDIENCE || 'ecommerce-api';
  const payload = {
    iss: issuer,
    aud: audience,
    sub: __ENV.JWT_SUBJECT || 'k6-tester',
    iat: now,
    exp: now + Number(__ENV.JWT_TTL_SECONDS || 3600)
  };

  const header = { alg: 'HS256', typ: 'JWT' };
  const headerB64 = base64UrlEncode(header);
  const payloadB64 = base64UrlEncode(payload);
  const signature = crypto.hmac('sha256', `${headerB64}.${payloadB64}`, secret, 'binary');
  const signatureB64 = encoding.b64encode(signature, 'rawurl');

  return `${headerB64}.${payloadB64}.${signatureB64}`;
}

export function getBearerToken() {
  return __ENV.E2E_JWT || buildDevToken();
}

export function getAuthHeaders() {
  const token = getBearerToken();
  if (!token) {
    return {};
  }

  return {
    Authorization: `Bearer ${token}`
  };
}
