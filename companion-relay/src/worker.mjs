import { DurableObject } from 'cloudflare:workers';

const hex = /^[a-f0-9]{64}$/;
const headers = { 'Cache-Control': 'no-store', 'Content-Type': 'application/json', 'X-Content-Type-Options': 'nosniff' };
const reply = (status, body = {}) => new Response(status === 204 ? null : JSON.stringify(body), { status, headers });
const hash = async value => [...new Uint8Array(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(value)))].map(v => v.toString(16).padStart(2, '0')).join('');

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (request.method === 'GET' && url.pathname === '/health') return reply(200, { service: 'kvieta-companion-relay', version: 2 });
    const match = url.pathname.match(/^\/v1\/rooms\/([a-f0-9]{64})(\/decision|\/fcm)?$/);
    if (url.search || !match || !['PUT', 'GET', 'DELETE', 'POST'].includes(request.method) || (match[2] && request.method === 'DELETE')) return reply(404);
    const token = request.headers.get('Authorization')?.replace(/^Bearer /, '') ?? '';
    if (!hex.test(token)) return reply(401);
    const ip = request.headers.get('CF-Connecting-IP') ?? 'local';
    if (!(await env.REQUEST_LIMIT.limit({ key: ip })).success) return reply(429);
    const room = match[1], decision = match[2] === '/decision', fcm = match[2] === '/fcm';
    const tokenHash = await hash(token);
    // One bounded preview mailbox avoids unbounded creation of objects by anonymous callers.
    const mailbox = env.MAILBOX.getByName('preview-v1');
    if (decision && request.method === 'GET') {
      if (tokenHash !== room) return reply(403);
      return mailbox.readDecision(room);
    }
    if (!decision && !fcm && request.method === 'GET') return mailbox.read(room, tokenHash);
    if (!decision && !fcm && tokenHash !== room) return reply(403);
    if (request.method === 'DELETE') return mailbox.revoke(room);
    if (Number(request.headers.get('Content-Length') ?? 0) > 16384) return reply(413);
    let total = 0;
    const chunks = [];
    if (!request.body) return reply(400);
    const reader = request.body.getReader();
    while (true) {
      const { value, done } = await reader.read();
      if (done) break;
      total += value.length;
      if (total > 16384) { await reader.cancel(); return reply(413); }
      chunks.push(value);
    }
    const bytes = new Uint8Array(total);
    let offset = 0;
    for (const chunk of chunks) { bytes.set(chunk, offset); offset += chunk.length; }
    let body;
    try { body = JSON.parse(new TextDecoder().decode(bytes)); } catch { return reply(400); }
    if (fcm) {
      if (request.method !== 'POST') return reply(405);
      return mailbox.registerFcm(room, tokenHash, body?.token);
    }
    if (decision) return mailbox.writeDecision(room, tokenHash, body);
    if (!body || !hex.test(body.readHash) || !hex.test(body.decisionHash) || typeof body.box !== 'string' ||
        body.box.length < 40 || body.box.length > 12000 || !/^[A-Za-z0-9+/]+={0,2}$/.test(body.box) ||
        !Number.isSafeInteger(body.sequence) || body.sequence < 1) return reply(400);
    return mailbox.write(room, body);
  }
};

