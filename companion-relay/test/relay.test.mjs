import { test } from 'node:test';
import assert from 'node:assert/strict';
import { randomBytes, createHash, createCipheriv, createDecipheriv } from 'node:crypto';

const origin = process.env.RELAY_TEST_URL ?? 'http://127.0.0.1:8799';
const token = () => randomBytes(32).toString('hex');
const hash = value => createHash('sha256').update(value).digest('hex');
test('relay keeps ciphertext opaque, separates permissions, rejects replay and revokes', async () => {
  const writer = token(), reader = token(), decision = token(), room = hash(writer);
  const key = randomBytes(32), iv = randomBytes(12);
  const plaintext = JSON.stringify({ snapshot: { usedSeconds: 42 }, sequence: 1 });
  const cipher = createCipheriv('aes-256-gcm', key, iv);
  cipher.setAAD(Buffer.from(`kvieta-relay-v1\n${room}`));
  const ciphertext = Buffer.concat([cipher.update(plaintext), cipher.final()]);
  const box = Buffer.concat([iv, ciphertext, cipher.getAuthTag()]).toString('base64');
  const body = { box, sequence: 1, readHash: hash(reader), decisionHash: hash(decision) };
  const call = (method, auth, data, suffix = '') => fetch(`${origin}/v1/rooms/${room}${suffix}`, {
    method, headers: { Authorization: `Bearer ${auth}`, 'Content-Type': 'application/json' },
    body: data === undefined ? undefined : JSON.stringify(data)
  });
  assert.equal((await call('PUT', reader, body)).status, 403);
  assert.equal((await call('PUT', writer, body)).status, 200);
  assert.equal((await call('GET', token())).status, 403);
  assert.equal((await call('GET', writer)).status, 403);
  const response = await call('GET', reader);
  assert.equal(response.status, 200);
  assert.equal(response.headers.get('cache-control'), 'no-store');
  const data = await response.json();
  assert.deepEqual(data, { box, sequence: 1 });
  const packed = Buffer.from(data.box, 'base64');
  const decipher = createDecipheriv('aes-256-gcm', key, packed.subarray(0, 12));
  decipher.setAAD(Buffer.from(`kvieta-relay-v1\n${room}`));
  decipher.setAuthTag(packed.subarray(-16));
  assert.equal(Buffer.concat([decipher.update(packed.subarray(12, -16)), decipher.final()]).toString(), plaintext);
  const decisionBox = randomBytes(64).toString('base64');
  assert.equal((await call('PUT', reader, { box: decisionBox, sequence: 2 }, '/decision')).status, 403);
  assert.equal((await call('PUT', decision, { box: decisionBox, sequence: 2 }, '/decision')).status, 200);
  assert.equal((await call('GET', reader, undefined, '/decision')).status, 403);
  const savedDecision = await call('GET', writer, undefined, '/decision');
  assert.equal(savedDecision.status, 200);
  assert.deepEqual(await savedDecision.json(), { box: decisionBox, sequence: 2 });
  assert.equal((await call('PUT', decision, { box: decisionBox, sequence: 2 }, '/decision')).status, 200);
  assert.equal((await call('PUT', decision, { box: randomBytes(64).toString('base64'), sequence: 2 }, '/decision')).status, 409);
  assert.equal((await call('PUT', writer, body)).status, 409);
  assert.equal((await call('PUT', writer, { ...body, sequence: 2 })).status, 429);
  assert.equal((await call('DELETE', reader)).status, 403);
  assert.equal((await call('DELETE', writer)).status, 200);
  assert.equal((await call('GET', reader)).status, 410);
  assert.equal((await call('PUT', writer, { ...body, sequence: 2 })).status, 403);
});
test('relay rejects missing auth, malformed envelopes and oversized uploads', async () => {
  const writer = token(), room = hash(writer);
  const url = `${origin}/v1/rooms/${room}`;
  assert.equal((await fetch(url)).status, 401);
  const headers = { Authorization: `Bearer ${writer}`, 'Content-Type': 'application/json' };
  assert.equal((await fetch(url, { method: 'PUT', headers, body: 'null' })).status, 400);
  assert.equal((await fetch(url, { method: 'PUT', headers, body: 'x'.repeat(18000) })).status, 413);
  assert.equal((await fetch(url + '?token=hidden', { headers })).status, 404);
});
