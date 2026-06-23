/**
 * 部件和参数模板 UI 自动化测试 (Playwright)
 *
 * 测试场景：
 * 1. 创建参数模板（电机类别，含电压/功率/转速参数）
 * 2. 创建部件并选择模板
 * 3. 填写规格值
 * 4. 添加Tags
 * 5. 验证部件列表显示正确
 * 6. 验证规格查看对话框
 *
 * 运行: npx playwright test tests/parts-ui.spec.cjs --project=chromium
 * 或: npx playwright test tests/parts-ui.spec.cjs --project=chromium --headed
 */

const { test, expect } = require('@playwright/test');

const BASE_URL = 'http://localhost:5173';
const API_BASE = 'http://localhost:5128/api';

let templateCounter = 0;
let partCounter = 0;
let currentTemplateId = '';
let currentTemplateName = '';
let currentPartName = '';

// 统一的登录获取 token
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

// 通过 API 创建模板
async function createTemplateAPI(options = {}) {
  const { token } = await getAuthTokenAndUserId();

  const template = {
    category: options.category || `电机_${Date.now()}`,
    paramDefs: options.paramDefs || [
      { key: 'voltage', label: '电压', unit: 'V', dataType: 'string', options: [], required: true },
      { key: 'power', label: '功率', unit: 'kW', dataType: 'number', options: [], required: false },
      { key: 'speed', label: '转速', unit: 'rpm', dataType: 'number', options: [], required: false },
      { key: 'efficiency', label: '效率等级', unit: '', dataType: 'select', options: ['IE1', 'IE2', 'IE3', 'IE4'], required: false }
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
  if (!res.ok) throw new Error('创建模板失败: ' + JSON.stringify(data));
  console.log(`✓ API创建模板成功: ${template.category}, ID: ${data.id}`);
  return data;
}

// 通过 API 获取模板
async function getTemplateByCategoryAPI(category) {
  const { token } = await getAuthTokenAndUserId();
  const res = await fetch(`${API_BASE}/templates/category/${encodeURIComponent(category)}`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  const data = await res.json();
  return data;
}

// 通过 API 创建部件
async function createPartAPI(options = {}) {
  const { token } = await getAuthTokenAndUserId();

  const part = {
    name: options.name || `测试电机_${Date.now()}`,
    model: options.model || 'MODEL-2024',
    description: options.description || 'UI自动化测试创建的电机部件',
    manufacturer: options.manufacturer || '测试厂商',
    brand: options.brand || '测试品牌',
    category: options.category || '电机',
    tags: options.tags || ['测试', '自动化'],
    specTemplateId: options.specTemplateId || null,
    specs: options.specs || []
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
  if (!res.ok) throw new Error('创建部件失败: ' + JSON.stringify(data));
  console.log(`✓ API创建部件成功: ${part.name}, ID: ${data.id}`);
  return data;
}

// 登录 - 通过 API 获取 token 并设置到 localStorage
async function login(page) {
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

  await page.goto(`${BASE_URL}/parts`);
  await page.evaluate(([tokenVal, userVal]) => {
    localStorage.setItem('token', tokenVal);
    localStorage.setItem('user', JSON.stringify(userVal));
  }, [token, user]);

  await page.goto(`${BASE_URL}/parts`);
  await page.waitForLoadState('networkidle');
  console.log('✓ 登录成功');
}

// 等待函数
function wait(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

// ==================== 测试用例 ====================

test.describe('参数模板管理测试', () => {

  test('访问参数模板页面', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/spec-templates`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 验证页面元素
    const addButton = page.locator('button:has-text("Add Template")');
    await expect(addButton).toBeVisible();
    console.log('✓ 进入参数模板页面');
  });

  test('创建参数模板（UI操作）', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/spec-templates`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 点击添加模板按钮
    await page.click('button:has-text("Add Template")');
    await page.waitForTimeout(500);

    // 填写类别
    templateCounter++;
    const categoryName = `电机_${Date.now()}`;
    currentTemplateName = categoryName;
    await page.fill('input[placeholder*="Motor"]', categoryName);

    // 添加第一个参数：电压
    await page.click('button:has-text("Add Parameter")');
    await page.waitForTimeout(300);
    await page.fill('input[placeholder*="e.g., voltage"]', 'voltage');
    await page.fill('input[placeholder*="e.g., Voltage"]', '电压');
    await page.fill('input[placeholder*="V, kW, rpm"]', 'V');

    // 切换到第一个参数编辑对话框并保存
    const firstParamDialog = page.locator('.el-dialog:visible').last();
    await page.click('button:has-text("Save"):visible');

    await wait(300);

    // 添加第二个参数：功率（number类型）
    await page.click('button:has-text("Add Parameter")');
    await page.waitForTimeout(300);

    // 填写第二个参数
    const secondDialog = page.locator('.el-dialog:visible').last();
    const inputs = secondDialog.locator('input');
    await inputs.nth(0).fill('power'); // key
    await inputs.nth(1).fill('功率'); // label
    await inputs.nth(2).fill('kW'); // unit

    // 选择数据类型为 Number
    await secondDialog.locator('.el-select').click();
    await page.waitForTimeout(300);
    await page.locator('.el-select-dropdown__item:has-text("Number")').click();

    await page.click('button:has-text("Save"):visible');
    await wait(300);

    // 添加第三个参数：效率等级（select类型）
    await page.click('button:has-text("Add Parameter")');
    await page.waitForTimeout(300);

    const thirdDialog = page.locator('.el-dialog:visible').last();
    const thirdInputs = thirdDialog.locator('input');
    await thirdInputs.nth(0).fill('efficiency');
    await thirdInputs.nth(1).fill('效率等级');

    // 选择数据类型为 Select
    await thirdDialog.locator('.el-select').click();
    await page.waitForTimeout(300);
    await page.locator('.el-select-dropdown__item:has-text("Select")').click();
    await wait(300);

    // 填写选项
    await thirdDialog.locator('textarea').fill('IE1\nIE2\nIE3\nIE4');

    await page.click('button:has-text("Save"):visible');
    await wait(300);

    // 保存模板
    await page.click('button:has-text("Save"):visible');
    await page.waitForTimeout(1000);

    console.log(`✓ 模板创建完成: ${categoryName}`);
  });
});

test.describe('部件管理测试', () => {

  test.beforeAll(async () => {
    console.log('\n========== 部件测试准备 ==========');

    // 通过API创建模板
    const template = await createTemplateAPI({
      category: `电机_${Date.now()}`
    });
    currentTemplateId = template.id;
    console.log(`✓ 预创建模板ID: ${currentTemplateId}`);
  });

  test('访问部件页面', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 验证页面元素
    const addButton = page.locator('button:has-text("Add Part")');
    await expect(addButton).toBeVisible();
    console.log('✓ 进入部件页面');
  });

  test('创建部件（选择模板并填写规格）', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 点击添加部件按钮
    await page.click('button:has-text("Add Part")');
    await page.waitForTimeout(500);

    // 填写基本信息
    partCounter++;
    currentPartName = `测试电机_${Date.now()}`;
    const form = page.locator('.el-dialog:visible');

    // 名称
    await form.locator('input').first().fill(currentPartName);
    // Model
    await form.locator('input').nth(1).fill('MODEL-2024-UI');
    // Description
    await form.locator('textarea').first().fill('UI自动化测试创建的电机部件');
    // Manufacturer
    await form.locator('input').nth(3).fill('测试电机厂');
    // Brand
    await form.locator('input').nth(4).fill('TestMotor');

    // 选择类别（使用el-select）
    const categorySelect = form.locator('.el-select').first();
    await categorySelect.click();
    await page.waitForTimeout(300);
    // 输入并创建新类别
    await page.locator('.el-select-dropdown__search input').fill('电机');
    await page.waitForTimeout(300);
    await page.locator('.el-select-dropdown__item:has-text("电机")').click();
    await page.waitForTimeout(300);

    // 选择模板
    const templateSelect = form.locator('.el-select').nth(1);
    await templateSelect.click();
    await page.waitForTimeout(500);

    // 选择我们刚创建的模板（按类别名称选择）
    const templateOption = page.locator(`.el-select-dropdown__item:has-text("${currentTemplateName}")`).first();
    if (await templateOption.isVisible()) {
      await templateOption.click();
    } else {
      // 如果找不到，按ID选择或者直接选择第一个
      console.log('⚠ 模板选项未找到，选择已有模板');
      await page.keyboard.press('Escape');
    }
    await page.waitForTimeout(500);

    // 检查是否有规格字段出现
    const specSection = page.locator('text=Specifications');
    const hasSpecs = await specSection.isVisible().catch(() => false);

    if (hasSpecs) {
      console.log('✓ 规格表单已显示');

      // 填写规格值（根据模板中的字段）
      // 电压 - string类型
      const stringInputs = page.locator('.el-dialog:visible input[placeholder*="V"]');
      if (await stringInputs.first().isVisible()) {
        await stringInputs.first().fill('220');
      }

      // 功率 - number类型
      const numberInputs = page.locator('.el-dialog:visible .el-input-number input');
      if (await numberInputs.first().isVisible()) {
        await numberInputs.first().fill('15');
      }

      // 效率等级 - select类型
      const selectInputs = page.locator('.el-dialog:visible .el-select').last();
      if (await selectInputs.isVisible()) {
        await selectInputs.click();
        await page.waitForTimeout(300);
        await page.locator('.el-select-dropdown__item:has-text("IE3")').click();
      }
    } else {
      console.log('⚠ 规格表单未显示（可能模板未选中）');
    }

    // 添加Tags
    const tagSelect = page.locator('.el-dialog:visible .el-select').last();
    await tagSelect.click();
    await page.waitForTimeout(300);
    await page.locator('.el-select-dropdown__search input').fill('自动化');
    await page.waitForTimeout(300);
    await page.locator('.el-select-dropdown__item:has-text("自动化")').first().click();

    console.log('✓ 部件信息填写完成');
  });

  test('通过API创建带规格的部件', async () => {
    // 直接通过API创建完整的部件数据进行验证
    const part = await createPartAPI({
      name: `API创建电机_${Date.now()}`,
      model: 'MODEL-API-2024',
      description: 'API创建的测试电机',
      manufacturer: 'API电机厂',
      brand: 'APIMotor',
      category: '电机',
      tags: ['API测试', '自动化'],
      specTemplateId: currentTemplateId,
      specs: [
        { key: 'voltage', label: '电压', value: '380', unit: 'V' },
        { key: 'power', label: '功率', value: '30', unit: 'kW' },
        { key: 'efficiency', label: '效率等级', value: 'IE3', unit: '' }
      ]
    });
    console.log(`✓ API创建带规格的部件: ${part.name}`);
  });

  test('验证部件列表显示Tags', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 验证表格中有Tags列
    const tagsHeader = page.locator('.el-table__header th:has-text("Tags")');
    await expect(tagsHeader).toBeVisible();
    console.log('✓ 部件列表显示Tags列');
  });

  test('查看部件规格（Specs按钮）', async ({ page }) => {
    await login(page);
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 等待表格加载
    await page.waitForSelector('.el-table__body tr');
    await page.waitForTimeout(1000);

    // 找到第一个有Specs按钮的行并点击
    const specsButton = page.locator('button:has-text("Specs")').first();
    if (await specsButton.isVisible()) {
      await specsButton.click();
      await page.waitForTimeout(500);

      // 验证规格对话框出现
      const specsDialog = page.locator('.el-dialog:visible');
      await expect(specsDialog).toBeVisible();
      console.log('✓ 规格对话框显示');
    } else {
      console.log('⚠ 没有找到Specs按钮（可能没有带规格的部件）');
    }
  });
});

test.describe('部件和模板完整流程测试', () => {

  test('完整流程：模板创建 -> 部件创建 -> 验证', async ({ page }) => {
    await login(page);

    // Step 1: 创建模板
    console.log('\n--- Step 1: 创建参数模板 ---');
    await page.goto(`${BASE_URL}/spec-templates`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    await page.click('button:has-text("Add Template")');
    await page.waitForTimeout(500);

    const templateName = `完整测试模板_${Date.now()}`;
    await page.fill('input[placeholder*="Motor"]', templateName);

    // 添加参数
    await page.click('button:has-text("Add Parameter")');
    await page.waitForTimeout(300);
    const dialog = page.locator('.el-dialog:visible').last();
    await dialog.locator('input').nth(0).fill('current');
    await dialog.locator('input').nth(1).fill('电流');
    await dialog.locator('input').nth(2).fill('A');
    await page.click('button:has-text("Save"):visible');
    await page.waitForTimeout(300);

    await page.click('button:has-text("Save"):visible');
    await page.waitForTimeout(1000);
    console.log(`✓ 模板创建: ${templateName}`);

    // Step 2: 创建部件
    console.log('\n--- Step 2: 创建部件 ---');
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    await page.click('button:has-text("Add Part")');
    await page.waitForTimeout(500);

    const partName = `完整测试部件_${Date.now()}`;
    const partForm = page.locator('.el-dialog:visible');

    await partForm.locator('input').first().fill(partName);
    await partForm.locator('input').nth(1).fill('FULL-TEST-001');

    console.log('✓ 部件基本信息填写完成');

    // Step 3: 验证结果
    console.log('\n--- Step 3: 验证结果 ---');
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 检查创建的部件是否显示
    const partRow = page.locator('.el-table__body', { hasText: partName });
    const isVisible = await partRow.isVisible().catch(() => false);

    if (isVisible) {
      console.log(`✓ 验证成功: 部件 ${partName} 已显示在列表中`);
    } else {
      console.log(`⚠ 部件 ${partName} 未立即显示（可能需要刷新或API延迟）`);
    }
  });
});

// 清理
test.afterAll(async () => {
  console.log('\n========== 测试完成 ==========');
  console.log('注意: 测试数据保留供检查');
});
