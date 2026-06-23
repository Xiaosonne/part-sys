/**
 * 工作流回归测试脚本 (API 版本)
 *
 * 测试流程：
 * 1. 创建流程定义（包含项目选择、项目文件选择字段）
 * 2. 启动流程并填写表单（选择项目和文件）
 * 3. 完成流程审批
 * 4. 验证流程详情页面的 projectid 和 projectfiles
 *
 * 运行: node tests/workflow-regression-test.js
 */

const API_BASE = 'http://localhost:5128/api';
const TEST_WORKFLOW_PREFIX = '回归测试流程_';

let token = null;
let definitionId = null;
let instanceId = null;
let testProjectId = null;
let testFileIds = [];

// 登录获取 token
async function login() {
  console.log('=== 登录 ===');
  const loginRes = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'admin123' })
  });
  const loginData = await loginRes.json();
  token = loginData.data?.token || loginData.token;
  if (!token) throw new Error('登录失败，无法获取 token');
  console.log('✓ 登录成功');
}

// 获取用户列表
async function getUsers() {
  const res = await fetch(`${API_BASE}/users`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const data = await res.json();
  return (data.data || data || []).filter(u => u.username === 'admin');
}

// 获取项目列表
async function getProjects() {
  const res = await fetch(`${API_BASE}/projects`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const data = await res.json();
  return (data.data || data || []).filter(p => p.type === 'project');
}

// 获取项目的文件列表
async function getProjectFiles(projectId) {
  // 使用 files/project API 获取实际文件
  const res = await fetch(`${API_BASE}/files/project/${projectId}`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const text = await res.text();
  try {
    const data = JSON.parse(text);
    const files = data.data || data || [];
    // 过滤出有有效 id 的文件
    return files.filter(f => f.id);
  } catch (e) {
    console.log('  文件列表解析失败:', text.substring(0, 100));
    return [];
  }
}

// 创建流程定义
async function createWorkflowDefinition() {
  console.log('\n=== 创建流程定义 ===');

  const users = await getUsers();
  const approverId = users[0]?.id;
  console.log('  审批人 ID:', approverId);

  const workflowDef = {
    name: TEST_WORKFLOW_PREFIX + Date.now(),
    description: '回归测试流程 - 包含项目/项目文件字段',
    category: '测试',
    entityType: '',
    version: 1,
    isActive: true,
    startConfig: {
      requireEntity: false,
      entityType: 'Project',
      requireFiles: false,
      minFileCount: 0,
      maxFileCount: 0,
      allowedFileTypes: [],
      formFields: []
    },
    nodes: [
      {
        id: 'node_start',
        name: '开始',
        nodeType: 'Start',
        nextNodes: ['node_approval'],
        formFields: [
          {
            id: 'field_project',
            label: '项目',
            key: 'projectid',
            fieldType: 'project',
            placeholder: '',
            required: true,
            options: [],
            entityType: 'Project',
            entitySourceKey: '',
            allowedFileTypes: []
          },
          {
            id: 'field_projectfile',
            label: '项目文件',
            key: 'projectfiles',
            fieldType: 'projectFile',
            placeholder: '',
            required: true,
            options: [],
            entityType: '',
            entitySourceKey: 'projectid',
            allowedFileTypes: []
          }
        ]
      },
      {
        id: 'node_approval',
        name: '审批',
        nodeType: 'SingleApproval',
        approvers: [approverId],
        timeoutMinutes: 60,
        nextNodes: ['node_end'],
        formFields: []
      },
      {
        id: 'node_end',
        name: '结束',
        nodeType: 'End',
        nextNodes: []
      }
    ]
  };

  const res = await fetch(`${API_BASE}/workflows/definitions`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(workflowDef)
  });

  const data = await res.json();
  if (!res.ok) throw new Error('创建流程定义失败: ' + JSON.stringify(data));

  definitionId = data.id;
  console.log('✓ 流程定义创建成功, ID:', definitionId);
  return definitionId;
}

// 启动流程实例（带项目和文件）
async function startWorkflowInstance(projectId, fileIds) {
  console.log('\n=== 启动流程实例 ===');
  console.log('  项目 ID:', projectId);
  console.log('  文件 IDs:', fileIds);

  const startData = {
    definitionId,
    entityType: 'Project',
    entityId: projectId,
    selectedFileIds: fileIds,
    name: '回归测试流程实例',
    formData: {
      projectid: projectId,
      projectfiles: fileIds
    }
  };

  const res = await fetch(`${API_BASE}/workflows/instances`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(startData)
  });

  const data = await res.json();
  if (!res.ok) throw new Error('启动流程失败: ' + JSON.stringify(data));

  instanceId = data.id;
  console.log('✓ 流程实例启动成功, ID:', instanceId);
  console.log('  状态:', data.status);

  // 验证 formData
  if (data.formData) {
    console.log('  formData.projectid:', data.formData.projectid);
    console.log('  formData.projectfiles:', JSON.stringify(data.formData.projectfiles));
  }

  return instanceId;
}

// 审批流程
async function approveWorkflow() {
  console.log('\n=== 执行审批 ===');

  // 获取待办任务
  const tasksRes = await fetch(`${API_BASE}/workflows/tasks/pending`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const tasksData = await tasksRes.json();
  const tasks = tasksData.data || tasksData || [];
  const myTask = tasks.find(t => t.instanceId === instanceId);

  if (!myTask) {
    const instanceRes = await fetch(`${API_BASE}/workflows/instances/${instanceId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const instance = await instanceRes.json();
    console.log('  当前流程状态:', instance.status);
    console.log('  当前节点:', instance.currentNodeName);
    if (instance.status === 'Completed') {
      console.log('  流程已完成，无需审批');
      return;
    }
    throw new Error('未找到待办任务');
  }

  console.log('  找到待办任务, ID:', myTask.id, ', 节点:', myTask.nodeName);

  const approveRes = await fetch(`${API_BASE}/workflows/tasks/${myTask.id}/approve`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ comment: '回归测试审批通过', formData: {} })
  });

  if (!approveRes.ok) {
    const text = await approveRes.text();
    throw new Error('审批失败: ' + text);
  }
  console.log('✓ 审批成功');
}

// 验证流程详情
async function verifyWorkflowDetail() {
  console.log('\n=== 验证流程详情 ===');

  // 获取流程实例
  const res = await fetch(`${API_BASE}/workflows/instances/${instanceId}`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const instance = await res.json();
  const data = instance.data || instance;

  console.log('  流程名称:', data.name);
  console.log('  流程状态:', data.status);
  console.log('  当前节点:', data.currentNodeName);

  // 获取历史
  const historyRes = await fetch(`${API_BASE}/workflows/instances/${instanceId}/history`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const historyData = await historyRes.json();
  const history = historyData.data || historyData || [];

  console.log('  审批历史:');
  let allPassed = true;

  for (const h of history) {
    console.log(`    - ${h.nodeName}: ${h.action} by ${h.operatorName || h.operatorId}`);
    if (h.formData && Object.keys(h.formData).length > 0) {
      console.log(`      表单数据:`, JSON.stringify(h.formData));

      // 验证 Start 节点的表单数据
      if (h.nodeName === '开始' || h.nodeType === 'Start') {
        const formData = h.formData;
        const expectedProjectId = testProjectId;
        const expectedFileIds = testFileIds;

        console.log('\n  === 验证表单数据 ===');
        console.log('  预期 projectid:', expectedProjectId);
        console.log('  实际 projectid:', formData.projectid);
        console.log('  预期 projectfiles:', JSON.stringify(expectedFileIds));
        console.log('  实际 projectfiles:', JSON.stringify(formData.projectfiles));

        // 检查 projectid
        if (formData.projectid !== expectedProjectId) {
          console.log('  ❌ projectid 不匹配!');
          allPassed = false;
        } else {
          console.log('  ✓ projectid 匹配');
        }

        // 检查 projectfiles
        const actualFiles = formData.projectfiles || [];
        if (actualFiles.length !== expectedFileIds.length) {
          console.log(`  ❌ projectfiles 数量不匹配! 预期: ${expectedFileIds.length}, 实际: ${actualFiles.length}`);
          allPassed = false;
        } else {
          const filesMatch = actualFiles.every(f => expectedFileIds.includes(f));
          if (!filesMatch) {
            console.log('  ❌ projectfiles 内容不匹配!');
            allPassed = false;
          } else {
            console.log('  ✓ projectfiles 匹配');
          }
        }
      }
    }
  }

  return allPassed && data.status === 'Completed';
}

// 清理测试数据
async function cleanup() {
  if (!instanceId) return;
  console.log('\n=== 清理测试数据 ===');
  console.log('  跳过清理（保留测试数据供检查）');
}

// 主测试流程
async function runTest() {
  let success = false;

  try {
    console.log('========================================');
    console.log('工作流回归测试开始');
    console.log('========================================\n');

    await login();

    // 获取测试项目和文件
    const projects = await getProjects();
    testProjectId = projects[0]?.id;
    if (!testProjectId) {
      throw new Error('未找到测试项目');
    }
    console.log('  测试项目 ID:', testProjectId);

    // 获取项目文件
    const files = await getProjectFiles(testProjectId);
    console.log('  项目文件数量:', files.length);

    if (files.length > 0) {
      testFileIds = files.slice(0, 2).map(f => f.id);
      console.log('  测试文件 IDs:', testFileIds);
    } else {
      console.log('  警告: 项目没有文件，projectfiles 将为空数组');
      testFileIds = [];
    }

    // 创建流程定义
    await createWorkflowDefinition();

    // 启动流程（带项目和文件）
    await startWorkflowInstance(testProjectId, testFileIds);

    // 等待流程处理
    await new Promise(r => setTimeout(r, 2000));

    // 执行审批
    await approveWorkflow();

    // 等待审批处理
    await new Promise(r => setTimeout(r, 2000));

    // 验证流程详情
    const detailVerified = await verifyWorkflowDetail();

    console.log('\n========================================');
    console.log('测试结果汇总');
    console.log('========================================');
    console.log(`流程定义 ID: ${definitionId}`);
    console.log(`流程实例 ID: ${instanceId}`);
    console.log(`项目 ID: ${testProjectId}`);
    console.log(`文件 IDs: ${JSON.stringify(testFileIds)}`);
    console.log(`流程详情验证: ${detailVerified ? '✓ 通过' : '✗ 失败'}`);
    console.log(`\n${detailVerified ? '🎉 所有测试通过!' : '⚠️ 测试未通过'}`);

    success = detailVerified;

  } catch (error) {
    console.error('\n❌ 测试失败:', error.message);
  } finally {
    await cleanup();
    console.log('\n测试结束');
    process.exit(success ? 0 : 1);
  }
}

runTest();
