import { spawn } from 'child_process';

const mcpPath = 'E:\\NodeNPM\\node_global\\node_modules\\chrome-devtools-mcp\\build\\src\\bin\\chrome-devtools-mcp.js';

// Start with headless, but also use a temporary user data dir
const mcp = spawn('node', [
  mcpPath,
  '--headless',
  '--isolated'
], {
  stdio: ['pipe', 'pipe', 'pipe'],
});

let stderrBuf = '';
mcp.stderr.on('data', d => { stderrBuf += d; });

let stdoutBuf = '';
mcp.stdout.on('data', d => { 
  const text = d.toString();
  stdoutBuf += text;
  console.log('DATA:', JSON.stringify(text.slice(0, 200)));
});

mcp.on('error', (err) => console.log('ERR:', err.message));

mcp.on('exit', (code) => console.log('EXIT:', code));

function send(id, method, params = {}) {
  const body = JSON.stringify({ jsonrpc: '2.0', id, method, params });
  const len = Buffer.byteLength(body, 'utf-8');
  const header = `Content-Length: ${len}\r\n\r\n`;
  console.log('SEND:', method, id);
  mcp.stdin.write(header + body);
}

// Wait for initialization, then send tools/list
setTimeout(() => {
  console.log('=== STDERR ===');
  console.log(stderrBuf.slice(0, 500));
  console.log('=== BUFFER ===');
  console.log(JSON.stringify(stdoutBuf));
  send(1, 'tools/list');
}, 12000);

setTimeout(() => {
  console.log('=== FINAL STDOUT ===');
  console.log(stdoutBuf.slice(0, 2000));
  console.log('=== DONE ===');
  mcp.kill();
  process.exit(1);
}, 25000);
