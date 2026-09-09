import { request, connect, accountPath } from './api.js';
import { $, time, toast, showError, confirmAction, renderAccounts, renderDetail, renderLogs } from './view.js';
import { openEditor, setupEditor } from './editor.js';
import { showStatistics } from './statistics.js';
let meta, state, selected = null, logs = [], lastLog = 0, loaded = false, autoScroll = true, polling = false, stopped = false, actionBusy = false;
let listSignature = '', detailSignature = '';
let sessionId = null;
async function bootstrap() {
  try { meta = await connect(); setupEditor(meta, async id => { selected = id; await poll(); }); await poll(); }
  catch (error) { disconnected(error); }
}
function disconnected(error) {
  $('connection').textContent = stopped ? '后端已停止' : '连接已断开'; $('connection').className = 'connection offline';
  $('connection-error').hidden = false; $('connection-error').querySelector('span').textContent = error.message;
  $('check-status').textContent = loaded ? '显示上次连接时的状态' : '尚未连接后端';
  if (!loaded) { $('list-empty').querySelector('h3').textContent = '尚未读取到账号'; $('list-empty').querySelector('p').textContent = '请先恢复后端连接。'; }
}
async function poll() {
  if (polling || stopped) return;
  polling = true;
  try {
    if (!meta) { meta = await connect(); setupEditor(meta, async id => { selected = id; await poll(); }); }
    const next = await request('/state');
    if (sessionId !== next.sessionId) {
      // Backend sequences and CSRF tokens belong to one process lifetime.
      // Refresh only metadata; do not reopen or reset the account editor draft.
      meta = await connect();
      setupEditor(meta, async id => { selected = id; await poll(); });
      sessionId = next.sessionId; lastLog = 0; logs = [];
      renderLogs(logs, autoScroll);
    }
    state = next; loaded = true;
    if (!state.accounts.some(a => a.id === selected)) selected = null;
    $('connection-error').hidden = true; $('connection').textContent = '后端已连接'; $('connection').className = 'connection';
    $('clock').textContent = time(state.now); $('clock').dateTime = state.now;
    $('check-status').textContent = state.safeMode ? '安全模式 · 自动调度已停用' : state.checking ? '正在检测运行状态' : `下次检测 ${time(state.nextCheck)}`;
    $('safe-notice').hidden = !state.safeMode;
    render();
    const entries = await request(`/logs?after=${lastLog}`);
    if (entries.length) { logs = [...logs, ...entries].slice(-500); lastLog = entries.at(-1).id; renderLogs(logs, autoScroll); }
  } catch (error) { disconnected(error); }
  finally { polling = false; }
}
function render(force = false) {
  if (!state) return;
  $('app-version').textContent = meta.version ? `中控 · ${meta.version}` : '中控';
  document.title = meta.version ? `HSCentric ${meta.version} · 运行编排台` : 'HSCentric · 运行编排台';
  const listKey = JSON.stringify([state.accounts, selected, $('search').value, $('filter').value]);
  if (force || listKey !== listSignature) { renderAccounts(state.accounts, selected, loaded); listSignature = listKey; }
  const account = state.accounts.find(a => a.id === selected);
  const open = !!account;
  document.body.classList.toggle('has-selection', open);
  for (const id of ['account-controls', 'statistics-reveal']) { $(id).inert = !open; $(id).setAttribute('aria-hidden', String(!open)); }
  $('selection-hint').textContent = open ? '再次点击已选行可收起' : '点击任意一行，展开账号控制与经验统计';
  showStatistics(selected, state.now);
  const detailKey = JSON.stringify([account, state.safeMode, new Date(state.now).getMinutes()]);
  if (account && (force || detailKey !== detailSignature)) {
    const expanded = $('account-detail').querySelector('details')?.open;
    renderDetail(account, state.now, state.safeMode); detailSignature = detailKey;
    if (expanded && $('account-detail').querySelector('details')) $('account-detail').querySelector('details').open = true;
  }
  if (!account) detailSignature = '';
}
async function edit(id = null, taskIndex = null) { try { await openEditor(id, taskIndex); } catch (error) { toast(error.message); } }
$('add-account').onclick = () => edit();
$('list-empty').onclick = event => { if (event.target.closest('[data-add]')) edit(); };
function selectRow(row) {
  if (!row) return;
  selected = selected === row.dataset.account ? null : row.dataset.account; render();
}
$('account-rows').onclick = event => selectRow(event.target.closest('[data-account]'));
$('account-rows').onkeydown = event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); selectRow(event.target.closest('[data-account]')); } };
$('search').oninput = () => { selected = null; render(); }; $('filter').onchange = () => { selected = null; render(); };
$('retry').onclick = () => { meta = null; stopped = false; bootstrap(); };
$('account-detail').onclick = async event => {
  const button = event.target.closest('[data-action]'); if (!button || !selected || actionBusy) return;
  const id = selected, action = button.dataset.action;
  if (action === 'collapse') {
    selected = null; render();
    [...$('account-rows').querySelectorAll('[data-account]')].find(row => row.dataset.account === id)?.focus({ preventScroll: true }); return;
  }
  if (action === 'edit' || action === 'edit-task') return edit(id, action === 'edit-task' ? Number(button.dataset.index) : null);
  const confirmations = {
    delete: ['移除账号？', `移除「${id}」的中控配置与编排。已经启动的游戏进程不会因此关闭。`, '移除账号'],
    'reset-xp': ['重置全部经验统计？', `清空「${id}」的每日记录、历史经验和累计运行时间。此操作无法恢复，下一次经验采样后重新开始统计。`, '清空全部统计'],
    deploy: ['部署插件配置？', `将「${id}」的中控设置写入本机游戏插件配置。`, '部署配置'],
    'disable-plugin': ['关闭插件？', `修改「${id}」的插件开关。自动调度仍可能在下次启动时重新部署设置。`, '关闭插件']
  };
  if (confirmations[action] && !await confirmAction(...confirmations[action])) return;
  actionBusy = true; button.disabled = true;
  try {
    const result = await request(action === 'delete' ? accountPath(id) : `${accountPath(id)}/actions`, { method: action === 'delete' ? 'DELETE' : 'POST', ...(action === 'delete' ? {} : { body: { action } }) });
    toast(result.message); await poll();
  } catch (error) { toast(error.message); }
  finally { actionBusy = false; if (button.isConnected) button.disabled = false; }
};
$('log-filter').onchange = () => renderLogs(logs, autoScroll);
$('log-pause').onclick = () => { autoScroll = !autoScroll; $('log-pause').textContent = autoScroll ? '暂停滚动' : '恢复滚动'; $('log-pause').setAttribute('aria-pressed', String(!autoScroll)); $('log-status').textContent = autoScroll ? '最近 500 条' : '已暂停滚动 · 继续接收日志'; };
$('log-download').onclick = () => {
  const blob = new Blob([logs.map(l => `${l.time} [${l.level}] ${l.message}`).join('\r\n')], { type: 'text/plain;charset=utf-8' });
  const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = `hscentric-${new Date().toISOString().slice(0, 10)}.log`; link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
};
$('settings-open').onclick = async () => {
  try {
    const fresh = await connect(); $('battle-net-path').value = fresh.battleNetPath; showError('settings-error', '');
    const checkbox = $('run-at-login'); checkbox.disabled = true; checkbox.checked = false;
    $('startup-help').textContent = '正在读取开机启动设置…'; $('settings-dialog').showModal();
    try {
      const startup = await request('/startup'); checkbox.checked = startup.enabled; checkbox.disabled = !startup.canChange;
      $('startup-help').textContent = startup.disabledReason || (startup.canChange ? '后端电脑的当前用户登录后启动，不打开浏览器。更改后立即生效。' : '安全模式不修改开机启动设置。');
    } catch (error) { $('startup-help').textContent = error.message; }
  }
  catch (error) { toast(error.message); }
};
$('run-at-login').onchange = async event => {
  const checkbox = event.currentTarget, enabled = checkbox.checked; checkbox.disabled = true; $('settings-open').disabled = true;
  showError('settings-error', '');
  try { const result = await request('/startup', { method: 'PUT', body: { enabled } }); toast(result.message); }
  catch (error) { checkbox.checked = !enabled; showError('settings-error', error.message); }
  finally { checkbox.disabled = false; $('settings-open').disabled = false; }
};
$('settings-close').onclick = () => $('settings-dialog').close();
$('settings-form').onsubmit = async event => {
  event.preventDefault(); const button = event.submitter; button.disabled = true;
  try { await request('/settings', { method: 'PUT', body: { battleNetPath: $('battle-net-path').value } }); toast('运行设置已保存。'); $('settings-dialog').close(); }
  catch (error) { showError('settings-error', error.message); }
  finally { button.disabled = false; }
};
$('shutdown').onclick = async () => {
  if (!await confirmAction('停止后端服务？', '调度和自动检测将停止。配置会保存，恢复服务需要重新启动 HSCentric。', '停止服务')) return;
  try { await request('/shutdown', { method: 'POST', body: {} }); stopped = true; $('settings-dialog').close(); disconnected(new Error('后端正在停止。重新启动 HSCentric 后，可点击重新连接。')); }
  catch (error) { showError('settings-error', error.message); }
};
setInterval(poll, 3000);
document.addEventListener('visibilitychange', () => { if (!document.hidden) poll(); });
bootstrap();
