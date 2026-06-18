import { spawn } from 'child_process';

const mcpPath = 'E:\\NodeNPM\\node_global\\node_modules\\chrome-devtools-mcp\\build\\src\\bin\\chrome-devtools-mcp.js';

const mcp = spawn('node', [mcpPath, '--headless', '--isolated'], {
  stdio: ['pipe', 'pipe', 'pipe'],
});

let stdoutBuf = '';
let stderrBuf = '';

mcp.stdout.on('data', d => { stdoutBuf += d; });
mcp.stderr.on('data', d => { stderrBuf += d; });
mcp.on('exit', (c) => console.log('EXIT CODE:', c));

function send(id, method, params = {}) {
  const body = JSON.stringify({ jsonrpc: '2.0', id, method, params });
  const len = Buffer.byteLength(body, 'utf-8');
  mcp.stdin.write(`Content-Length: ${len}\r\n\r\n${body}`);
  console.log('Sent:', method, 'id:', id);
}

// Wait for server init
setTimeout(() => {
  console.log('STDERR (preview):', stderrBuf.slice(0, 300).replace(/\n/g, ' | '));
  send(1, 'tools/list');
}, 12000);

// Read response
setTimeout(() => {
  console.log('\\n=== STDOUT ===');
  console.log(stdoutBuf.slice(0, 3000));
  console.log('\\n=== STDERR (end) ===');
  console.log(stderrBuf.slice(-300));
  
  // Try to parse response
  const match = stdoutBuf.match(/Content-Length:\s*(\d+)\s*\r?\n\r?\n([\s\S]*?)\r?\n(?=Content-Length:|$)/);
  if (match) {
    try {
      const parsed = JSON.parse(match[2].trim());
      console.log('\\nPARSED:', JSON.stringify(parsed).slice(0, 500));
    } catch(e) {
      console.log('Parse error:', e.message);
    }
  }
  
  mcp.kill();
  process.exit(0);
}, 25000);
