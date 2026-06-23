/**
 * 工作流问题诊断脚本
 * 检查待办任务为什么为空
 */

const { chromium } = require('playwright');

const BASE_URL = 'http://localhost:5173';
const API_BASE = 'http://localhost:5128/api';

async function debugWorkflow() {
  console.log('========================================');
  console.log('工作流问题诊断');
  console.log('========================================\n');

  // 1. 登录获取 token
  console.log('[1] 登录获取 token...');
  const loginRes = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'admin123' })
  });
  const loginData = await loginRes.json();

  if (!loginData.token) {
    console.error('  ✗ 登录失败:', JSON.stringify(loginData));
    return;
  }

  const token = loginData.token;
  const userId = loginData.user?.id;
  const username = loginData.user?.username;
  const role = loginData.user?.role;

  console.log(`  ✓ 登录成功`);
  console.log(`    userId: ${userId}`);
  console.log(`    username: ${username}`);
  console.log(`    role: ${role}`);
  console.log(`    token: ${token.substring(0, 30)}...`);

  // 2. 检查所有流程定义
  console.log('\n[2] 检查所有流程定义...');
  const defRes = await fetch(`${API_BASE}/workflows/definitions`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const defData = await defRes.json();
  const definitions = defData.data || defData || [];
  console.log(`  ✓ 找到 ${definitions.length} 个流程定义`);
  definitions.forEach(d => {
    console.log(`    - ${d.name} (ID: ${d.id}, Category: ${d.category})`);
  });

  // 3. 检查最新的流程定义详情
  if (definitions.length > 0) {
    const latestDef = definitions[definitions.length - 1];
    console.log(`\n[3] 检查最新流程定义详情: ${latestDef.name}`);
    const defDetailRes = await fetch(`${API_BASE}/workflows/definitions/${latestDef.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const defDetail = await defDetailRes.json();

    if (defDetail && defDetail.nodes) {
      console.log(`  节点数量: ${defDetail.nodes.length}`);
      defDetail.nodes.forEach(node => {
        console.log(`    - ${node.name} (Type: ${node.nodeType}, Approvers: ${JSON.stringify(node.approvers)})`);
      });
    }
  }

  // 4. 启动一个新的流程实例
  console.log('\n[4] 启动新的流程实例...');
  const startRes = await fetch(`${API_BASE}/workflows/instances`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      definitionId: definitions[definitions.length - 1]?.id,
      entityType: 'Project',
      entityId: '69b37ef9de39c2c443461d19',
      name: `诊断测试_${Date.now()}`,
      formData: {}
    })
  });
  const startData = await startRes.json();
  console.log(`  响应状态: ${startRes.status}`);
  console.log(`  实例ID: ${startData.id}`);
  console.log(`  错误: ${startData.message || '无'}`);

  // 等待处理
  await new Promise(r => setTimeout(r, 2000));

  // 5. 检查所有待办任务（admin应该能看到全部）
  console.log('\n[5] 检查所有待办任务 (as admin)...');
  const pendingRes = await fetch(`${API_BASE}/workflows/tasks/pending`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const pendingData = await pendingRes.json();
  const allTasks = Array.isArray(pendingData) ? pendingData : (pendingData.data || []);
  console.log(`  ✓ 找到 ${allTasks.length} 个待办任务`);
  allTasks.forEach(t => {
    console.log(`    - 任务ID: ${t.id}, 实例: ${t.instanceId}, 节点: ${t.nodeName}, 审批人: ${t.assigneeName || t.assigneeId}`);
  });

  // 6. 检查我发起的流程实例
  console.log('\n[6] 检查我发起的流程实例...');
  const instanceRes = await fetch(`${API_BASE}/workflows/instances`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const instanceData = await instanceRes.json();
  const instances = instanceData.data || instanceData || [];
  console.log(`  ✓ 找到 ${instances.length} 个流程实例`);
  instances.slice(-5).forEach(inst => {
    console.log(`    - ${inst.name} (状态: ${inst.status}, 当前节点: ${inst.currentNodeId})`);
  });

  // 7. 检查 WorkflowTasks 表直接（通过获取所有实例的任务）
  console.log('\n[7] 检查最新实例的任务...');
  if (startData.id) {
    const instanceDetailRes = await fetch(`${API_BASE}/workflows/instances/${startData.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const instanceDetail = await instanceDetailRes.json();
    console.log(`  实例: ${instanceDetail.name}`);
    console.log(`  状态: ${instanceDetail.status}`);
    console.log(`  当前节点: ${instanceDetail.currentNodeId}`);
    console.log(`  定义节点数: ${instanceDetail.definition?.nodes?.length || 0}`);
  }

  console.log('\n========================================');
  console.log('诊断完成');
  console.log('========================================');
}

debugWorkflow().catch(console.error);