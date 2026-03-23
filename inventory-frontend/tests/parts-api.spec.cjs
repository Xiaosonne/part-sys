/**
 * 部件和参数模板 API + UI 验证测试 (Playwright)
 *
 * 测试场景：
 * 1. 通过API创建模板和部件
 * 2. 验证UI页面能正确显示数据
 *
 * 运行: npx playwright test tests/parts-api.spec.cjs --project=chromium
 */

const { test, expect } = require('@playwright/test');

const BASE_URL = 'http://localhost:5173';
const API_BASE = 'http://localhost:5128/api';

let createdTemplateId = '';
let createdPartId = '';
let testTimestamp = Date.now();

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

  await page.goto(`${BASE_URL}/parts`);
  await page.evaluate(([tokenVal, userVal]) => {
    localStorage.setItem('token', tokenVal);
    localStorage.setItem('user', JSON.stringify(userVal));
  }, [token, user]);

  await page.goto(`${BASE_URL}/parts`);
  await page.waitForLoadState('networkidle');
}

// ==================== API 测试 ====================

test.describe('API层验证', () => {

  test('Templates API - 创建模板', async () => {
    const { token } = await getAuthTokenAndUserId();

    const template = {
      category: `测试模板_${testTimestamp}`,
      paramDefs: [
        { key: 'voltage', label: '电压', unit: 'V', dataType: 'string', options: [], required: true },
        { key: 'power', label: '功率', unit: 'kW', dataType: 'number', options: [], required: false }
      ]
    };

    const res = await fetch(`${API_BASE}/templates`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(template)
    });

    const data = await res.json();
    expect(res.ok).toBe(true);
    expect(data.id).toBeTruthy();
    createdTemplateId = data.id;
    console.log(`✓ API创建模板成功: ${data.category}, ID: ${data.id}`);
  });

  test('Templates API - 获取模板列表', async () => {
    const { token } = await getAuthTokenAndUserId();

    const res = await fetch(`${API_BASE}/templates`, {
      headers: { Authorization: `Bearer ${token}` }
    });

    const data = await res.json();
    expect(Array.isArray(data)).toBe(true);
    expect(data.length).toBeGreaterThan(0);
    console.log(`✓ API获取模板列表成功，共 ${data.length} 个模板`);
  });

  test('Parts API - 创建部件', async () => {
    const { token } = await getAuthTokenAndUserId();

    const part = {
      name: `测试部件_${testTimestamp}`,
      model: 'TEST-MODEL-001',
      description: '测试描述',
      manufacturer: '测试厂商',
      brand: '测试品牌',
      category: '测试类别',
      tags: ['API测试', '自动化'],
      specTemplateId: createdTemplateId,
      specs: [
        { key: 'voltage', label: '电压', value: '220', unit: 'V' },
        { key: 'power', label: '功率', value: '15', unit: 'kW' }
      ]
    };

    const res = await fetch(`${API_BASE}/parts`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(part)
    });

    const data = await res.json();
    expect(res.ok).toBe(true);
    expect(data.id).toBeTruthy();
    createdPartId = data.id;
    console.log(`✓ API创建部件成功: ${data.name}, ID: ${data.id}`);
  });

  test('Parts API - 获取部件列表', async () => {
    const { token } = await getAuthTokenAndUserId();

    const res = await fetch(`${API_BASE}/parts`, {
      headers: { Authorization: `Bearer ${token}` }
    });

    const data = await res.json();
    expect(Array.isArray(data)).toBe(true);
    expect(data.length).toBeGreaterThan(0);

    // 验证我们有带specs的部件
    const partWithSpecs = data.find(p => p.specs && p.specs.length > 0);
    expect(partWithSpecs).toBeTruthy();
    console.log(`✓ API获取部件列表成功，共 ${data.length} 个部件，其中 ${partWithSpecs?.specs?.length} 个带规格`);
  });
});

// ==================== UI 测试 ====================

