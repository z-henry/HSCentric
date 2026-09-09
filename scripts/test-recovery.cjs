// Targeted regression tests for backend-session recovery and keyboard focus.
// Requires the synthetic accounts created by test-webui.cjs in a safe-mode host.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { execFileSync } = require('node:child_process');
const { chromium } = require('playwright');
const root = path.resolve(__dirname, '..');
const runtime = path.join(root, 'artifacts', 'verify');
const base = 'http://127.0.0.1:17322';
const results = [];
let token;
async function api(route, method = 'GET', body) {
  const response = await fetch(base + '/api' + route, { method, headers: { 'Content-Type': 'application/json', 'X-HSCentric-Token': token || '' }, ...(body ? { body: JSON.stringify(body) } : {}) });
  const data = await response.json(); assert.equal(response.status, 200, JSON.stringify(data)); return data;
}
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
(async () => {
  assert.ok((await api('/state')).safeMode);
  const metadata = await api('/meta'); token = metadata.csrfToken;
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
  const errors = []; page.on('pageerror', e => errors.push(e.message));
  await page.goto(base); await page.getByText('后端已连接', { exact: true }).waitFor();
  await page.locator('[data-account="测试账号 · 收菜"] td').first().click();
  const second = page.locator('.schedule-list button').nth(1); await second.focus();
  const route = '/accounts/' + encodeURIComponent('测试账号 · 收菜');
  let account = await api(route); const originalTeam = account.tasks[1].teamName;
  account.tasks[1].teamName = '键盘焦点验证'; await api(route, 'PUT', account);
  await page.locator('#account-detail').getByText(/键盘焦点验证/).waitFor();
  assert.equal(await page.evaluate(() => document.activeElement?.dataset.index), '1');
  assert.equal(await page.evaluate(() => document.activeElement?.tagName), 'BUTTON');
  results.push('second schedule button keeps exact focus across actual state refresh');
  await page.locator('.maintenance summary').focus();
  account = await api(route); account.tasks[1].teamName = originalTeam; await api(route, 'PUT', account);
  await page.locator('#account-detail').getByText(/键盘焦点验证/).waitFor({ state: 'hidden' });
  assert.equal(await page.evaluate(() => document.activeElement?.tagName), 'SUMMARY');
  results.push('maintenance summary keeps keyboard focus across state refresh');
  await page.getByRole('button', { name: '编辑配置', exact: true }).click();
  await page.locator('[name="hsPath"]').fill('D:\\unsaved-after-restart.exe');
  await page.locator('#task-add').click(); await page.locator('#task-mode').selectOption('PVP');
  const oldSession = (await api('/state')).sessionId; assert.ok(oldSession);
  await api('/shutdown', 'POST', {});
  for (let i = 0; i < 50; i++) { try { await fetch(base + '/api/state'); await sleep(200); } catch { break; } }
  const executable = path.join(runtime, 'HSCentric.exe').replace(/'/g, "''");
  execFileSync('powershell.exe', ['-NoProfile', '-Command', `Start-Process -FilePath '${executable}' -ArgumentList '--safe-mode','--no-browser','--port=17322' -WindowStyle Hidden`], { windowsHide: true });
  let restarted;
  for (let i = 0; i < 80; i++) { try { restarted = await api('/meta'); if (restarted.sessionId !== oldSession) break; } catch {} await sleep(250); }
  assert.notEqual(restarted.sessionId, oldSession); token = restarted.csrfToken;
  const startup = (await api('/logs'))[0];
  const startupTime = new Date(startup.time).toLocaleTimeString('zh-CN', { hour12: false });
  await page.waitForFunction(time => document.querySelectorAll('#log-lines .log-line').length === 1 && document.querySelector('#log-lines time')?.textContent === time, startupTime);
  assert.equal(await page.locator('[name="hsPath"]').inputValue(), 'D:\\unsaved-after-restart.exe');
  assert.equal(await page.locator('#task-mode').inputValue(), 'PVP');
  results.push('backend restart resets the log cursor while preserving form and task drafts');
  await page.locator('#task-cancel').click(); await page.locator('#editor-close').click(); await page.locator('#confirm-accept').click();
  await page.locator('#account-detail button[data-action="enable"]').click();
  await page.waitForFunction(() => document.querySelector('#log-lines').innerText.includes('操作完成：enable'));
  assert.ok((await api(route)).enable);
  results.push('refreshed session token supports post-restart actions and new low-sequence logs');
  await api(route + '/actions', 'POST', { action: 'disable' });
  await page.waitForFunction(() => document.querySelector('#account-detail').innerText.includes('已停用'));
  await page.evaluate(() => document.fonts.ready);
  assert.ok(await page.evaluate(() => document.fonts.check('650 28px "HSCentric Display"', '运行编排台')));
  assert.ok((await page.locator('h1').evaluate(el => getComputedStyle(el).fontFamily)).includes('HSCentric Display'));
  results.push('self-hosted Chinese display font loads successfully');
  const shots = path.join(root, '.impeccable', 'review');
  await page.locator('#toast').waitFor({ state: 'hidden' });
  for (const [width, height, name] of [[1440,1000,'desktop'],[768,1000,'tablet'],[375,900,'mobile'],[320,900,'small-mobile']]) {
    await page.setViewportSize({ width, height }); await page.evaluate(() => window.scrollTo(0, 0));
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    await page.screenshot({ path: path.join(shots, name + '.png'), fullPage: true });
  }
  await page.getByRole('button', { name: '编辑配置', exact: true }).click();
  await page.screenshot({ path: path.join(shots, 'mobile-editor.png'), fullPage: true });
  await page.keyboard.press('Escape');
  assert.equal(errors.length, 0, errors.join('\n'));
  await browser.close();
  fs.writeFileSync(path.join(shots, 'recovery-results.json'), JSON.stringify({ passed: results, errors }, null, 2));
  console.log(JSON.stringify({ passed: results.length, checks: results }, null, 2));
})().catch(error => { console.error(error); process.exit(1); });
