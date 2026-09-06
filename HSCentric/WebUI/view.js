export const $ = (id) => document.getElementById(id);
export const escape = (value) => String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
export const time = (value) => value ? new Date(value).toLocaleTimeString('zh-CN', { hour12: false }) : '—';
export const range = (task) => task ? `${task.start.slice(0, 5)}–${task.stop.slice(0, 5)}${task.stop < task.start ? ' 次日' : ''}` : '未配置时段';
let toastTimer;
export function toast(message) { $('toast').textContent = message; $('toast').hidden = false; clearTimeout(toastTimer); toastTimer = setTimeout(() => $('toast').hidden = true, 6000); }
export function showError(id, message) { $(id).textContent = message; $(id).hidden = !message; }
export function confirmAction(title, message, accept = '确认') {
  $('confirm-title').textContent = title; $('confirm-copy').textContent = message; $('confirm-accept').textContent = accept;
  const dialog = $('confirm-dialog'); dialog.returnValue = ''; dialog.showModal();
  dialog.querySelector('[value="cancel"]').focus();
  return new Promise(resolve => dialog.addEventListener('close', () => resolve(dialog.returnValue === 'confirm'), { once: true }));
}
export function renderAccounts(accounts, selected, loaded) {
  const focused = document.activeElement?.dataset.account;
  const query = $('search').value.toLocaleLowerCase(); const filter = $('filter').value;
  const visible = accounts.filter(a => `${a.id} ${a.currentTask?.mode || ''}`.toLocaleLowerCase().includes(query))
    .filter(a => filter === 'all' || filter === 'enabled' && a.enable || filter === 'disabled' && !a.enable || filter === 'running' && a.running);
  $('account-count').textContent = accounts.length;
  $('list-summary').textContent = `${accounts.filter(a => a.enable).length} 个启用 · ${accounts.filter(a => a.running).length} 个运行中`;
  $('account-rows').innerHTML = visible.map(a => `<tr class="${a.id === selected ? 'selected' : ''}"><th scope="row"><button class="account-select" data-account="${escape(a.id)}" aria-pressed="${a.id === selected}">${escape(a.id)}</button><span class="state ${a.running ? 'running' : a.enable ? 'waiting' : 'off'}">${escape(a.status)}</span></th><td><strong>${escape(a.currentTask?.mode || '—')}</strong><small class="data">${escape(range(a.currentTask))}</small></td><td class="numeric"><strong>${escape(a.level)} <span class="unit">级</span></strong><small>${escape(a.xp)} XP</small></td><td class="numeric"><strong class="data">${escape(a.xpRate)}</strong><small>对战 + 任务 + 其他</small></td></tr>`).join('');
  $('list-empty').hidden = visible.length > 0;
  if (!visible.length) $('list-empty').innerHTML = `<div class="empty-rule" aria-hidden="true"></div><h3>${!loaded ? '正在读取账号' : accounts.length ? '没有匹配的账号' : '还没有账号编排'}</h3><p>${!loaded ? '连接本机后端，加载运行配置。' : accounts.length ? '换个关键词，或将状态切回「全部状态」。' : '添加第一个账号，设置连接信息和每日运行时段。'}</p>${loaded && !accounts.length ? '<button class="primary" data-add>添加第一个账号</button>' : ''}`;
  if (focused) [...$('account-rows').querySelectorAll('[data-account]')].find(el => el.dataset.account === focused)?.focus({ preventScroll: true });
}
const seconds = text => text.split(':').reduce((total, v, i) => total + Number(v) * [3600, 60, 1][i], 0);
function timeline(tasks, now) {
  const rects = tasks.flatMap((t, i) => {
    const start = seconds(t.start) / 86400 * 480, stop = seconds(t.stop) / 86400 * 480;
    return (stop < start ? [[start, 480 - start], [0, stop]] : [[start, stop - start]])
      .map(([x, w]) => `<rect data-action="edit-task" data-index="${i}" x="${x}" y="20" width="${Math.max(w, 1)}" height="16" class="segment segment-${i % 3}"><title>${escape(t.mode)} ${escape(range(t))}</title></rect>`);
  }).join('');
  const date = new Date(now), current = (date.getHours() * 3600 + date.getMinutes() * 60 + date.getSeconds()) / 86400 * 480;
  return `<svg class="timeline" viewBox="0 0 480 44" preserveAspectRatio="none" role="img" aria-label="每日时段分布，详细时段见下方列表"><path class="timeline-base" d="M0 28H480"/>${rects}<path class="now-line" d="M${current} 12v30"/><circle class="now-dot" cx="${current}" cy="10" r="3"/></svg><div class="timeline-labels data" aria-hidden="true"><span>00:00</span><span>06:00</span><span>12:00</span><span>18:00</span><span>24:00</span></div>`;
}
export function renderDetail(account, now, safeMode) {
  if (!account) {
    $('account-detail').innerHTML = '<div class="empty detail-empty"><h2>从一个账号开始</h2><p>选择已有账号，核对每日时段；<br>或添加账号，建立第一份运行编排。</p><svg class="empty-timeline" viewBox="0 0 280 50" aria-hidden="true"><path d="M0 25h280M0 19v12M70 19v12M140 19v12M210 19v12M280 19v12"/></svg></div>'; return;
  }
  const focused = document.activeElement;
  const focusedAction = focused?.matches('button[data-action]') ? focused.dataset.action : null;
  const focusedIndex = focused?.dataset.index;
  const focusedSummary = focused?.matches('.maintenance summary');
  $('account-detail').innerHTML = `<div class="detail-heading"><div><h2>${escape(account.id)}</h2><span class="state ${account.running ? 'running' : account.enable ? 'waiting' : 'off'}">${escape(account.status)}</span></div><button data-action="edit">编辑配置</button></div>
    <div class="account-facts"><span>佣兵 PVP <strong class="data">${escape(account.pvpRate)}</strong></span><span>传统等级 <strong>${escape(account.classicRate || '暂无记录')}</strong></span></div>
    <div class="schedule-title"><h3>每日时段</h3><span>${account.tasks.length} 段编排</span></div>${timeline(account.tasks, now)}
    <ol class="schedule-list">${account.tasks.map((t, i) => `<li><button data-action="edit-task" data-index="${i}"><span class="data">${escape(range(t))}</span><strong>${escape(t.mode)}</strong><small>${escape(t.mode === '酒馆' ? '酒馆模式' : `${t.teamName} · ${t.strategyName}`)}</small></button></li>`).join('')}</ol>
    ${account.switchTask ? '<p class="switch-note">已启用达标后自动换模式</p>' : ''}
    ${account.wakeTime && new Date(account.wakeTime).getFullYear() > 2000 && account.currentTask?.mode === '挂机收菜' ? `<p class="switch-note">收菜唤醒 ${time(account.wakeTime)}</p>` : ''}
    <div class="account-actions"><button data-action="${account.enable ? 'disable' : 'enable'}" class="${account.enable ? '' : 'primary'}">${account.enable ? '停用调度' : '启用调度'}</button><button data-action="start" ${safeMode ? 'disabled title="安全模式不启动游戏"' : ''}>启动一次</button><button data-action="pause">暂停 8 小时</button></div>
    <details class="maintenance"><summary>插件与维护</summary><div><button data-action="deploy" ${safeMode ? 'disabled' : ''}>部署插件配置</button><button data-action="backup">备份插件配置</button><button data-action="disable-plugin" ${safeMode ? 'disabled' : ''}>关闭插件</button><button data-action="reset-xp">重置经验效率</button><button data-action="delete" class="danger quiet">移除账号</button></div></details>`;
  if (focusedAction) [...$('account-detail').querySelectorAll('button[data-action]')].find(el => el.dataset.action === focusedAction && el.dataset.index === focusedIndex)?.focus({ preventScroll: true });
  else if (focusedSummary) $('account-detail').querySelector('.maintenance summary')?.focus({ preventScroll: true });
}
export function renderLogs(logs, autoScroll) {
  const filter = $('log-filter').value;
  const filtered = logs.filter(l => filter === 'all' || l.level === filter);
  const names = { Info: '信息', Error: '错误', Debug: '调试' };
  $('log-lines').innerHTML = filtered.length ? filtered.map(l => `<li class="log-line ${l.level === 'Error' ? 'log-error' : ''}"><time>${time(l.time)}</time><span class="log-level">${names[l.level] || escape(l.level)}</span><span>${escape(l.message)}</span></li>`).join('') : '<li class="log-empty">当前没有符合条件的运行记录。</li>';
  if (autoScroll) { const container = $('log-lines').parentElement; container.scrollTop = container.scrollHeight; }
}
