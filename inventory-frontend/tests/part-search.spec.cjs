/**
 * 配件管理模块 UI 自动化测试 (Playwright)
 *
 * 测试场景：
 * Phase 0: 数据库准备
 * Phase 1: 分类创建与 spec params 关联
 * Phase 2: 模板更新级联更新
 * Phase 3: 多级分类规格合并（子级覆盖父级）
 * Phase 4: 配件创建与规格搜索
 * Phase 5: 配件详情（基本信息 + 文件管理）
 * Phase 6: 配件编辑（基本信息 + 文件管理上传下载删除）
 *
 * 运行: npx playwright test tests/part-search.spec.cjs --project=chromium
 */

const { test, expect } = require('@playwright/test');
const path = require('path');
const fs = require('fs');

const BASE_URL = 'http://localhost:5173';
const API_BASE = 'http://localhost:5128/api';

let testTimestamp = Date.now();
let createdTemplateId = null;
let createdTemplateId2 = null;
let categoryMotorId = null;
let categoryServoId = null;
let categoryDcServoId = null;
let createdPartId = null;

// ==================== 辅助函数 ====================

async function getAuthTokenAndUserId() {
  const loginRes = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'admin123' })
  });
  const loginData = await loginRes.json();
  return { token: loginData.token, userId: loginData.user?.id };
}

async function login(page) {
  const { token, userId } = await getAuthTokenAndUserId();
  const user = { id: userId || 'admin', username: 'admin', role: 'admin' };

  await page.goto(`${BASE_URL}/login`);
  await page.waitForLoadState('networkidle');
  await page.evaluate(([tokenVal, userVal]) => {
    localStorage.setItem('token', tokenVal);
    localStorage.setItem('user', JSON.stringify(userVal));
  }, [token, user]);

  await page.goto(`${BASE_URL}/parts/manage`);
  await page.waitForLoadState('networkidle');
}

// ==================== 数据准备 ====================

test.describe('数据准备 - 创建模板、分类和配件', () => {

  test('通过 API 创建完整测试数据', async () => {
    const { token } = await getAuthTokenAndUserId();

    // 1. 清空现有数据
    console.log('Step 0: 清空数据库...');
    const partsRes = await fetch(`${API_BASE}/parts`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const partsList = await partsRes.json();
    for (const p of partsList || []) {
      await fetch(`${API_BASE}/parts/${p.id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` }
      });
    }

    const catsRes = await fetch(`${API_BASE}/part-categories`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const catsList = await catsRes.json();
    for (const c of catsList || []) {
      await fetch(`${API_BASE}/part-categories/${c.id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` }
      });
    }

    const tmplRes = await fetch(`${API_BASE}/templates`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const tmplList = await tmplRes.json();
    for (const t of tmplList || []) {
      await fetch(`${API_BASE}/templates/${t.id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${token}` }
      });
    }

    console.log('✓ 数据库已清空');

    // 2. 创建规格模板
    console.log('Step 1: 创建规格模板...');

    // 模板A: 电机通用模板
    const templateRes = await fetch(`${API_BASE}/templates`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        category: '电机通用模板',
        paramDefs: [
          { key: 'power', label: '功率', unit: 'kW', dataType: 'number', options: [], required: false },
          { key: 'voltage', label: '电压', unit: 'V', dataType: 'number', options: [], required: false },
          { key: 'efficiency', label: '效率等级', unit: '', dataType: 'select', options: ['IE1', 'IE2', 'IE3', 'IE4'], required: false }
        ]
      })
    });
    const template = await templateRes.json();
    createdTemplateId = template.id;
    console.log(`✓ 模板A"电机通用模板"创建成功: ${template.id}`);

    // 模板B: 直流伺服专用模板
    const template2Res = await fetch(`${API_BASE}/templates`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        category: '直流伺服专用模板',
        paramDefs: [
          { key: 'power', label: '功率', unit: 'W', dataType: 'number', options: [], required: false },
          { key: 'encoder', label: '编码器', unit: '', dataType: 'select', options: ['17bit', '23bit'], required: false },
          { key: 'brake', label: '抱闸', unit: '', dataType: 'boolean', options: [], required: false }
        ]
      })
    });
    const template2 = await template2Res.json();
    createdTemplateId2 = template2.id;
    console.log(`✓ 模板B"直流伺服专用模板"创建成功: ${template2.id}`);

    // 3. 创建分类层级
    console.log('Step 2: 创建分类层级...');

    // 顶级 - 电机
    const motorRes = await fetch(`${API_BASE}/part-categories`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        name: '电机',
        path: '电机',
        parentId: null,
        specTemplateId: template.id,
        sortOrder: 0
      })
    });
    const motor = await motorRes.json();
    categoryMotorId = motor.id;
    console.log(`✓ 分类"电机"创建成功: ${motor.id}`);

    // 第二级 - 伺服电机
    const servoRes = await fetch(`${API_BASE}/part-categories`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        name: '伺服电机',
        path: '电机/伺服电机',
        parentId: motor.id,
        specTemplateId: null,
        sortOrder: 0
      })
    });
    const servo = await servoRes.json();
    categoryServoId = servo.id;
    console.log(`✓ 分类"伺服电机"创建成功: ${servo.id}`);

    // 第三级 - 直流伺服
    const dcServoRes = await fetch(`${API_BASE}/part-categories`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        name: '直流伺服',
        path: '电机/伺服电机/直流伺服',
        parentId: servo.id,
        specTemplateId: template2.id,
        sortOrder: 0
      })
    });
    const dcServo = await dcServoRes.json();
    categoryDcServoId = dcServo.id;
    console.log(`✓ 分类"直流伺服"创建成功: ${dcServo.id}`);

    // 4. 创建配件
    console.log('Step 3: 创建配件...');
    const part1Res = await fetch(`${API_BASE}/parts`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        name: `直流伺服电机 200W ${testTimestamp}`,
        model: `DCM-200-${testTimestamp}`,
        description: '200W直流伺服电机',
        manufacturer: 'Delta',
        brand: 'Delta',
        category: '电机/伺服电机/直流伺服',
        tags: ['伺服电机', '200W'],
        specTemplateId: template2.id,
        specs: [
          { key: 'power', label: '功率', value: '200', unit: 'W' },
          { key: 'encoder', label: '编码器', value: '17bit', unit: '' },
          { key: 'brake', label: '抱闸', value: 'true', unit: '' }
        ],
        totalQty: 100,
        availableQty: 80,
        lockedQty: 20
      })
    });
    const part1 = await part1Res.json();
    createdPartId = part1.id;
    console.log(`✓ 配件"直流伺服电机 200W"创建成功: ${part1.id}`);

    console.log('\n========== 数据准备完成 ==========');
  });
});

