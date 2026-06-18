const { spawn } = require('child_process');
const path = require('path');
const mcpPath = path.join(process.env.USERPROFILE, 'AppData', 'Roaming', 'npm', 'node_modules', 'chrome-devtools-mcp', 'build', 'src', 'bin', 'chrome-devtools-mcp.js');
console.log('Path:', mcpPath);
const mcp = spawn('node', [mcpPath, '--headless'], { stdio: ['pipe', 'pipe', 'pipe'] });
let output = '';
mcp.stdout.on('data', d => { output += d.toString(); });
mcp.stderr.on('data', d => { /* ignore warnings */ });
mcp.on('error', e => console.error('ERR:', e.message));
setTimeout(() => {
  mcp.stdin.write(JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'tools/list', params: {} }) + '\n');
}, 5000);
setTimeout(() => {
  const lines = output.split('\n');
  for (const l of lines) {
    try {
      const p = JSON.parse(l.trim());
      if (p.id === 1 && p.result) {
        console.log('OK: ' + p.result.tools.length + ' tools');
        p.result.tools.forEach(t => console.log('  ' + t.name));
        mcp.kill();
        process.exit(0);
      }
    } catch (_) {}
  }
  console.log('RAW:', output.slice(0, 1000));
  mcp.kill();
  process.exit(1);
}, 15000);
