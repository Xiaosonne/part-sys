const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  // First login
  console.log('Navigating and logging in...');
  await page.goto('http://localhost:5174/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);

  await page.locator('input[type="text"]').fill('admin');
  await page.locator('input[type="password"]').fill('admin123');

  await page.locator('button:has-text("Login")').click();
  await page.waitForTimeout(3000);

  // Click on 选型管理 menu item
  console.log('Clicking 选型管理 menu...');
  await page.locator('.el-menu-item:has-text("选型管理")').click();
  await page.waitForTimeout(2000);

  // Click on the project item (TestProjectA)
  const projectItem = page.locator('.el-tree-node__content:has-text("TestProjectA")');
  if (await projectItem.count() > 0) {
    await projectItem.click();
    await page.waitForTimeout(1000);
  }

  // Click + 新建选型单 button
  const newBtn = page.locator('button:has-text("+ 新建选型单")');
  if (await newBtn.count() > 0) {
    await newBtn.click();
    await page.waitForTimeout(1500);

    // Fill name
    await page.locator('input[placeholder="选型单名称"]').click({ clickCount: 3 });
    await page.locator('input[placeholder="选型单名称"]').type('TestSelectionA');
    await page.waitForTimeout(500);

    // Click first dropdown to open it
    console.log('Clicking project dropdown...');
    await page.locator('.el-select').first().click();
    await page.waitForTimeout(1000);

    // Click the option in the dropdown list - use the visible dropdown
    const dropdown = page.locator('.el-select-dropdown');
    if (await dropdown.isVisible()) {
      await page.locator('.el-select-dropdown__item').filter({ hasText: 'falseTestProjectA' }).click();
      await page.waitForTimeout(500);
    }

    await page.waitForTimeout(500);
    await page.screenshot({ path: 'test-step8-selected.png', fullPage: true });

    // Click 创建 button using JavaScript click since it's in a dialog
    console.log('Clicking 创建 button...');
    await page.evaluate(() => {
      const buttons = Array.from(document.querySelectorAll('button'));
      const createBtn = buttons.find(b => b.textContent === '创建' && b.classList.contains('el-button--primary'));
      if (createBtn) createBtn.click();
    });
    await page.waitForTimeout(3000);
    await page.screenshot({ path: 'test-step9-after-create.png', fullPage: true });

    // Check if form closed and new selection appeared
    const treeItems = await page.locator('.el-tree-node__content').allTextContents();
    console.log('Tree items after create:', treeItems);
  }

  console.log('Done!');
  await browser.close();
})();