async function sendFcmNotification(env, fcmToken, roomId) {
  if (!env?.FIREBASE_SERVICE_ACCOUNT || !fcmToken) return;
  try {
    const sa = typeof env.FIREBASE_SERVICE_ACCOUNT === 'string'
      ? JSON.parse(env.FIREBASE_SERVICE_ACCOUNT)
      : env.FIREBASE_SERVICE_ACCOUNT;
    const now = Math.floor(Date.now() / 1000);
    const header = btoa(JSON.stringify({ alg: 'RS256', typ: 'JWT' })).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
    const claim = btoa(JSON.stringify({
      iss: sa.client_email,
      scope: 'https://www.googleapis.com/auth/firebase.messaging',
      aud: 'https://oauth2.googleapis.com/token',
      exp: now + 3600,
      iat: now
    })).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
    const input = `${header}.${claim}`;

    const pem = sa.private_key.replace(/-----[^\n]+-----/g, '').replace(/\s+/g, '');
    const binaryKey = Uint8Array.from(atob(pem), c => c.charCodeAt(0));
    const cryptoKey = await crypto.subtle.importKey(
      'pkcs8',
      binaryKey.buffer,
      { name: 'RSASSA-PKCS1-v1_5', hash: 'SHA-256' },
      false,
      ['sign']
    );
    const signature = await crypto.subtle.sign('RSASSA-PKCS1-v1_5', cryptoKey, new TextEncoder().encode(input));
    const sigBase64 = btoa(String.fromCharCode(...new Uint8Array(signature))).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
    const jwt = `${input}.${sigBase64}`;

    const tokenRes = await fetch('https://oauth2.googleapis.com/token', {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'urn:ietf:params:oauth:grant-type:jwt-bearer',
        assertion: jwt
      })
    });
    const tokenData = await tokenRes.json();
    if (!tokenData?.access_token) return;

    await fetch(`https://fcm.googleapis.com/v1/projects/${sa.project_id}/messages:send`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${tokenData.access_token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        message: {
          token: fcmToken,
          data: { room: roomId, type: 'time_request' },
          android: { priority: 'HIGH' }
        }
      })
    });
  } catch (err) {
    console.error('FCM send failure:', err);
  }
}

