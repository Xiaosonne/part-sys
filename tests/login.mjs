import { chromium } from 'playwright';

const browser = await chromium.launch({ headless: false });
const page = await browser.newPage();
await page.goto('http://localhost:17000/login');
await page.waitForSelector('.el-input__inner', { timeout: 10000 });

const inputs = page.locator('.el-input__inner');
await inputs.nth(0).fill('admin');
await inputs.nth(1).fill('admin123');

await page.locator('button').filter({ hasText: 'Login' }).click();

await page.waitForTimeout(2000);
console.log('URL after login:', page.url());

await page.waitForTimeout(300000);
await browser.close();
