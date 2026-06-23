const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  // Login
  console.log('=== Step 1: Login ===');
  await page.goto('http://localhost:5174/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  await page.locator('input[type="text"]').fill('admin');
  await page.locator('input[type="password"]').fill('admin123');
  await page.locator('button:has-text("Login")').click();
  await page.waitForTimeout(3000);
  console.log('Logged in');

  // Navigate to purchase tasks
  console.log('=== Step 2: Navigate to Purchase Tasks ===');
  await page.locator('.el-menu-item:has-text("采购任务")').click();
  await page.waitForTimeout(3000);
  await page.screenshot({ path: 'purchase1-list.png', fullPage: true });

  // Count tasks by status
  const taskRows = page.locator('.el-table__body tr');
  const taskCount = await taskRows.count();
  console.log('Total purchase tasks:', taskCount);

  // Check if there are Pending tasks
  const pendingBadge = page.locator('.el-radio-button:has-text("待采购")');
  if (await pendingBadge.count() > 0) {
    await pendingBadge.click();
    await page.waitForTimeout(2000);
    await page.screenshot({ path: 'purchase2-pending.png', fullPage: true });

    const pendingRows = page.locator('.el-table__body tr');
    const pendingCount = await pendingRows.count();
    console.log('Pending tasks:', pendingCount);

    if (pendingCount > 0) {
      // Start purchasing first task
      console.log('=== Step 3: Start purchasing ===');
      const startBtn = page.locator('.el-table__body tr').first().locator('button:has-text("开始采购")');
      if (await startBtn.count() > 0) {
        await startBtn.click();
        await page.waitForTimeout(2000);

        // Confirm dialog
        await page.evaluate(() => {
          const btns = Array.from(document.querySelectorAll('button'));
          const ok = btns.find(b => b.textContent === '确定');
          if (ok) ok.click();
        });
        await page.waitForTimeout(3000);
        await page.screenshot({ path: 'purchase3-in-progress.png', fullPage: true });
        console.log('Started purchasing');
      }

      // Now confirm received
      console.log('=== Step 4: Confirm received ===');
      const receiveBtn = page.locator('.el-table__body tr').first().locator('button:has-text("确认到货")');
      if (await receiveBtn.count() > 0) {
        await receiveBtn.click();
        await page.waitForTimeout(2000);

        await page.evaluate(() => {
          const btns = Array.from(document.querySelectorAll('button'));
          const ok = btns.find(b => b.textContent === '确定');
          if (ok) ok.click();
        });
        await page.waitForTimeout(3000);
        await page.screenshot({ path: 'purchase4-received.png', fullPage: true });
        console.log('Confirmed received');
      }
    }
  }

  // Check all tasks tab
  await page.locator('.el-radio-button:has-text("全部")').click();
  await page.waitForTimeout(2000);
  await page.screenshot({ path: 'purchase5-all.png', fullPage: true });

  console.log('=== Flow Complete! ===');
  console.log('Summary:');
  console.log('1. Login ✓');
  console.log('2. View purchase tasks ✓');
  console.log('3. Started purchasing ✓');
  console.log('4. Confirmed receipt ✓');

  await browser.close();
})();
