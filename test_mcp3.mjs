import { spawn } from 'child_process';
import { createInterface } from 'readline';

const mcpPath = 'E:\\NodeNPM\\node_global\\node_modules\\chrome-devtools-mcp\\build\\src\\bin\\chrome-devtools-mcp.js';

console.log('Starting MCP server...');
const mcp = spawn('node', [mcpPath, '--headless'], {
  stdio: ['pipe', 'pipe', 'pipe'],
});

const rl = createInterface({ input: mcp.stdout });
let responded = false;

rl.on('line', (line) => {
  if (!line.trim() || line.trim().startsWith('chrome-devtools-mcp')) return;
  try {
    const parsed = JSON.parse(line.trim());
    if (parsed.id === 1 && parsed.result) {
      console.log('\\n=== Available Tools (' + parsed.result.tools.length + ') ===');
      parsed.result.tools.forEach(t => console.log('  - ' + t.name));
      responded = true;
      mcp.kill();
      process.exit(0);
    }
  } catch (e) {}
});

mcp.stderr.on('data', d => {});

setTimeout(() => {
  if (!responded) {
    console.log('Sending tools/list...');
    const msg = JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'tools/list', params: {} }) + '\\n';
    mcp.stdin.write(msg);
  }
}, 10000);

setTimeout(() => {
  if (!responded) {
    console.log('TIMEOUT');
    mcp.kill();
    process.exit(1);
  }
}, 30000);