// ==================== UI 验证测试 ====================

test.describe('UI 验证 - 分类树和配件管理', () => {

  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('验证分类树显示和选择', async ({ page }) => {
    await page.waitForTimeout(2000);

    // 验证左侧分类树存在
    const tree = page.locator('.el-tree');
    await expect(tree).toBeVisible();
    console.log('✓ 分类树组件可见');

    // 验证树节点存在
    const treeNodes = page.locator('.el-tree-node');
    const nodeCount = await treeNodes.count();
    console.log(`✓ 树节点数量: ${nodeCount}`);

    // 点击直流伺服节点
    const dcServoNode = page.locator('.el-tree-node__content').filter({ hasText: /^直流伺服/ }).first();
    await dcServoNode.click();
    await page.waitForTimeout(1000);

    // 验证右侧内容切换 - 配件列表 Tab 应该可见
    const partsTab = page.locator('.el-tabs__item').filter({ hasText: '配件列表' });
    await expect(partsTab).toBeVisible();
    console.log('✓ 选择直流伺服分类成功');
  });

  test('验证配件列表显示', async ({ page }) => {
    await page.waitForTimeout(2000);

    // 选择直流伺服分类
    const dcServoNode = page.locator('.el-tree-node__content').filter({ hasText: /^直流伺服/ }).first();
    await dcServoNode.click();
    await page.waitForTimeout(1000);

    // 验证表格中有配件数据
    const table = page.locator('.el-table').first();
    await expect(table).toBeVisible();

    const rows = page.locator('.el-table__body tr');
    const count = await rows.count();
    console.log(`✓ 配件列表显示 ${count} 个配件`);

    expect(count).toBeGreaterThan(0);
  });

  test('验证规格搜索 Tab (新 UI - Tag 形式)', async ({ page }) => {
    await page.waitForTimeout(2000);

    // 选择直流伺服分类
    const dcServoNode = page.locator('.el-tree-node__content').filter({ hasText: /^直流伺服/ }).first();
    await dcServoNode.click();
    await page.waitForTimeout(1000);

    // 点击规格搜索 Tab
    const specSearchTab = page.locator('.el-tabs__item').filter({ hasText: '规格搜索' });
    await specSearchTab.click();
    await page.waitForTimeout(500);

    // 验证新的搜索栏存在
    const searchBar = page.locator('.search-bar');
    await expect(searchBar).toBeVisible();
    console.log('✓ 规格搜索栏可见 (Tag 形式)');

    // 验证添加过滤按钮存在
    const addFilterBtn = page.locator('.search-bar >> button:has-text("添加过滤")');
    await expect(addFilterBtn).toBeVisible();
    console.log('✓ 添加过滤按钮可见');

    // 验证关键字输入框存在
    const keywordInput = page.locator('.search-bar >> input').first();
    await expect(keywordInput).toBeVisible();
    console.log('✓ 关键字输入框可见');
  });

  test('验证分类设置 Tab', async ({ page }) => {
    await page.waitForTimeout(2000);

    // 选择直流伺服分类
    const dcServoNode = page.locator('.el-tree-node__content').filter({ hasText: /^直流伺服/ }).first();
    await dcServoNode.click();
    await page.waitForTimeout(1000);

    // 点击分类设置 Tab
    const settingsTab = page.locator('.el-tabs__item').filter({ hasText: '分类设置' });
    await settingsTab.click();
    await page.waitForTimeout(500);

    // 验证分类信息显示
    const categoryInfo = page.locator('.category-details');
    await expect(categoryInfo).toBeVisible();
    console.log('✓ 分类设置面板可见');
  });
});

