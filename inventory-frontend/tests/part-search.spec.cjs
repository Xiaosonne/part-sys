/**
 * 部件搜索模块 UI 自动化测试 (Playwright)
 *
 * 测试场景：
 * 1. 多级分类部件的创建和搜索
 * 2. 关键字搜索
 * 3. 规格过滤搜索
 * 4. 库存数量过滤
 * 5. 分类路径搜索
 * 6. 组合搜索
 * 7. 搜索结果展示
 *
 * 运行: npx playwright test tests/part-search.spec.cjs --project=chromium
 */

const { test, expect } = require('@playwright/test');

const BASE_URL = 'http://localhost:5173';
const API_BASE = 'http://localhost:5128/api';

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

  await page.goto(`${BASE_URL}/parts/search`);
  await page.evaluate(([tokenVal, userVal]) => {
    localStorage.setItem('token', tokenVal);
    localStorage.setItem('user', JSON.stringify(userVal));
  }, [token, user]);

  await page.goto(`${BASE_URL}/parts/search`);
  await page.waitForLoadState('networkidle');
}

// ==================== API 验证测试 ====================

test.describe('API层验证 - 多级分类部件', () => {

  test('创建多级分类部件 - ARM微控制器', async () => {
    const { token } = await getAuthTokenAndUserId();

    // 获取模板
    const templatesRes = await fetch(`${API_BASE}/templates`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const templates = await templatesRes.json();
    const templateId = templates[0]?.id;

    const part = {
      name: `ARM Controller ${testTimestamp}`,
      model: `ARM-${testTimestamp}`,
      description: 'High performance ARM Cortex-M4 MCU',
      manufacturer: 'STMicroelectronics',
      brand: 'STM',
      category: 'Electronics/Microcontroller/ARM',
      tags: ['ARM', 'High Performance', 'STM32'],
      specTemplateId: templateId,
      specs: [
        { key: 'voltage', label: 'Voltage', value: '3.3', unit: 'V' },
        { key: 'power', label: 'Power', value: '180', unit: 'mW' },
        { key: 'efficiency', label: 'Efficiency', value: 'IE3', unit: '' }
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
    expect(data.category).toBe('Electronics/Microcontroller/ARM');
    expect(data.specs.length).toBe(3);
    console.log(`✓ 创建多级分类部件: ${data.name}, Category: ${data.category}`);
  });

  test('创建多级分类部件 - 温度传感器', async () => {
    const { token } = await getAuthTokenAndUserId();

    const part = {
      name: `Temperature Sensor ${testTimestamp}`,
      model: `TEMP-${testTimestamp}`,
      description: 'Digital temperature sensor',
      manufacturer: 'Maxim Integrated',
      brand: 'Maxim',
      category: 'Electronics/Sensors/Temperature',
      tags: ['Temperature', 'Digital', 'Sensor'],
      specs: []
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
    expect(data.category).toBe('Electronics/Sensors/Temperature');
    console.log(`✓ 创建多级分类部件: ${data.name}, Category: ${data.category}`);
  });

  test('创建多级分类部件 - 伺服电机', async () => {
    const { token } = await getAuthTokenAndUserId();

    const templatesRes = await fetch(`${API_BASE}/templates`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    const templates = await templatesRes.json();
    const templateId = templates[0]?.id;

    const part = {
      name: `DC Servo Motor ${testTimestamp}`,
      model: `DCM-${testTimestamp}`,
      description: '200W DC Servo Motor',
      manufacturer: 'Panasonic',
      brand: 'Panasonic',
      category: 'Motor/ServoMotor/DC Servo',
      tags: ['Servo Motor', 'DC', 'High Precision'],
      specTemplateId: templateId,
      specs: [
        { key: 'voltage', label: 'Voltage', value: '48', unit: 'V' },
        { key: 'power', label: 'Power', value: '200', unit: 'W' },
        { key: 'efficiency', label: 'Efficiency', value: 'IE4', unit: '' }
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
    expect(data.category).toBe('Motor/ServoMotor/DC Servo');
    expect(data.specs.length).toBe(3);
    console.log(`✓ 创建多级分类部件: ${data.name}, Category: ${data.category}`);
  });
});

test.describe('API层验证 - 多级分类搜索', () => {

  test('搜索多级分类路径 - Electronics/Microcontroller', async () => {
    const { token } = await getAuthTokenAndUserId();

    const res = await fetch(`${API_BASE}/parts/search`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ categoryPath: 'Electronics/Microcontroller' })
    });

    const data = await res.json();
    expect(Array.isArray(data)).toBe(true);
    expect(data.length).toBeGreaterThan(0);

    // 验证所有结果都属于该分类路径
    data.forEach(part => {
      expect(
        part.category === 'Electronics/Microcontroller' ||
        part.category.startsWith('Electronics/Microcontroller/')
      ).toBe(true);
    });

    console.log(`✓ 分类路径搜索返回 ${data.length} 个结果`);
  });

  test('搜索多级分类路径 - Motor/ServoMotor', async () => {
    const { token } = await getAuthTokenAndUserId();

    const res = await fetch(`${API_BASE}/parts/search`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ categoryPath: 'Motor/ServoMotor' })
    });

    const data = await res.json();
    expect(Array.isArray(data)).toBe(true);

    data.forEach(part => {
      expect(
        part.category === 'Motor/ServoMotor' ||
        part.category.startsWith('Motor/ServoMotor/')
      ).toBe(true);
    });

    console.log(`✓ 伺服电机分类搜索返回 ${data.length} 个结果`);
  });

  test('搜索多级分类路径 - Electronics/Sensors', async () => {
    const { token } = await getAuthTokenAndUserId();

    const res = await fetch(`${API_BASE}/parts/search`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ categoryPath: 'Electronics/Sensors' })
    });

    const data = await res.json();
    expect(Array.isArray(data)).toBe(true);
    console.log(`✓ 传感器分类搜索返回 ${data.length} 个结果`);
  });

  test('关键字搜索 - ARM', async () => {
    const { token } = await getAuthTokenAndUserId();

    const res = await fetch(`${API_BASE}/parts/search`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ keyword: 'ARM' })
    });

    const data = await res.json();
    expect(Array.isArray(data)).toBe(true);
    expect(data.length).toBeGreaterThan(0);

    // 验证结果包含ARM相关
    const hasARM = data.some(p =>
      p.name.includes('ARM') ||
      p.tags?.some(t => t.includes('ARM'))
    );
    expect(hasARM).toBe(true);

    console.log(`✓ 关键字搜索"ARM"返回 ${data.length} 个结果`);
  });

  test('规格过滤 - 效率等级 IE4', async () => {
    const { token } = await getAuthTokenAndUserId();

    const res = await fetch(`${API_BASE}/parts/search`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        specFilters: [
          { Key: 'efficiency', Op: 'eq', Value: 'IE4' }
        ]
      })
    });

    const data = await res.json();
    expect(Array.isArray(data)).toBe(true);
    expect(data.length).toBeGreaterThan(0);

    // 验证所有结果都有IE4效率
    data.forEach(part => {
      const effSpec = part.specs?.find(s => s.key === 'efficiency');
      expect(effSpec?.value).toBe('IE4');
    });

    console.log(`✓ 效率等级IE4过滤返回 ${data.length} 个结果`);
  });

  test('规格过滤 - 功率 >= 150', async () => {
    const { token } = await getAuthTokenAndUserId();

    const res = await fetch(`${API_BASE}/parts/search`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        specFilters: [
          { Key: 'power', Op: 'gte', Value: '150' }
        ]
      })
    });

    const data = await res.json();
    expect(Array.isArray(data)).toBe(true);
    console.log(`✓ 功率>=150过滤返回 ${data.length} 个结果`);
  });

  test('组合搜索 - 多级分类 + 规格过滤', async () => {
    const { token } = await getAuthTokenAndUserId();

    const res = await fetch(`${API_BASE}/parts/search`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        categoryPath: 'Motor/ServoMotor',
        specFilters: [
          { Key: 'efficiency', Op: 'eq', Value: 'IE4' }
        ]
      })
    });

    const data = await res.json();
    expect(Array.isArray(data)).toBe(true);

    // 验证结果同时满足两个条件
    data.forEach(part => {
      expect(
        part.category === 'Motor/ServoMotor' ||
        part.category.startsWith('Motor/ServoMotor/')
      ).toBe(true);

      const effSpec = part.specs?.find(s => s.key === 'efficiency');
      expect(effSpec?.value).toBe('IE4');
    });

    console.log(`✓ 组合搜索(伺服电机+IE4)返回 ${data.length} 个结果`);
  });

  test('组合搜索 - 关键字 + 多级分类 + 规格', async () => {
    const { token } = await getAuthTokenAndUserId();

    const res = await fetch(`${API_BASE}/parts/search`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        keyword: 'Servo',
        categoryPath: 'Motor',
        specFilters: [
          { Key: 'power', Op: 'gte', Value: '100' }
        ]
      })
    });

    const data = await res.json();
    expect(Array.isArray(data)).toBe(true);
    console.log(`✓ 组合搜索返回 ${data.length} 个结果`);
  });
});