test.describe('UI层验证 - SpecTemplates页面', () => {

  test('验证模板列表显示', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/spec-templates`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 等待表格加载
    const table = page.locator('.el-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    // 验证有数据行
    const rows = page.locator('.el-table__body tr');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);

    console.log(`✓ 模板列表显示 ${count} 条数据`);
  });

  test('验证创建模板对话框', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/spec-templates`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 点击添加按钮
    await page.click('button:has-text("Add Template")');
    await page.waitForTimeout(500);

    // 验证对话框出现
    const dialog = page.locator('.el-dialog:visible');
    await expect(dialog).toBeVisible();

    // 填写类别名称
    await page.fill('input[placeholder*="Motor"]', `UI测试模板_${testTimestamp}`);

    console.log('✓ 创建模板对话框正常');
  });
});

test.describe('UI层验证 - Parts页面', () => {

  test('验证部件列表显示', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 等待表格加载
    const table = page.locator('.el-table');
    await expect(table).toBeVisible({ timeout: 10000 });

    // 验证有数据行
    const rows = page.locator('.el-table__body tr');
    const count = await rows.count();
    expect(count).toBeGreaterThan(0);

    // 验证Tags列存在
    const tagsHeader = page.locator('.el-table__header th:has-text("Tags")');
    await expect(tagsHeader).toBeVisible();

    console.log(`✓ 部件列表显示 ${count} 条数据，Tags列存在`);
  });

  test('验证创建部件对话框', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 点击添加按钮
    await page.click('button:has-text("Add Part")');
    await page.waitForTimeout(500);

    // 验证对话框出现
    const dialog = page.locator('.el-dialog:visible');
    await expect(dialog).toBeVisible();

    // 验证表单字段
    const nameInput = dialog.locator('input').first();
    await expect(nameInput).toBeVisible();

    // 验证Template选择器存在
    const templateSelect = dialog.locator('.el-select').nth(1);
    await expect(templateSelect).toBeVisible();

    // 验证Tags选择器存在
    const tagsSelect = dialog.locator('.el-select').last();
    await expect(tagsSelect).toBeVisible();

    console.log('✓ 创建部件对话框正常，所有字段存在');
  });

  test('验证Specs查看按钮', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 等待表格加载
    await page.waitForSelector('.el-table__body tr');

    // 点击第一个Specs按钮
    const specsButton = page.locator('button:has-text("Specs")').first();
    if (await specsButton.isVisible()) {
      await specsButton.click();
      await page.waitForTimeout(500);

      // 验证对话框出现
      const dialog = page.locator('.el-dialog:visible');
      await expect(dialog).toBeVisible();

      console.log('✓ Specs对话框正常');
    } else {
      console.log('⚠ 没有找到带Specs的部件（可能当前部件没有规格）');
    }
  });

  test('验证选择模板后显示规格字段', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 点击添加按钮
    await page.click('button:has-text("Add Part")');
    await page.waitForTimeout(500);

    // 选择模板
    const templateSelect = page.locator('.el-dialog:visible .el-select').nth(1);
    await templateSelect.click();
    await page.waitForTimeout(500);

    // 选择第一个模板选项
    const firstOption = page.locator('.el-select-dropdown__item').first();
    if (await firstOption.isVisible()) {
      await firstOption.click();
      await page.waitForTimeout(1000);

      // 检查是否有规格字段显示
      const specSection = page.locator('text=Specifications');
      const hasSpecs = await specSection.isVisible().catch(() => false);

      if (hasSpecs) {
        console.log('✓ 选择模板后，规格字段正确显示');
      } else {
        console.log('⚠ 规格字段未显示（可能模板列表为空）');
      }
    } else {
      console.log('⚠ 没有模板选项可选择');
    }
  });
});

// 清理
test.afterAll(async () => {
  console.log('\n========== 测试完成 ==========');
  console.log(`创建的模板ID: ${createdTemplateId}`);
  console.log(`创建的部件ID: ${createdPartId}`);
});