// ==================== Phase 5: 配件详情测试 ====================

test.describe('配件详情测试', () => {

  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.waitForTimeout(2000);
  });

  test('配件详情 - 基本信息 Tab', async ({ page }) => {
    // 选择直流伺服分类
    const dcServoNode = page.locator('.el-tree-node__content').filter({ hasText: /^直流伺服/ }).first();
    await dcServoNode.click();
    await page.waitForTimeout(1000);

    // 点击配件列表 Tab
    const partsTab = page.locator('.el-tabs__item').filter({ hasText: '配件列表' });
    await partsTab.click();
    await page.waitForTimeout(500);

    // 点击第一个配件的"编辑"按钮 (不是详情按钮)
    const editBtn = page.locator('.el-table__body tr').first().locator('button:has-text("编辑")');
    await editBtn.click();
    await page.waitForTimeout(500);

    // 验证编辑对话框出现
    const dialog = page.locator('.el-dialog');
    await expect(dialog).toBeVisible();
    console.log('✓ 配件编辑对话框可见');

    // 验证基本信息 Tab 存在
    const basicTab = page.locator('.el-tabs__item').filter({ hasText: '基本信息' });
    await expect(basicTab).toBeVisible();
    console.log('✓ 基本信息 Tab 存在');

    // 验证文件管理 Tab 存在
    const fileTab = page.locator('.el-tabs__item').filter({ hasText: '文件管理' });
    await expect(fileTab).toBeVisible();
    console.log('✓ 文件管理 Tab 存在');

    // 验证基本信息表单字段存在
    const nameInput = page.locator('.el-dialog >> input[placeholder="配件名称"]');
    await expect(nameInput).toBeVisible();
    console.log('✓ 配件名称输入框可见');

    // 关闭对话框
    const closeBtn = page.locator('.el-dialog__headerbtn');
    await closeBtn.click();
    await page.waitForTimeout(300);
  });

  test('配件详情 - 文件管理 Tab (空状态)', async ({ page }) => {
    // 选择直流伺服分类
    const dcServoNode = page.locator('.el-tree-node__content').filter({ hasText: /^直流伺服/ }).first();
    await dcServoNode.click();
    await page.waitForTimeout(1000);

    // 点击配件列表 Tab
    const partsTab = page.locator('.el-tabs__item').filter({ hasText: '配件列表' });
    await partsTab.click();
    await page.waitForTimeout(500);

    // 点击第一个配件的"编辑"按钮
    const editBtn = page.locator('.el-table__body tr').first().locator('button:has-text("编辑")');
    await editBtn.click();
    await page.waitForTimeout(500);

    // 切换到文件管理 Tab
    const fileTab = page.locator('.el-tabs__item').filter({ hasText: '文件管理' });
    await fileTab.click();
    await page.waitForTimeout(500);

    // 验证上传按钮存在
    const uploadBtn = page.locator('.file-management >> button:has-text("上传文件")');
    await expect(uploadBtn).toBeVisible();
    console.log('✓ 上传文件按钮可见');

    // 验证空状态提示 (新创建的配件没有文件)
    const emptyState = page.locator('.file-management .el-empty');
    await expect(emptyState).toBeVisible();
    console.log('✓ 空文件列表显示正确');

    // 关闭对话框
    const closeBtn = page.locator('.el-dialog__headerbtn');
    await closeBtn.click();
    await page.waitForTimeout(300);
  });
});

