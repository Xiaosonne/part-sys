import * as http from 'http';

const host = '127.0.0.1';
const port = 12306;
const path = '/mcp';
const sessionId = 'sess-' + Date.now();

function send(msg) {
  return new Promise((resolve, reject) => {
    const body = JSON.stringify(msg);
    const opts = {
      hostname: host, port, path: path + '?sessionId=' + sessionId,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json, text/event-stream',
        'Content-Length': Buffer.byteLength(body)
      }
    };
    const req = http.request(opts, res => {
      let data = '';
      res.on('data', c => data += c);
      res.on('end', () => {
        try { resolve(JSON.parse(data)); }
        catch { resolve(data); }
      });
    });
    req.on('error', reject);
    req.write(body);
    req.end();
  });
}

function openSSE() {
  return new Promise((resolve, reject) => {
    const req = http.get(`http://${host}:${port}${path}?sessionId=${sessionId}`, {
      headers: { 'Accept': 'text/event-stream' }
    }, res => {
      let buf = '';
      res.on('data', c => buf += c);
      res.on('end', () => resolve({ status: res.statusCode, body: buf }));
    });
    req.on('error', reject);
    req.setTimeout(5000, () => { req.destroy(); reject('SSE timeout'); });
  });
}

console.log('Session:', sessionId);

const sse = await openSSE();
console.log('SSE:', JSON.stringify(sse));

if (sse.status === 200) {
  const init = await send({
    jsonrpc: '2.0', id: 1, method: 'initialize',
    params: { protocolVersion: '2024-11-05', capabilities: {}, clientInfo: { name: 'codex', version: '1' } }
  });
  console.log('Init:', JSON.stringify(init));

  const tools = await send({ jsonrpc: '2.0', id: 2, method: 'tools/list' });
  console.log('Tools:', JSON.stringify(tools, null, 2));
}
