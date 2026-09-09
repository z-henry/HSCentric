// Real API and browser checks against the disposable fixture from test-daily-statistics.ps1.
// No screenshots, live game processes, or production configuration are used.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { execFileSync } = require('node:child_process');
const { chromium } = require('playwright');
const root = path.resolve(__dirname, '..');
const runtime = fs.readFileSync(path.join(root, 'artifacts/daily-test-runtime.txt'), 'utf8').replace(/^\uFEFF/, '').trim();
assert.ok(runtime.startsWith(path.join(root, 'artifacts', 'daily-tests-')));
const base = 'http://127.0.0.1:17331';
let token = '', browser, owned = false;
const checks = [];
const check = (name, condition) => { assert.ok(condition, name); checks.push(name); };
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
async function api(route, method = 'GET', body, status = 200) {
  const response = await fetch(base + '/api' + route, { method, headers: { 'Content-Type': 'application/json', 'X-HSCentric-Token': token }, ...(body ? { body: JSON.stringify(body) } : {}) });
  const data = await response.json(); assert.equal(response.status, status, JSON.stringify(data)); return data;
}
async function start() {
  const executable = path.join(runtime, 'HSCentric.exe').replace(/'/g, "''");
  execFileSync('powershell.exe', ['-NoProfile', '-Command', `Start-Process -FilePath '${executable}' -ArgumentList '--safe-mode','--host=127.0.0.1','--port=17331','--no-browser' -WindowStyle Hidden`], { windowsHide: true });
  owned = true;
  for (let i = 0; i < 60; i++) {
    try { const state = await api('/state'); assert.ok(state.safeMode); token = (await api('/meta')).csrfToken; return state; } catch { await sleep(200); }
  }
  throw new Error('Isolated backend did not start');
}
async function stop() {
  await api('/shutdown', 'POST', {});
  for (let i = 0; i < 60; i++) { await sleep(200); try { await fetch(base + '/api/state'); } catch { owned = false; return; } }
  throw new Error('Isolated backend did not stop');
}
(async () => {
  let occupied = false; try { await fetch(base + '/api/state'); occupied = true; } catch {}
  assert.ok(!occupied, 'Test port must be free; existing backend is never touched');
  const state = await start();
  check('daily history loaded by real backend', state.accounts[0].todayStats.rate === 1000);
  let stats = await api('/accounts/daily-test-0/statistics');
  check('today, weighted seven-day and legacy history totals', stats.todayStats.xp === 2000 && stats.weekStats.rate === 933.33 && stats.historicalStats.rate === 950 && stats.hasLegacy);
  check('daily gaps stay null', stats.days.length === 7 && stats.days.at(-2).totals.rate === null);
  await api('/accounts/daily-test-0/statistics?from=invalid', 'GET', null, 400);
  await api('/accounts/daily-test-0/statistics?from=2020-01-01', 'GET', null, 400);
  await api('/accounts/daily-test-0/statistics?to=2999-01-01', 'GET', null, 400);
  check('invalid, excessive and future date ranges rejected', true);
  let account = await api('/accounts/daily-test-0');
  await api('/accounts/daily-test-0', 'PUT', { ...account, hsPath: 'D:\\synthetic\\edited.exe' });
  check('editing preserves daily data and historical totals', (await api('/accounts/daily-test-0/statistics')).historicalStats.xp === 3800);
  const oldSession = state.sessionId; await stop(); await start();
  check('daily data survives backend restart', (await api('/accounts/daily-test-0/statistics')).todayStats.xp === 2000 && (await api('/state')).sessionId !== oldSession);

  browser = await chromium.launch({ channel: 'msedge', headless: true });
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
  const errors = []; page.on('pageerror', e => errors.push(e.message)); page.on('console', m => { if (m.type() === 'error' && /Content Security Policy/.test(m.text())) errors.push(m.text()); });
  await page.goto(base); await page.locator('#account-rows tr').first().waitFor();
  check('initial view has five single-content columns and no selected row', await page.locator('#accounts thead th').count() === 5 && !await page.locator('body').evaluate(e => e.classList.contains('has-selection')));
  check('efficiency help and no motion checkbox', (await page.locator('.rate-help').getAttribute('title')).includes('对战 + 任务 + 其他') && await page.locator('.preview-motion').count() === 0);
  const fullWidth = await page.locator('.accounts-section').evaluate(e => e.getBoundingClientRect().width);
  await page.locator('[data-account="daily-test-0"] td').nth(1).click();
  await page.waitForFunction(() => document.querySelector('#statistics-metrics > div strong')?.textContent.trim() === '700 XP/小时');
  await page.waitForTimeout(650);
  check('whole row expands controls and real statistics', await page.locator('#account-controls').getAttribute('aria-hidden') === 'false' && await page.locator('.accounts-section').evaluate(e => e.getBoundingClientRect().width) < fullWidth);
  check('default motion is enabled without a checkbox', await page.locator('.workspace').evaluate(e => getComputedStyle(e).transitionDuration !== '0s'));
  const sourceBoxes = page.locator('#statistics-sources input');
  check('only battle component is selected by default', await sourceBoxes.nth(0).isChecked() && !await sourceBoxes.nth(1).isChecked() && !await sourceBoxes.nth(2).isChecked());
  const componentKeys = ['battleXp', 'questXp', 'otherXp'];
  const format = n => Number(n).toLocaleString('zh-CN', { maximumFractionDigits: 0 });
  for (let mask = 0; mask < 8; mask++) {
    for (let i = 0; i < 3; i++) await sourceBoxes.nth(i).setChecked(Boolean(mask & (1 << i)));
    const xp = totals => componentKeys.reduce((sum, key, i) => sum + (mask & (1 << i) ? totals[key] : 0), 0);
    const currentXp = xp(stats.todayStats), expectedRate = mask ? format(currentXp * 3600 / stats.todayStats.runtimeSeconds) : '—';
    assert.equal((await page.locator('#statistics-metrics > div').nth(0).locator('strong').textContent()).trim(), `${expectedRate} XP/小时`);
    assert.equal((await page.locator('#statistics-metrics > div').nth(1).locator('strong').textContent()).trim(), `${mask ? format(xp(stats.weekStats) * 3600 / stats.weekStats.runtimeSeconds) : '—'} XP/小时`);
    assert.equal((await page.locator('#statistics-metrics > div').nth(2).locator('strong').textContent()).trim(), `${mask ? format(xp(stats.historicalStats) * 3600 / stats.historicalStats.runtimeSeconds) : '—'} XP/小时`);
    assert.equal(await page.locator('#statistics-rows tr').first().locator('td').nth(0).textContent(), mask ? `+${format(currentXp)} XP` : '—');
    assert.equal(await page.locator('#statistics-rows tr').first().locator('td').nth(1).textContent(), '2小时00分');
    if (mask) {
      await page.locator('.chart-point').last().focus();
      assert.ok((await page.locator('#chart-readout').textContent()).includes(`效率 ${expectedRate} XP/小时，经验 +${format(currentXp)} XP`));
      assert.ok(await page.locator('.chart-average').count() === 1);
      if (mask === 4) assert.ok((await page.locator('.chart-point').first().getAttribute('aria-label')).includes('效率 0 XP/小时'));
    } else {
      assert.equal(await page.locator('.chart-point').count(), 0);
      assert.ok((await page.locator('#efficiency-chart').textContent()).includes('请选择至少一项'));
    }
  }
  check('all eight component combinations update summaries, curve, tooltip and details with unchanged runtime', true);
  await page.waitForTimeout(3200);
  check('polling preserves component selection', await page.locator('#statistics-sources input:checked').count() === 3 && (await page.locator('#statistics-metrics > div').first().locator('strong').textContent()).trim() === '1,000 XP/小时');
  check('curve breaks across missing dates', ((await page.locator('.chart-path').getAttribute('d')).match(/M/g) || []).length === 2);
  await page.locator('.chart-point').last().focus(); check('point focus displays daily details', (await page.locator('#chart-readout').textContent()).includes('2,000'));
  await page.locator('[data-period="30"]').click(); await page.waitForFunction(() => document.querySelector('#statistics-expand').textContent.includes('30'));
  await page.locator('#statistics-expand').click(); check('30-day detail expansion', await page.locator('#statistics-rows tr').count() === 30);
  await page.locator('[data-period="custom"]').click(); await page.locator('#statistics-from').fill(stats.today); await page.locator('#statistics-to').fill(stats.today); await page.locator('#statistics-range button').click();
  await page.waitForFunction(() => document.querySelectorAll('#statistics-rows tr').length === 1);
  check('custom one-day range renders', await page.locator('.chart-point').count() === 1);
  check('date changes preserve component selection', await page.locator('#statistics-sources input:checked').count() === 3);
  await page.locator('[data-action="collapse"]').click(); await page.waitForTimeout(650);
  check('collapse restores table and row focus', await page.locator('.statistics-reveal').evaluate(e => e.getBoundingClientRect().height) === 0 && await page.evaluate(() => document.activeElement.dataset.account) === 'daily-test-0');
  await page.keyboard.press('Enter'); await page.waitForTimeout(650); await page.locator('[data-account="daily-test-0"] td').first().click(); await page.waitForTimeout(650);
  check('keyboard selection and row deselection', !await page.locator('body').evaluate(e => e.classList.contains('has-selection')));

  // Delay a previous selection to verify its response cannot repaint a new account.
  let release;
  const gate = new Promise(resolve => { release = resolve; });
  await page.route('**/api/accounts/daily-test-0/statistics?*', async route => { await gate; await route.continue(); });
  await page.locator('[data-account="daily-test-0"] td').first().click();
  await page.locator('[data-account="daily-test-1"] td').first().click();
  await page.waitForFunction(() => document.querySelector('#statistics-account').textContent === 'daily-test-1' && document.querySelector('#statistics-metrics').textContent.includes('1,000'));
  release(); await page.waitForTimeout(300); await page.unroute('**/api/accounts/daily-test-0/statistics?*');
  check('late response cannot overwrite new selection', await page.locator('#statistics-account').textContent() === 'daily-test-1' && (await page.locator('#efficiency-chart').textContent()).includes('暂无效率数据'));
  check('account changes preserve component selection', await page.locator('#statistics-sources input:checked').count() === 3);
  await page.route('**/api/accounts/daily-test-2/statistics?*', route => route.fulfill({ status: 500, contentType: 'application/json', body: JSON.stringify({ error: 'test statistics unavailable' }) }));
  await page.locator('[data-account="daily-test-2"] td').first().click(); await page.locator('#statistics-retry').waitFor({ state: 'visible' });
  check('statistics error is explicit', (await page.locator('#statistics-status').textContent()).includes('unavailable'));
  await page.unroute('**/api/accounts/daily-test-2/statistics?*'); await page.locator('#statistics-retry').click(); await page.locator('#statistics-retry').waitFor({ state: 'hidden' });
  await page.locator('#search').fill('daily-test-0'); check('filter clears selection', !await page.locator('body').evaluate(e => e.classList.contains('has-selection')));
  await page.locator('#search').fill(''); await page.locator('[data-account="daily-test-0"] td').first().click();
  await page.locator('[data-action="edit"]').click(); await page.locator('#account-dialog[open]').waitFor();
  await page.locator('[name="hsPath"]').fill('D:\\synthetic\\browser-edited.exe'); await page.locator('#editor-save').click(); await page.locator('#account-dialog').waitFor({ state: 'hidden' });
  check('existing editor still saves through new layout', (await api('/accounts/daily-test-0')).hsPath.includes('browser-edited'));
  for (const width of [1100, 768, 390, 320]) { await page.setViewportSize({ width, height: 900 }); await page.waitForTimeout(650); check('no page overflow at ' + width, await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)); }
  check('no script or CSP errors', errors.length === 0);
  await api('/accounts/daily-test-0/actions', 'POST', { action: 'reset-xp' });
  stats = await api('/accounts/daily-test-0/statistics');
  check('explicit reset clears daily and legacy history together', stats.historicalStats.xp === 0 && stats.todayStats.runtimeSeconds === 0 && !stats.hasLegacy && stats.days.every(d => !d.recorded));
  await stop(); await start();
  check('reset persists after restart', (await api('/accounts/daily-test-0/statistics')).historicalStats.xp === 0);
  console.log(JSON.stringify({ passed: checks.length, checks }, null, 2));
})().catch(error => { console.error(error); process.exitCode = 1; }).finally(async () => { if (browser) await browser.close(); if (owned) { try { await stop(); } catch (e) { console.error(e.message); } } });