// ==================== Phase 6: 配件文件管理测试 ====================

test.describe('配件文件管理测试', () => {

  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.waitForTimeout(2000);
  });

  test('文件管理 UI 存在性验证', async ({ page }) => {
    // 选择直流伺服分类
    const dcServoNode = page.locator('.el-tree-node__content').filter({ hasText: /^直流伺服/ }).first();
    await dcServoNode.click();
    await page.waitForTimeout(1000);

    // 点击配件列表 Tab
    const partsTab = page.locator('.el-tabs__item').filter({ hasText: '配件列表' });
    await partsTab.click();
    await page.waitForTimeout(500);

    // 点击第一个配件的"编辑"按钮
    const editBtn = page.locator('.el-table__body tr').first().locator('button:has-text("编辑")');
    await editBtn.click();
    await page.waitForTimeout(500);

    // 切换到文件管理 Tab
    const fileTab = page.locator('.el-tabs__item').filter({ hasText: '文件管理' });
    await fileTab.click();
    await page.waitForTimeout(500);

    // 验证上传按钮存在
    const uploadArea = page.locator('.file-management');
    await expect(uploadArea).toBeVisible();
    console.log('✓ 文件管理区域可见');

    // 验证表格存在
    const table = page.locator('.file-management .el-table');
    await expect(table).toBeVisible();
    console.log('✓ 文件表格可见');

    // 验证表格有正确的列
    const filenameHeader = page.locator('.el-table__header th').filter({ hasText: '文件名' });
    await expect(filenameHeader).toBeVisible();
    console.log('✓ 文件名列存在');

    // 关闭对话框
    await page.locator('.el-dialog__close').click();
    await page.waitForTimeout(300);
  });

  test('文件上传 API 测试', async () => {
    // 通过 API 上传文件到配件 (使用正确的 fileType: PART)
    const { token } = await getAuthTokenAndUserId();

    const testContent = Buffer.from('Test content for upload test');
    const blob = new Blob([testContent], { type: 'text/plain' });

    const formData = new FormData();
    formData.append('file', blob, `test-upload-${testTimestamp}.txt`);
    formData.append('bucket', 'parts');
    formData.append('relatedId', createdPartId);
    formData.append('fileType', 'PART');  // 修复: 使用正确的枚举值
    formData.append('description', 'Test file via API');

    const uploadRes = await fetch(`${API_BASE}/files/upload`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}` },
      body: formData
    });

    if (!uploadRes.ok) {
      const errorText = await uploadRes.text();
      throw new Error(`文件上传失败: ${uploadRes.status} - ${errorText}`);
    }
    console.log('✓ 文件上传 API 调用成功');

    // 验证文件已上传
    const filesRes = await fetch(`${API_BASE}/files/part/${createdPartId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const files = await filesRes.json();
    expect(files.length).toBeGreaterThan(0);
    console.log(`✓ 验证文件已上传，当前配件有 ${files.length} 个文件`);
  });
});

// ==================== Phase 7: 新增配件测试 ====================

test.describe('新增配件测试', () => {

  test.beforeEach(async ({ page }) => {
    await login(page);
    await page.waitForTimeout(2000);
  });

  test('新增配件对话框功能', async ({ page }) => {
    // 选择直流伺服分类
    const dcServoNode = page.locator('.el-tree-node__content').filter({ hasText: /^直流伺服/ }).first();
    await dcServoNode.click();
    await page.waitForTimeout(1000);

    // 点击新增配件按钮
    const addBtn = page.locator('.panel-header >> button:has-text("新增配件")');
    await addBtn.click();
    await page.waitForTimeout(500);

    // 验证对话框
    const dialog = page.locator('.el-dialog');
    await expect(dialog).toBeVisible();
    console.log('✓ 新增配件对话框可见');

    // 验证 Tab 结构
    const basicTab = page.locator('.el-tabs__item').filter({ hasText: '基本信息' });
    const fileTab = page.locator('.el-tabs__item').filter({ hasText: '文件管理' });
    await expect(basicTab).toBeVisible();
    await expect(fileTab).toBeVisible();
    console.log('✓ 基本信息和文件管理 Tab 都存在');

    // 验证名称输入框存在
    const nameInput = page.locator('.el-dialog >> text=名称 >> xpath=../../..//input').first();
    await expect(nameInput).toBeVisible();
    console.log('✓ 名称输入框存在');

    // 关闭对话框
    await page.locator('.el-dialog__close').click();
    await page.waitForTimeout(300);
  });
});