export class Mailbox extends DurableObject {
  constructor(ctx, env) {
    super(ctx, env);
    this.env = env;
    this.sql = ctx.storage.sql;
    this.sql.exec('CREATE TABLE IF NOT EXISTS rooms (id TEXT PRIMARY KEY, reader TEXT NOT NULL, decision_reader TEXT, box TEXT, sequence INTEGER NOT NULL, updated INTEGER NOT NULL, revoked INTEGER NOT NULL DEFAULT 0, decision_box TEXT, decision_sequence INTEGER, decision_updated INTEGER, fcm_token TEXT)');
    for (const column of ['decision_reader TEXT', 'decision_box TEXT', 'decision_sequence INTEGER', 'decision_updated INTEGER', 'fcm_token TEXT']) {
      try { this.sql.exec(`ALTER TABLE rooms ADD COLUMN ${column}`); } catch { /* Existing column. */ }
    }
    this.sql.exec('CREATE TABLE IF NOT EXISTS budget (day TEXT PRIMARY KEY, count INTEGER NOT NULL)');
  }
  async write(id, body) {
    const now = Date.now();
    // SQL work is synchronous; no await between checking sequence and replacing data.
    this.sql.exec('DELETE FROM rooms WHERE updated < ?', now - 7 * 86400000);
    const old = this.sql.exec('SELECT * FROM rooms WHERE id = ?', id).toArray()[0];
    if (old?.revoked || old && (old.reader !== body.readHash || old.decision_reader && old.decision_reader !== body.decisionHash)) return reply(403);
    if (old && body.sequence <= old.sequence) return reply(409);
    if (old && now - old.updated < 45000) return reply(429);
    if (!old) {
      const day = new Date(now).toISOString().slice(0, 10);
      this.sql.exec('DELETE FROM budget WHERE day <> ?', day);
      const count = this.sql.exec('SELECT count FROM budget WHERE day = ?', day).toArray()[0]?.count ?? 0;
      const rooms = this.sql.exec('SELECT COUNT(*) AS count FROM rooms WHERE revoked=0').one().count;
      if (count >= 50 || rooms >= 25) return reply(503, { error: 'preview_capacity' });
      this.sql.exec('INSERT INTO budget(day,count) VALUES(?,1) ON CONFLICT(day) DO UPDATE SET count=count+1', day);
    }
    this.sql.exec('INSERT INTO rooms(id,reader,decision_reader,box,sequence,updated) VALUES(?,?,?,?,?,?) ON CONFLICT(id) DO UPDATE SET decision_reader=excluded.decision_reader,box=excluded.box,sequence=excluded.sequence,updated=excluded.updated', id, body.readHash, body.decisionHash, body.box, body.sequence, now);
    if (await this.ctx.storage.getAlarm() === null) await this.ctx.storage.setAlarm(now + 86400000);
    const target = this.sql.exec('SELECT fcm_token FROM rooms WHERE id = ?', id).toArray()[0];
    if (target?.fcm_token) {
      this.ctx.waitUntil(sendFcmNotification(this.env, target.fcm_token, id));
    }
    return reply(200);
  }
  registerFcm(id, reader, fcmToken) {
    if (!fcmToken || typeof fcmToken !== 'string' || fcmToken.length < 20 || fcmToken.length > 500) return reply(400);
    const row = this.sql.exec('SELECT * FROM rooms WHERE id = ?', id).toArray()[0];
    if (!row || row.reader !== reader || row.revoked) return reply(403);
    this.sql.exec('UPDATE rooms SET fcm_token = ? WHERE id = ?', fcmToken, id);
    return reply(200);
  }
  read(id, reader) {
    const row = this.sql.exec('SELECT * FROM rooms WHERE id = ?', id).toArray()[0];
    if (!row || row.reader !== reader) return reply(403);
    if (row.revoked) return reply(410);
    if (Date.now() - row.updated > 86400000) return reply(204);
    return reply(200, { box: row.box, sequence: row.sequence });
  }
  writeDecision(id, decisionReader, body) {
    const row = this.sql.exec('SELECT * FROM rooms WHERE id = ?', id).toArray()[0];
    if (!row || row.revoked || row.decision_reader !== decisionReader) return reply(403);
    if (!body || typeof body.box !== 'string' || body.box.length < 40 || body.box.length > 4096 ||
        !/^[A-Za-z0-9+/]+={0,2}$/.test(body.box) || !Number.isSafeInteger(body.sequence) || body.sequence < 1) return reply(400);
    if (body.sequence === row.decision_sequence && body.box === row.decision_box) return reply(200);
    if (row.decision_sequence && body.sequence <= row.decision_sequence) return reply(409);
    this.sql.exec('UPDATE rooms SET decision_box=?, decision_sequence=?, decision_updated=? WHERE id=?', body.box, body.sequence, Date.now(), id);
    return reply(200);
  }
  readDecision(id) {
    const row = this.sql.exec('SELECT * FROM rooms WHERE id = ?', id).toArray()[0];
    if (!row || row.revoked) return reply(row?.revoked ? 410 : 403);
    if (!row.decision_box || Date.now() - row.decision_updated > 3600000) return reply(204);
    return reply(200, { box: row.decision_box, sequence: row.decision_sequence });
  }
  async revoke(id) {
    const existing = this.sql.exec('SELECT revoked FROM rooms WHERE id = ?', id).toArray()[0];
    if (existing?.revoked) return reply(200);
    if (!existing) {
      const day = new Date().toISOString().slice(0, 10);
      this.sql.exec('DELETE FROM budget WHERE day <> ?', day);
      const count = this.sql.exec('SELECT count FROM budget WHERE day = ?', day).toArray()[0]?.count ?? 0;
      if (count >= 50 || this.sql.exec('SELECT COUNT(*) AS count FROM rooms').one().count >= 500) return reply(503);
      this.sql.exec('INSERT INTO budget(day,count) VALUES(?,1) ON CONFLICT(day) DO UPDATE SET count=count+1', day);
    }
    // Keep a tombstone so an in-flight old upload cannot recreate a revoked room.
    this.sql.exec("INSERT INTO rooms(id,reader,box,sequence,updated,revoked) VALUES(?,'',NULL,0,?,1) ON CONFLICT(id) DO UPDATE SET box=NULL,revoked=1,updated=excluded.updated", id, Date.now());
    if (await this.ctx.storage.getAlarm() === null) await this.ctx.storage.setAlarm(Date.now() + 86400000);
    return reply(200);
  }
  async alarm() {
    this.sql.exec('DELETE FROM rooms WHERE updated < ?', Date.now() - 7 * 86400000);
    if (this.sql.exec('SELECT COUNT(*) AS count FROM rooms').one().count > 0) await this.ctx.storage.setAlarm(Date.now() + 86400000);
  }
}
