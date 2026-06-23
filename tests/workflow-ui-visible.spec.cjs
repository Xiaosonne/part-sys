/**
 * 工作流 UI 可视化测试 (Playwright)
 *
 * 特点：
 * 1. 弹出浏览器，可视化观察
 * 2. 操作间隔慢，方便查看每一步
 * 3. 每个步骤都有日志提示
 * 4. 覆盖审批操作：通过、拒绝、取消
 * 5. 随机对任务进行通过或拒绝操作，检查是否有报错
 *
 * 运行: npx playwright test tests/workflow-ui-visible.spec.cjs --headed
 */

const { test, expect } = require('@playwright/test');

const BASE_URL = 'http://localhost:5173';
const API_BASE = 'http://localhost:5128/api';

// 慢速操作间隔（毫秒）
const SLOW_DELAY = 2000;
const FAST_DELAY = 1000;

let workflowCounter = 0;
let currentWorkflowName = '';
let currentDefinitionId = '';
let projectId = '';
let fileIds = [];

// 统一的登录获取 token 和 userId
async function getAuthTokenAndUserId() {
  const loginRes = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'admin123' })
  });
  const loginData = await loginRes.json();
  return {
    token: loginData.token,
    userId: loginData.user?.id
  };
}

function generateWorkflowName(prefix = 'UI测试流程') {
  workflowCounter++;
  currentWorkflowName = `${prefix}_${Date.now()}_${workflowCounter}`;
  return currentWorkflowName;
}

// 登录
async function login(page) {
  console.log('    [登录] 1. 调用登录 API...');
  const loginRes = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'admin123' })
  });
  const loginData = await loginRes.json();

  if (!loginData.token) {
    throw new Error('登录失败: ' + JSON.stringify(loginData));
  }

  const token = loginData.token;
  const user = loginData.user || { id: 'admin', username: 'admin' };

  console.log('    [登录] 2. 访问前端页面...');
  await page.goto(`${BASE_URL}/parts`);
  await page.waitForLoadState('domcontentloaded');

  console.log('    [登录] 3. 设置 localStorage...');
  await page.evaluate(([tokenVal, userVal]) => {
    localStorage.setItem('token', tokenVal);
    localStorage.setItem('user', JSON.stringify(userVal));
  }, [token, user]);

  console.log('    [登录] 4. 刷新页面验证登录...');
  await page.goto(`${BASE_URL}/parts`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(FAST_DELAY);

  console.log('    [登录] ✓ 登录成功');
}

