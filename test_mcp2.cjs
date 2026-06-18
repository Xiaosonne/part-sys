const { spawn } = require('child_process');
const path = require('path');

const mcpPath = 'E:\\NodeNPM\\node_global\\node_modules\\chrome-devtools-mcp\\build\\src\\bin\\chrome-devtools-mcp.js';
const mcp = spawn('node', [mcpPath, '--headless'], {
  stdio: ['pipe', 'pipe', 'pipe'],
  timeout: 20000
});

let buf = '';
let responded = false;

mcp.stdout.on('data', (data) => {
  const text = data.toString();
  // Skip warning lines (start with non-JSON text)
  if (!text.startsWith('{')) return;
  
  buf += text;
  const lines = buf.split('\n');
  for (const line of lines) {
    if (!line.trim()) continue;
    try {
      const parsed = JSON.parse(line.trim());
      if (parsed.id === 1 && parsed.result) {
        console.log('SUCCESS! Tools:', parsed.result.tools.length);
        parsed.result.tools.slice(0, 15).forEach(t => console.log('  - ' + t.name));
        if (parsed.result.tools.length > 15) console.log('  ... and ' + (parsed.result.tools.length - 15) + ' more');
        responded = true;
        mcp.kill();
        process.exit(0);
      }
    } catch (e) {}
  }
});

mcp.stderr.on('data', (data) => {
  const text = data.toString();
  const lines = text.split('\n').filter(l => l.trim());
  lines.forEach(l => console.log('ERR:', l.substring(0, 200)));
});

mcp.on('error', (err) => {
  console.log('SPAWN ERROR:', err.message);
  if (!responded) { console.log('NO RESPONSE'); process.exit(1); }
});

setTimeout(() => {
  if (!responded) {
    console.log('Sending tools/list request...');
    const msg = JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'tools/list', params: {} }) + '\n';
    mcp.stdin.write(msg);
  }
}, 8000);

setTimeout(() => {
  if (!responded) {
    console.log('TIMEOUT');
    console.log('Buffer:', buf.slice(0, 500));
    mcp.kill();
    process.exit(1);
  }
}, 25000);
