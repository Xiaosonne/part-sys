import { spawn } from 'child_process';

const mcpPath = 'E:\\NodeNPM\\node_global\\node_modules\\chrome-devtools-mcp\\build\\src\\bin\\chrome-devtools-mcp.js';

const mcp = spawn('node', [mcpPath, '--headless'], {
  stdio: ['pipe', 'pipe', 'pipe'],
});

let stderrBuf = '';
mcp.stderr.on('data', d => { stderrBuf += d; });

function sendMCP(method, id, params = {}) {
  const body = JSON.stringify({ jsonrpc: '2.0', id, method, params });
  const header = `Content-Length: ${Buffer.byteLength(body, 'utf-8')}\r\n\r\n`;
  mcp.stdin.write(header + body);
}

function findResponse(buf, id) {
  // Parse MCP framed responses (Content-Length: N\r\n\r\n{...})
  const regex = /Content-Length:\s*(\d+)\s*\r?\n\r?\n([\s\S]*?)(?=\nContent-Length:|$)/g;
  let match;
  let results = [];
  while ((match = regex.exec(buf)) !== null) {
    try {
      const parsed = JSON.parse(match[2].trim());
      if (parsed.id === id) results.push(parsed);
    } catch(e) {}
  }
  return results;
}

let stdoutBuf = '';
mcp.stdout.on('data', d => {
  stdoutBuf += d.toString();
  const responses = findResponse(stdoutBuf, 1);
  if (responses.length > 0) {
    const result = responses[0];
    if (result.result && result.result.tools) {
      console.log('\\n=== Connected! Tools (' + result.result.tools.length + ') ===');
      const tools = result.result.tools;
      const categories = {};
      tools.forEach(t => {
        const cat = t.name.split('_')[0] || 'other';
        if (!categories[cat]) categories[cat] = [];
        categories[cat].push(t.name);
      });
      Object.entries(categories).forEach(([cat, names]) => {
        console.log(`  [${cat}] ${names.slice(0, 5).join(', ')}${names.length > 5 ? '...' : ''}`);
      });
      mcp.kill();
      process.exit(0);
    }
  }
});

setTimeout(() => {
  console.log('Connecting...');
  sendMCP('tools/list', 1);
}, 10000);

setTimeout(() => {
  console.log('\\n=== STDERR (warnings) ===');
  console.log(stderrBuf.slice(0, 500));
  console.log('\\n=== STDOUT (raw) ===');
  console.log(stdoutBuf.slice(0, 1000));
  mcp.kill();
  process.exit(1);
}, 25000);