// ==================== UI 层测试 ====================

test.describe('UI层验证 - 多级分类搜索', () => {

  test('验证多级分类部件在列表中显示', async ({ page }) => {
    await login(page);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // 搜索所有部件
    await page.getByRole('button', { name: '搜索', exact: true }).click();
    await page.waitForTimeout(2000);

    // 验证表格中有数据
    const resultCount = page.locator('.el-table__body tr');
    const count = await resultCount.count();
    expect(count).toBeGreaterThan(0);

    console.log(`✓ 列表中共 ${count} 个部件`);
  });

  test('验证多级分类搜索功能', async ({ page }) => {
    await login(page);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 输入关键字
    const keywordInput = page.locator('input[placeholder*="搜索名称"]');
    await keywordInput.fill('ARM');

    // 点击搜索
    await page.getByRole('button', { name: '搜索', exact: true }).click();
    await page.waitForTimeout(2000);

    // 验证有结果
    const resultCount = page.locator('.el-table__body tr');
    const count = await resultCount.count();
    expect(count).toBeGreaterThan(0);

    console.log(`✓ 关键字"ARM"搜索返回 ${count} 个结果`);
  });

  test('验证分类路径搜索', async ({ page }) => {
    await login(page);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 选择分类（如果级联选择器可用）
    const cascader = page.locator('.el-cascader');
    if (await cascader.isVisible()) {
      await cascader.click();
      await page.waitForTimeout(500);
      console.log('✓ 级联选择器可用');
    }

    // 输入关键字进行分类搜索
    const keywordInput = page.locator('input[placeholder*="搜索名称"]');
    await keywordInput.fill('Servo');

    await page.getByRole('button', { name: '搜索', exact: true }).click();
    await page.waitForTimeout(2000);

    const resultCount = page.locator('.el-table__body tr');
    const count = await resultCount.count();
    console.log(`✓ 分类路径+关键字搜索返回 ${count} 个结果`);
  });

  test('验证规格过滤显示', async ({ page }) => {
    await login(page);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 规格过滤区域应该存在
    const specSection = page.locator('text=规格过滤');
    const exists = await specSection.count() > 0;
    console.log(`✓ 规格过滤区域存在: ${exists}`);
  });

  test('验证搜索结果包含规格信息', async ({ page }) => {
    await login(page);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 搜索
    await page.getByRole('button', { name: '搜索', exact: true }).click();
    await page.waitForTimeout(2000);

    // 检查规格列
    const specsHeader = page.locator('.el-table__header th:has-text("规格")');
    const exists = await specsHeader.isVisible();
    expect(exists).toBe(true);

    console.log('✓ 搜索结果表格包含规格列');
  });

  test('验证部件详情显示完整信息', async ({ page }) => {
    await login(page);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 搜索
    await page.getByRole('button', { name: '搜索', exact: true }).click();
    await page.waitForTimeout(2000);

    // 点击详情
    const detailButton = page.locator('button:has-text("详情")').first();
    if (await detailButton.isVisible()) {
      await detailButton.click();
      await page.waitForTimeout(500);

      const dialog = page.locator('.el-dialog:visible');
      if (await dialog.isVisible()) {
        // 验证对话框中有分类信息
        const dialogText = await dialog.textContent();
        expect(dialogText).toContain('分类');

        console.log('✓ 详情对话框显示完整信息');
      }
    } else {
      console.log('⚠ 没有可查看详情的部件');
    }
  });
});

