import crypto from 'k6/crypto';
import encoding from 'k6/encoding';

export function makeCorrelationId() {
  const bytes = crypto.randomBytes(16);
  const hex = encoding.hexEncode(bytes);
  return `k6-${hex}`;
}
