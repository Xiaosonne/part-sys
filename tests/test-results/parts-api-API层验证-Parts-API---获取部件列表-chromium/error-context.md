# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: parts-api.spec.cjs >> API层验证 >> Parts API - 获取部件列表
- Location: parts-api.spec.cjs:124:3

# Error details

```
Error: expect(received).toBeGreaterThan(expected)

Expected: > 0
Received:   0
```

# Test source

```ts
  33  |   const { token, userId } = await getAuthTokenAndUserId();
  34  |   const user = { id: userId || 'admin', username: 'admin', role: 'admin' };
  35  | 
  36  |   await page.goto(`${BASE_URL}/parts`);
  37  |   await page.evaluate(([tokenVal, userVal]) => {
  38  |     localStorage.setItem('token', tokenVal);
  39  |     localStorage.setItem('user', JSON.stringify(userVal));
  40  |   }, [token, user]);
  41  | 
  42  |   await page.goto(`${BASE_URL}/parts`);
  43  |   await page.waitForLoadState('networkidle');
  44  | }
  45  | 
  46  | // ==================== API 测试 ====================
  47  | 
  48  | test.describe('API层验证', () => {
  49  | 
  50  |   test('Templates API - 创建模板', async () => {
  51  |     const { token } = await getAuthTokenAndUserId();
  52  | 
  53  |     const template = {
  54  |       category: `测试模板_${testTimestamp}`,
  55  |       paramDefs: [
  56  |         { key: 'voltage', label: '电压', unit: 'V', dataType: 'string', options: [], required: true },
  57  |         { key: 'power', label: '功率', unit: 'kW', dataType: 'number', options: [], required: false }
  58  |       ]
  59  |     };
  60  | 
  61  |     const res = await fetch(`${API_BASE}/templates`, {
  62  |       method: 'POST',
  63  |       headers: {
  64  |         Authorization: `Bearer ${token}`,
  65  |         'Content-Type': 'application/json'
  66  |       },
  67  |       body: JSON.stringify(template)
  68  |     });
  69  | 
  70  |     const data = await res.json();
  71  |     expect(res.ok).toBe(true);
  72  |     expect(data.id).toBeTruthy();
  73  |     createdTemplateId = data.id;
  74  |     console.log(`✓ API创建模板成功: ${data.category}, ID: ${data.id}`);
  75  |   });
  76  | 
  77  |   test('Templates API - 获取模板列表', async () => {
  78  |     const { token } = await getAuthTokenAndUserId();
  79  | 
  80  |     const res = await fetch(`${API_BASE}/templates`, {
  81  |       headers: { Authorization: `Bearer ${token}` }
  82  |     });
  83  | 
  84  |     const data = await res.json();
  85  |     expect(Array.isArray(data)).toBe(true);
  86  |     expect(data.length).toBeGreaterThan(0);
  87  |     console.log(`✓ API获取模板列表成功，共 ${data.length} 个模板`);
  88  |   });
  89  | 
  90  |   test('Parts API - 创建部件', async () => {
  91  |     const { token } = await getAuthTokenAndUserId();
  92  | 
  93  |     const part = {
  94  |       name: `测试部件_${testTimestamp}`,
  95  |       model: 'TEST-MODEL-001',
  96  |       description: '测试描述',
  97  |       manufacturer: '测试厂商',
  98  |       brand: '测试品牌',
  99  |       category: '测试类别',
  100 |       tags: ['API测试', '自动化'],
  101 |       specTemplateId: createdTemplateId,
  102 |       specs: [
  103 |         { key: 'voltage', label: '电压', value: '220', unit: 'V' },
  104 |         { key: 'power', label: '功率', value: '15', unit: 'kW' }
  105 |       ]
  106 |     };
  107 | 
  108 |     const res = await fetch(`${API_BASE}/parts`, {
  109 |       method: 'POST',
  110 |       headers: {
  111 |         Authorization: `Bearer ${token}`,
  112 |         'Content-Type': 'application/json'
  113 |       },
  114 |       body: JSON.stringify(part)
  115 |     });
  116 | 
  117 |     const data = await res.json();
  118 |     expect(res.ok).toBe(true);
  119 |     expect(data.id).toBeTruthy();
  120 |     createdPartId = data.id;
  121 |     console.log(`✓ API创建部件成功: ${data.name}, ID: ${data.id}`);
  122 |   });
  123 | 
  124 |   test('Parts API - 获取部件列表', async () => {
  125 |     const { token } = await getAuthTokenAndUserId();
  126 | 
  127 |     const res = await fetch(`${API_BASE}/parts`, {
  128 |       headers: { Authorization: `Bearer ${token}` }
  129 |     });
  130 | 
  131 |     const data = await res.json();
  132 |     expect(Array.isArray(data)).toBe(true);
> 133 |     expect(data.length).toBeGreaterThan(0);
      |                         ^ Error: expect(received).toBeGreaterThan(expected)
  134 | 
  135 |     // 验证我们有带specs的部件
  136 |     const partWithSpecs = data.find(p => p.specs && p.specs.length > 0);
  137 |     expect(partWithSpecs).toBeTruthy();
  138 |     console.log(`✓ API获取部件列表成功，共 ${data.length} 个部件，其中 ${partWithSpecs?.specs?.length} 个带规格`);
  139 |   });
  140 | });
  141 | 
  142 | // ==================== UI 测试 ====================
  143 | 
  144 | test.describe('UI层验证 - SpecTemplates页面', () => {
  145 | 
  146 |   test('验证模板列表显示', async ({ page }) => {
  147 |     await login(page);
  148 |     await page.goto(`${BASE_URL}/spec-templates`);
  149 |     await page.waitForLoadState('networkidle');
  150 |     await page.waitForTimeout(2000);
  151 | 
  152 |     // 等待表格加载
  153 |     const table = page.locator('.el-table');
  154 |     await expect(table).toBeVisible({ timeout: 10000 });
  155 | 
  156 |     // 验证有数据行
  157 |     const rows = page.locator('.el-table__body tr');
  158 |     const count = await rows.count();
  159 |     expect(count).toBeGreaterThan(0);
  160 | 
  161 |     console.log(`✓ 模板列表显示 ${count} 条数据`);
  162 |   });
  163 | 
  164 |   test('验证创建模板对话框', async ({ page }) => {
  165 |     await login(page);
  166 |     await page.goto(`${BASE_URL}/spec-templates`);
  167 |     await page.waitForLoadState('networkidle');
  168 |     await page.waitForTimeout(1000);
  169 | 
  170 |     // 点击添加按钮
  171 |     await page.click('button:has-text("Add Template")');
  172 |     await page.waitForTimeout(500);
  173 | 
  174 |     // 验证对话框出现
  175 |     const dialog = page.locator('.el-dialog:visible');
  176 |     await expect(dialog).toBeVisible();
  177 | 
  178 |     // 填写类别名称
  179 |     await page.fill('input[placeholder*="Motor"]', `UI测试模板_${testTimestamp}`);
  180 | 
  181 |     console.log('✓ 创建模板对话框正常');
  182 |   });
  183 | });
  184 | 
  185 | test.describe('UI层验证 - Parts页面', () => {
  186 | 
  187 |   test('验证部件列表显示', async ({ page }) => {
  188 |     await login(page);
  189 |     await page.goto(`${BASE_URL}/parts`);
  190 |     await page.waitForLoadState('networkidle');
  191 |     await page.waitForTimeout(2000);
  192 | 
  193 |     // 等待表格加载
  194 |     const table = page.locator('.el-table');
  195 |     await expect(table).toBeVisible({ timeout: 10000 });
  196 | 
  197 |     // 验证有数据行
  198 |     const rows = page.locator('.el-table__body tr');
  199 |     const count = await rows.count();
  200 |     expect(count).toBeGreaterThan(0);
  201 | 
  202 |     // 验证Tags列存在
  203 |     const tagsHeader = page.locator('.el-table__header th:has-text("Tags")');
  204 |     await expect(tagsHeader).toBeVisible();
  205 | 
  206 |     console.log(`✓ 部件列表显示 ${count} 条数据，Tags列存在`);
  207 |   });
  208 | 
  209 |   test('验证创建部件对话框', async ({ page }) => {
  210 |     await login(page);
  211 |     await page.goto(`${BASE_URL}/parts`);
  212 |     await page.waitForLoadState('networkidle');
  213 |     await page.waitForTimeout(1000);
  214 | 
  215 |     // 点击添加按钮
  216 |     await page.click('button:has-text("Add Part")');
  217 |     await page.waitForTimeout(500);
  218 | 
  219 |     // 验证对话框出现
  220 |     const dialog = page.locator('.el-dialog:visible');
  221 |     await expect(dialog).toBeVisible();
  222 | 
  223 |     // 验证表单字段
  224 |     const nameInput = dialog.locator('input').first();
  225 |     await expect(nameInput).toBeVisible();
  226 | 
  227 |     // 验证Template选择器存在
  228 |     const templateSelect = dialog.locator('.el-select').nth(1);
  229 |     await expect(templateSelect).toBeVisible();
  230 | 
  231 |     // 验证Tags选择器存在
  232 |     const tagsSelect = dialog.locator('.el-select').last();
  233 |     await expect(tagsSelect).toBeVisible();
```