const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  // Login
  console.log('Logging in...');
  await page.goto('http://localhost:5174/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  await page.locator('input[type="text"]').fill('admin');
  await page.locator('input[type="password"]').fill('admin123');
  await page.locator('button:has-text("Login")').click();
  await page.waitForTimeout(3000);

  // Navigate to selections
  console.log('Navigating to selections...');
  await page.locator('.el-menu-item:has-text("选型管理")').click();
  await page.waitForTimeout(2000);

  // The tree has default-expand-all, so we should see project + selection children
  // But clicking the project selects it and shows table view, hiding tree selection nodes
  // Solution: Click selection node directly without selecting project first

  // Try clicking selection plan directly
  console.log('Looking for selection plan directly in tree...');

  // Get all tree node contents and print them
  const treeNodes = page.locator('.selection-tree .el-tree-node__content');
  const count = await treeNodes.count();
  console.log('Total tree nodes:', count);
  for (let i = 0; i < count; i++) {
    const text = await treeNodes.nth(i).textContent();
    console.log(`  Node ${i}: "${text.trim()}"`);
  }

  // Now try to click the selection plan
  console.log('Clicking selection plan...');
  const selectionPlan = page.locator('.selection-tree .el-tree-node__content:has-text("TestSelectionA")');
  const selCount = await selectionPlan.count();
  console.log('Selection plan nodes found:', selCount);

  if (selCount > 0) {
    // Check if visible
    const isVisible = await selectionPlan.first().isVisible();
    console.log('Is visible:', isVisible);

    if (isVisible) {
      await selectionPlan.first().click();
      await page.waitForTimeout(2000);
    } else {
      // Not visible - parent is collapsed. Expand project first.
      console.log('Selection not visible, expanding project...');
      const projectItem = page.locator('.selection-tree .el-tree-node__content:has-text("TestProjectA")');
      await projectItem.click();
      await page.waitForTimeout(1000);
      // Now try selection again
      await selectionPlan.first().click();
      await page.waitForTimeout(2000);
    }

    await page.screenshot({ path: 'debug-after-plan-click.png', fullPage: true });

    // Now click 添加配件 button
    console.log('Clicking 添加配件 button...');
    const addBtn = page.locator('button:has-text("+ 添加配件")');
    if (await addBtn.count() > 0) {
      await addBtn.click();
      await page.waitForTimeout(3000);
      await page.screenshot({ path: 'debug-parts-dialog.png', fullPage: true });

      // In the dialog, click a row in the results table
      const rows = page.locator('.el-dialog .el-table__body tr');
      const rowCount = await rows.count();
      console.log('Part rows in dialog:', rowCount);

      if (rowCount > 0) {
        await rows.first().click();
        await page.waitForTimeout(1000);
        await page.screenshot({ path: 'debug-part-selected.png', fullPage: true });

        // Set quantity in the form
        const qtyInput = page.locator('.el-dialog .add-form .el-input-number input');
        if (await qtyInput.count() > 0) {
          await qtyInput.fill('5');
          await page.waitForTimeout(500);
        }

        // Click 添加 button
        const addConfirmBtn = page.locator('.el-dialog__footer button:has-text("添加")');
        if (await addConfirmBtn.count() > 0) {
          await addConfirmBtn.click();
          await page.waitForTimeout(3000);
          await page.screenshot({ path: 'debug-part-added.png', fullPage: true });
          console.log('Part added successfully!');
        } else {
          console.log('添加 button not found');
        }
      } else {
        console.log('No part rows found, clicking search first...');
        const searchBtn = page.locator('.el-dialog button:has-text("搜索")');
        if (await searchBtn.count() > 0) {
          await searchBtn.click();
          await page.waitForTimeout(3000);
          await page.screenshot({ path: 'debug-after-search.png', fullPage: true });

          const rowsAfter = page.locator('.el-dialog .el-table__body tr');
          const newCount = await rowsAfter.count();
          console.log('Rows after search:', newCount);

          if (newCount > 0) {
            await rowsAfter.first().click();
            await page.waitForTimeout(1000);

            const qtyInput = page.locator('.el-dialog .add-form .el-input-number input');
            if (await qtyInput.count() > 0) {
              await qtyInput.fill('5');
            }
            await page.waitForTimeout(500);

            const addConfirmBtn = page.locator('.el-dialog__footer button:has-text("添加")');
            if (await addConfirmBtn.count() > 0) {
              await addConfirmBtn.click();
              await page.waitForTimeout(3000);
              await page.screenshot({ path: 'debug-final.png', fullPage: true });
              console.log('Part added!');
            }
          }
        }
      }
    } else {
      console.log('添加配件 button not found');
      // Print all buttons
      const btns = await page.locator('button').allTextContents();
      console.log('All buttons:', btns);
    }
  }

  console.log('Done!');
  await browser.close();
})();