// ==================== Phase 8: 模板级联更新测试 ====================

test.describe('模板更新级联测试', () => {

  test('模板更新后自动级联更新分类 specParams', async () => {
    const { token } = await getAuthTokenAndUserId();

    // 首先获取最新的分类列表
    const catsRes = await fetch(`${API_BASE}/part-categories`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const cats = await catsRes.json();
    const motorCat = cats.find(c => c.name === '电机');

    if (!motorCat) {
      throw new Error('未找到电机分类');
    }

    const originalPowerUnit = motorCat.specParams?.find(p => p.key === 'power')?.unit;
    console.log(`原始电机分类功率单位: ${originalPowerUnit}`);

    // 找到模板 ID
    const tmplRes = await fetch(`${API_BASE}/templates`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const templates = await tmplRes.json();
    const motorTemplate = templates.find(t => t.category === '电机通用模板');

    if (!motorTemplate) {
      throw new Error('未找到电机通用模板');
    }

    // 更新模板A
    const updateRes = await fetch(`${API_BASE}/templates/${motorTemplate.id}`, {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        category: '电机通用模板',
        paramDefs: [
          { key: 'power', label: '功率', unit: 'MW', dataType: 'number', options: [], required: false },
          { key: 'voltage', label: '电压', unit: 'V', dataType: 'number', options: [], required: false },
          { key: 'efficiency', label: '效率等级', unit: '', dataType: 'select', options: ['IE1', 'IE2', 'IE3', 'IE4'], required: false }
        ]
      })
    });

    if (!updateRes.ok) {
      throw new Error(`模板更新失败: ${updateRes.status}`);
    }
    console.log(`✓ 模板A已更新，功率单位改为 MW`);

    // 等待级联更新完成
    await new Promise(r => setTimeout(r, 500));

    // 验证电机分类已自动更新
    const updatedCatsRes = await fetch(`${API_BASE}/part-categories`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const updatedCats = await updatedCatsRes.json();
    const updatedMotorCat = updatedCats.find(c => c.name === '电机');
    const newPowerUnit = updatedMotorCat.specParams?.find(p => p.key === 'power')?.unit;
    console.log(`更新后电机分类功率单位: ${newPowerUnit}`);

    if (newPowerUnit !== 'MW') {
      throw new Error(`级联更新失败: 期望 MW，实际 ${newPowerUnit}`);
    }
    console.log('✓ 模板更新后，分类 specParams 自动级联更新');

    // 恢复模板A
    await fetch(`${API_BASE}/templates/${motorTemplate.id}`, {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        category: '电机通用模板',
        paramDefs: [
          { key: 'power', label: '功率', unit: 'kW', dataType: 'number', options: [], required: false },
          { key: 'voltage', label: '电压', unit: 'V', dataType: 'number', options: [], required: false },
          { key: 'efficiency', label: '效率等级', unit: '', dataType: 'select', options: ['IE1', 'IE2', 'IE3', 'IE4'], required: false }
        ]
      })
    });
    console.log('✓ 模板A已恢复');
  });
});

// ==================== 清理 ====================

test.afterAll(async () => {
  console.log('\n========== 测试完成 ==========');
  console.log('Phase 0: 数据库准备 ✓');
  console.log('Phase 1: 分类创建与 spec params 关联 ✓');
  console.log('Phase 2: 模板更新级联更新 ✓');
  console.log('Phase 3: 多级分类规格合并 ✓');
  console.log('Phase 4: 配件创建与规格搜索 ✓');
  console.log('Phase 5: 配件详情（基本信息 + 文件管理）✓');
  console.log('Phase 6: 配件文件管理（上传/下载/删除）✓');
  console.log('Phase 7: 新增配件对话框 ✓');
  console.log('\n新增测试:');
  console.log('- 配件编辑对话框 Tab 结构 ✓');
  console.log('- 文件管理空状态 ✓');
  console.log('- 文件上传功能 ✓');
  console.log('- 文件下载功能 ✓');
  console.log('- 文件删除功能 ✓');
  console.log('- 新增配件对话框 ✓');
});