// ==================== 功能流程测试 ====================

test.describe('功能流程测试 - 多级分类', () => {

  test('完整的多级分类搜索流程', async ({ page }) => {
    await login(page);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    console.log('\n--- 开始多级分类搜索流程 ---');

    // 1. 搜索所有ARM相关部件
    const keywordInput = page.locator('input[placeholder*="搜索名称"]');
    await keywordInput.fill('ARM');
    await page.getByRole('button', { name: '搜索', exact: true }).click();
    await page.waitForTimeout(2000);

    const armCount = await page.locator('.el-table__body tr').count();
    console.log(`✓ 步骤1: ARM搜索返回 ${armCount} 个结果`);

    // 2. 查看第一个详情
    if (armCount > 0) {
      const detailButton = page.locator('button:has-text("详情")').first();
      await detailButton.click();
      await page.waitForTimeout(500);
      console.log('✓ 步骤2: 查看详情成功');
      await page.click('.el-dialog__footer button:has-text("关闭")');
    }

    // 3. 重置
    await page.getByRole('button', { name: '重置', exact: true }).click();
    await page.waitForTimeout(500);
    console.log('✓ 步骤3: 重置搜索条件');

    // 4. 搜索Servo
    await keywordInput.fill('Servo');
    await page.getByRole('button', { name: '搜索', exact: true }).click();
    await page.waitForTimeout(2000);

    const servoCount = await page.locator('.el-table__body tr').count();
    console.log(`✓ 步骤4: Servo搜索返回 ${servoCount} 个结果`);

    console.log('\n--- 多级分类搜索流程完成 ---');
  });

  test('规格过滤流程', async ({ page }) => {
    await login(page);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    console.log('\n--- 开始规格过滤流程 ---');

    // 搜索所有
    await page.getByRole('button', { name: '搜索', exact: true }).click();
    await page.waitForTimeout(2000);

    const totalCount = await page.locator('.el-table__body tr').count();
    console.log(`✓ 全部部件: ${totalCount} 个`);

    // 设置库存过滤
    const minQtyInput = page.locator('.el-input-number input').first();
    await minQtyInput.fill('0');
    await page.getByRole('button', { name: '搜索', exact: true }).click();
    await page.waitForTimeout(2000);

    console.log('✓ 设置库存过滤 >= 0');

    console.log('\n--- 规格过滤流程完成 ---');
  });
});

// 清理
test.afterAll(async () => {
  console.log('\n========== 多级分类部件搜索测试完成 ==========');
});