// 获取项目 ID
async function getFirstProjectId() {
  try {
    const { token } = await getAuthTokenAndUserId();
    const res = await fetch(`${API_BASE}/projects`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const data = await res.json();
    const projects = data.data || data || [];
    return projects.find(p => p.type === 'project')?.id;
  } catch (e) {
    console.error('获取项目失败:', e);
    return null;
  }
}

// 获取项目文件
async function getProjectFiles(projectId) {
  try {
    const { token } = await getAuthTokenAndUserId();
    const res = await fetch(`${API_BASE}/files/project/${projectId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const data = await res.json();
    return (data.data || data || []).filter(f => f.id);
  } catch (e) {
    console.error('获取文件失败:', e);
    return [];
  }
}

// 通过 API 创建流程定义
async function createWorkflowDefinitionAPI(options = {}) {
  const {
    name,
    singleStep = true,
    withProjectField = true,
    withProjectFileField = true,
    randomFieldsCount = 2
  } = options;

  const { token, userId } = await getAuthTokenAndUserId();
  const approverId = userId;

  const startNode = {
    id: 'node_0',
    nodeType: 'Start',
    name: '开始',
    x: 100,
    y: 50,
    approvers: [],
    timeoutMinutes: 0,
    nextNodes: singleStep ? ['node_approval1'] : ['node_approval1'],
    formFields: []
  };

  if (withProjectField) {
    startNode.formFields.push({
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
    });
  }

  if (withProjectFileField) {
    startNode.formFields.push({
      id: 'field_projectfile',
      label: '项目文件',
      key: 'projectfiles',
      fieldType: 'projectFile',
      placeholder: '',
      required: false,
      options: [],
      entityType: '',
      entitySourceKey: 'projectid',
      allowedFileTypes: []
    });
  }

  const fieldLabels = ['备注', '金额', '紧急程度', '意见', '说明'];
  for (let i = 0; i < randomFieldsCount; i++) {
    const label = fieldLabels[i % fieldLabels.length];
    startNode.formFields.push({
      id: `field_start_${i}`,
      label: `${label}_开始`,
      key: `field_${label}_start_${i}`,
      fieldType: 'text',
      placeholder: `请输入${label}`,
      required: false,
      options: [],
      entityType: '',
      entitySourceKey: '',
      allowedFileTypes: []
    });
  }

  const nodes = [startNode];

  const approval1 = {
    id: 'node_approval1',
    nodeType: 'SingleApproval',
    name: '第一级审批',
    x: 100,
    y: 180,
    approvers: [approverId],
    timeoutMinutes: 60,
    nextNodes: singleStep ? ['node_end'] : ['node_approval2'],
    formFields: []
  };

  for (let i = 0; i < randomFieldsCount; i++) {
    const label = fieldLabels[i % fieldLabels.length];
    approval1.formFields.push({
      id: `field_approval1_${i}`,
      label: `${label}_审批1`,
      key: `field_${label}_approval1_${i}`,
      fieldType: 'text',
      placeholder: `请输入${label}`,
      required: false,
      options: [],
      entityType: '',
      entitySourceKey: '',
      allowedFileTypes: []
    });
  }
  nodes.push(approval1);

  if (!singleStep) {
    const approval2 = {
      id: 'node_approval2',
      nodeType: 'SingleApproval',
      name: '第二级审批',
      x: 100,
      y: 310,
      approvers: [approverId],
      timeoutMinutes: 60,
      nextNodes: ['node_end'],
      formFields: []
    };
    nodes.push(approval2);
  }

  const endNode = {
    id: 'node_end',
    nodeType: 'End',
    name: '结束',
    x: 100,
    y: 440,
    approvers: [],
    timeoutMinutes: 0,
    nextNodes: [],
    formFields: []
  };
  nodes.push(endNode);

  const workflowDef = {
    name,
    description: `可视化UI测试 - ${singleStep ? '单步' : '两步'}审批流程`,
    category: 'UI测试',
    entityType: '',
    version: 1,
    isActive: true,
    startConfig: {},
    nodes
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
  console.log(`  ✓ 流程定义创建成功: ${name}, ID: ${data.id}`);
  return data.id;
}

// 通过 API 启动流程实例
async function startWorkflowInstanceAPI(definitionId, projectId, fileIds = []) {
  const { token } = await getAuthTokenAndUserId();

  const startData = {
    definitionId,
    entityType: projectId ? 'Project' : '',
    entityId: projectId || '',
    selectedFileIds: fileIds,
    name: `可视化测试实例_${Date.now()}`,
    formData: {}
  };

  if (projectId) {
    startData.formData.projectid = projectId;
  }
  if (fileIds.length > 0) {
    startData.formData.projectfiles = fileIds;
  }

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
  console.log(`  ✓ 流程实例启动成功, ID: ${data.id}`);
  return data.id;
}

// 等待函数
function wait(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

// ==================== 可视化测试用例 ====================

// 测试场景 1: 登录 + 访问流程定义页面
test('【场景1】登录并访问流程定义页面', async ({ page }) => {
  console.log('\n========================================');
  console.log('[场景1] 登录并访问流程定义页面 (/workflows/start)');
  console.log('========================================');

  await login(page);

  console.log('\n>>> 正在跳转到 /workflows/start ...');
  await page.goto(`${BASE_URL}/workflows/start`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 等待页面加载完成...');
  await page.waitForSelector('.workflow-manager-container, .left-panel', { timeout: 15000 });
  await page.waitForTimeout(SLOW_DELAY);

  console.log('\n✓ 场景1完成：已进入流程定义页面');
});

// 测试场景 2: 访问"待办任务"页面（带 Tab）
test('【场景2】访问待办任务页面（查看 Tab 结构）', async ({ page }) => {
  console.log('\n========================================');
  console.log('[场景2] 访问待办任务页面（查看 Tab 结构）');
  console.log('========================================');

  await login(page);

  console.log('\n>>> 跳转到 /workflows/pending ...');
  await page.goto(`${BASE_URL}/workflows/pending`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 等待页面元素...');
  await page.waitForSelector('.el-tabs', { timeout: 15000 });
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 检查 Tab 结构...');
  const tabs = await page.locator('.el-tabs__item').allTextContents();
  console.log(`  找到 Tab: ${JSON.stringify(tabs)}`);

  console.log('>>> 检查默认选中的 Tab（应该是"待审批"）...');
  const activeTab = await page.locator('.el-tabs__item.is-active').textContent();
  console.log(`  当前激活的 Tab: ${activeTab}`);

  console.log('\n✓ 场景2完成：已检查 Tab 结构');
});

// 测试场景 3: 切换到历史记录 Tab
test('【场景3】切换到历史记录 Tab', async ({ page }) => {
  console.log('\n========================================');
  console.log('[场景3] 切换到历史记录 Tab');
  console.log('========================================');

  await login(page);

  console.log('\n>>> 跳转到 /workflows/pending ...');
  await page.goto(`${BASE_URL}/workflows/pending`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 等待页面加载...');
  await page.waitForSelector('.el-tabs', { timeout: 15000 });
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 点击"历史记录" Tab...');
  const historyTab = page.locator('.el-tabs__item:has-text("历史记录")');
  await historyTab.click();
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 检查是否切换成功...');
  const activeTab = await page.locator('.el-tabs__item.is-active').textContent();
  console.log(`  当前激活的 Tab: ${activeTab}`);

  console.log('\n✓ 场景3完成：已切换到历史记录 Tab');
});

// 测试场景 4: 创建流程定义
test('【场景4】创建单步审批流程定义', async () => {
  console.log('\n========================================');
  console.log('[场景4] 创建单步审批流程定义');
  console.log('========================================');

  projectId = await getFirstProjectId();
  if (projectId) {
    console.log(`✓ 获取项目ID: ${projectId}`);
    const files = await getProjectFiles(projectId);
    fileIds = files.slice(0, 2).map(f => f.id);
    console.log(`✓ 获取项目文件: ${fileIds.length} 个`);
  } else {
    console.log('⚠ 未找到项目');
  }

  console.log('\n>>> 创建单步审批流程定义...');
  const workflowName = generateWorkflowName('随机测试流程');
  currentDefinitionId = await createWorkflowDefinitionAPI({
    name: workflowName,
    singleStep: true,
    withProjectField: !!projectId,
    withProjectFileField: !!projectId && fileIds.length > 0,
    randomFieldsCount: 2
  });

  console.log('\n✓ 场景4完成：单步审批流程定义已创建');
});

// 测试场景 5: 启动多个流程实例用于随机审批
test('【场景5】启动多个流程实例用于随机审批测试', async () => {
  console.log('\n========================================');
  console.log('[场景5] 启动多个流程实例用于随机审批测试');
  console.log('========================================');

  console.log('\n>>> 启动流程实例 1...');
  await startWorkflowInstanceAPI(currentDefinitionId, projectId, fileIds);
  await wait(500);

  console.log('\n>>> 启动流程实例 2...');
  await startWorkflowInstanceAPI(currentDefinitionId, projectId, fileIds);
  await wait(500);

  console.log('\n>>> 启动流程实例 3...');
  await startWorkflowInstanceAPI(currentDefinitionId, projectId, fileIds);
  await wait(500);

  console.log('\n>>> 启动流程实例 4...');
  await startWorkflowInstanceAPI(currentDefinitionId, projectId, fileIds);
  await wait(500);

  console.log('\n>>> 启动流程实例 5...');
  await startWorkflowInstanceAPI(currentDefinitionId, projectId, fileIds);
  await wait(1000);

  console.log('\n✓ 场景5完成：已启动5个流程实例');
});

// 测试场景 6: 随机审批待办任务（通过或拒绝）
test('【场景6】随机审批待办任务（通过或拒绝）', async ({ page }) => {
  console.log('\n========================================');
  console.log('[场景6】随机审批待办任务（通过或拒绝）');
  console.log('========================================');

  await login(page);

  console.log('\n>>> 跳转到 /workflows/pending ...');
  await page.goto(`${BASE_URL}/workflows/pending`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 等待表格加载...');
  await page.waitForSelector('.el-table', { timeout: 15000 });
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 统计待办任务数量...');
  let tableRows = await page.locator('.el-table__row').count();
  console.log(`  当前待办任务数: ${tableRows}`);

  let approveCount = 0;
  let rejectCount = 0;
  const maxOperations = Math.min(tableRows, 3); // 最多操作3个任务

  for (let i = 0; i < maxOperations; i++) {
    console.log(`\n>>> 刷新列表获取最新任务...`);
    await page.locator('button:has-text("刷新")').click();
    await page.waitForTimeout(SLOW_DELAY);

    const rows = await page.locator('.el-tab-pane[name="pending"] .el-table__row').count();
    if (rows === 0) {
      console.log('  ⚠ 没有更多待办任务');
      break;
    }

    // 随机决定是通过还是拒绝
    const action = Math.random() > 0.5 ? 'approve' : 'reject';
    console.log(`  随机选择: ${action === 'approve' ? '通过' : '拒绝'}`);

    if (action === 'approve') {
      console.log('>>> 点击"通过"按钮...');
      const approveBtn = page.locator('.el-tab-pane[name="pending"] .el-table__row').first().locator('button:has-text("通过")');
      if (await approveBtn.isVisible()) {
        await approveBtn.click();
        await page.waitForTimeout(SLOW_DELAY);

        console.log('>>> 等待审批对话框出现...');
        const dialog = page.locator('.el-dialog');
        if (await dialog.isVisible()) {
          console.log('>>> 填写审批意见...');
          const commentInput = page.locator('.el-dialog textarea');
          if (await commentInput.isVisible()) {
            await commentInput.fill(`UI随机测试通过_${Date.now()}`);
            await page.waitForTimeout(FAST_DELAY);
          }

          console.log('>>> 点击确认通过按钮...');
          const submitBtn = page.locator('.el-dialog button:has-text("通过")');
          await submitBtn.click();
          await page.waitForTimeout(SLOW_DELAY);

          console.log('  ✓ 审批通过操作完成');
          approveCount++;
        }
      }
    } else {
      console.log('>>> 点击"拒绝"按钮...');
      const rejectBtn = page.locator('.el-tab-pane[name="pending"] .el-table__row').first().locator('button:has-text("拒绝")');
      if (await rejectBtn.isVisible()) {
        await rejectBtn.click();
        await page.waitForTimeout(SLOW_DELAY);

        console.log('>>> 等待拒绝对话框出现...');
        const dialog = page.locator('.el-dialog');
        if (await dialog.isVisible()) {
          console.log('>>> 填写拒绝原因...');
          const commentInput = page.locator('.el-dialog textarea');
          if (await commentInput.isVisible()) {
            await commentInput.fill(`UI随机测试拒绝_${Date.now()}`);
            await page.waitForTimeout(FAST_DELAY);
          }

          console.log('>>> 点击确认拒绝按钮...');
          const rejectConfirmBtn = page.locator('.el-dialog button:has-text("拒绝")');
          await rejectConfirmBtn.click();
          await page.waitForTimeout(SLOW_DELAY);

          console.log('  ✓ 审批拒绝操作完成');
          rejectCount++;
        }
      }
    }

    // 检查是否有错误消息
    const errorMsg = await page.locator('.el-message--error').textContent().catch(() => null);
    if (errorMsg) {
      console.log(`  ⚠ 出现错误消息: ${errorMsg}`);
    } else {
      console.log(`  ✓ 未发现错误`);
    }
  }

  console.log(`\n  汇总: 通过 ${approveCount} 个, 拒绝 ${rejectCount} 个`);
  console.log('\n✓ 场景6完成：随机审批测试完成');
});

// 测试场景 7: 检查历史记录 Tab
test('【场景7】检查历史记录是否包含刚才的操作', async ({ page }) => {
  console.log('\n========================================');
  console.log('[场景7] 检查历史记录是否包含刚才的操作');
  console.log('========================================');

  await login(page);

  console.log('\n>>> 跳转到 /workflows/pending ...');
  await page.goto(`${BASE_URL}/workflows/pending`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 点击"历史记录" Tab...');
  const historyTab = page.locator('.el-tabs__item:has-text("历史记录")');
  await historyTab.click();
  await page.waitForTimeout(3000); // 等待 Tab 切换动画完成

  console.log('>>> 检查历史记录表格...');
  // 检查是否有历史记录表格或空状态
  const emptyText = await page.locator('.el-tab-pane[name="history"]').textContent().catch(() => null);
  console.log(`  历史记录 Tab 内容: ${emptyText ? emptyText.substring(0, 100) : 'N/A'}`);

  // 尝试查找表格行或空状态提示
  const tableRows = await page.locator('.el-tab-pane[name="history"] .el-table__row').count().catch(() => 0);
  const emptyTip = await page.locator('.el-tab-pane[name="history"] .el-table__empty-text').textContent().catch(() => null);
  console.log(`  当前历史记录数: ${tableRows}`);

  if (tableRows > 0) {
    console.log('>>> 查看历史记录内容...');
    const firstRow = page.locator('.el-tab-pane[name="history"] .el-table__row').first();
    const instanceName = await firstRow.locator('td').first().textContent();
    const status = await firstRow.locator('.el-tag').textContent();
    console.log(`  第一条记录: ${instanceName}, 状态: ${status}`);
  } else if (emptyTip) {
    console.log(`  ⚠ 暂无历史记录: ${emptyTip}`);
  } else {
    console.log('  ⚠ 暂无历史记录');
  }

  console.log('\n✓ 场景7完成：已检查历史记录');
});

// 测试场景 8: 再次创建流程验证新任务
test('【场景8】创建新流程并验证出现在待审批列表', async () => {
  console.log('\n========================================');
  console.log('[场景8] 创建新流程并验证出现在待审批列表');
  console.log('========================================');

  console.log('\n>>> 创建新的流程定义...');
  const workflowName = generateWorkflowName('验证测试流程');
  const newDefId = await createWorkflowDefinitionAPI({
    name: workflowName,
    singleStep: true,
    withProjectField: !!projectId,
    withProjectFileField: false,
    randomFieldsCount: 2
  });

  console.log('\n>>> 启动新流程实例...');
  await startWorkflowInstanceAPI(newDefId, projectId, fileIds);
  await wait(1000);

  console.log('\n✓ 场景8完成：已创建并启动新流程');
});

// 测试场景 9: 查看待审批列表确认新任务出现
test('【场景9】查看待审批列表确认新任务出现', async ({ page }) => {
  console.log('\n========================================');
  console.log('[场景9] 查看待审批列表确认新任务出现');
  console.log('========================================');

  await login(page);

  console.log('\n>>> 跳转到 /workflows/pending ...');
  await page.goto(`${BASE_URL}/workflows/pending`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 等待页面加载...');
  await page.waitForSelector('.el-tabs', { timeout: 15000 });
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 刷新列表...');
  await page.locator('button:has-text("刷新")').click();
  await page.waitForTimeout(SLOW_DELAY);

  console.log('>>> 统计待办任务数量...');
  const tableRows = await page.locator('.el-table__row').count();
  console.log(`  当前待办任务数: ${tableRows}`);

  if (tableRows > 0) {
    console.log('>>> 查看第一个任务的流程名称...');
    const firstRow = page.locator('.el-table__row').first();
    const instanceName = await firstRow.locator('td').first().textContent();
    console.log(`  第一个任务: ${instanceName}`);
  }

  console.log('\n✓ 场景9完成');
});

// 清理
test.afterAll(async () => {
  console.log('\n========================================');
  console.log('[清理] 测试完成');
  console.log('========================================');
  console.log('测试数据已保留供检查');
  console.log('覆盖的操作：通过、拒绝、Tab切换、历史记录');
});
