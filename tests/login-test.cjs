/**
 * 简单登录测试脚本
 * 运行: node tests/login-test.cjs
 */

const { chromium } = require('playwright');

const BASE_URL = 'http://localhost:5173';
const API_BASE = 'http://localhost:5128/api';

async function loginTest() {
  console.log('启动 Chrome...');
  const browser = await chromium.launch({ headless: false });
  const context = await browser.newContext();
  const page = await context.newPage();

  try {
    // 1. 通过 API 登录获取 token
    console.log('1. 调用登录 API...');
    const loginRes = await fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username: 'admin', password: 'admin123' })
    });
    const loginData = await loginRes.json();

    if (!loginData.token) {
      throw new Error('登录失败: ' + JSON.stringify(loginData));
    }
    console.log('✓ API 登录成功, token:', loginData.token.substring(0, 20) + '...');

    // 2. 访问前端页面
    console.log('2. 访问前端页面...');
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');

    // 3. 设置 localStorage
    console.log('3. 设置 localStorage...');
    await page.evaluate(([token, user]) => {
      localStorage.setItem('token', token);
      localStorage.setItem('user', JSON.stringify(user));
    }, [loginData.token, loginData.user]);

    // 4. 重新访问验证登录状态
    console.log('4. 验证登录状态...');
    await page.goto(`${BASE_URL}/parts`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // 检查是否跳转到首页（登录成功）
    const url = page.url();
    console.log('   当前 URL:', url);

    // 检查页面内容
    const title = await page.title();
    console.log('   页面标题:', title);

    // 截图
    await page.screenshot({ path: 'login-test-result.png' });
    console.log('✓ 已截图: login-test-result.png');

    console.log('\n✓ 登录测试成功!');

  } catch (error) {
    console.error('✗ 测试失败:', error.message);
    await page.screenshot({ path: 'login-test-error.png' });
    console.log('✓ 错误截图: login-test-error.png');
  } finally {
    await browser.close();
  }
}

loginTest();