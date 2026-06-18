import { chromium } from 'playwright';

const browser = await chromium.launch({ headless: true });
const page = await browser.newPage();

// Capture all console messages
const logs = [];
page.on('console', msg => {
  logs.push(`[${msg.type()}] ${msg.text()}`);
});
page.on('pageerror', err => {
  logs.push(`[PAGE_ERROR] ${err.message}`);
});

await page.goto('http://localhost:17000/login', { waitUntil: 'networkidle' });
await page.waitForTimeout(2000);

console.log('=== Console output after page load ===');
for (const l of logs) {
  console.log(l);
}
console.log(`=== Total: ${logs.length} messages ===`);

// Try logging in
const inputs = page.locator('.el-input__inner');
await inputs.nth(0).fill('admin');
await inputs.nth(1).fill('admin123');
await page.locator('button').filter({ hasText: 'Login' }).click();
await page.waitForTimeout(3000);

console.log('\n=== Console output after login ===');
for (const l of logs) {
  console.log(l);
}

await browser.close();
