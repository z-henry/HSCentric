import { request, accountPath } from './api.js';
import { $, escape, showError } from './view.js';

const number = value => Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 0 });
const rate = value => value == null ? '—' : number(value);
const duration = seconds => `${Math.floor(seconds / 3600)}小时${String(Math.floor(seconds % 3600 / 60)).padStart(2, '0')}分`;
const components = new Map([['battleXp', '对局'], ['questXp', '任务'], ['otherXp', '其他']]);
let selectedComponents = ['battleXp'], displayData = null;
const componentLabel = () => selectedComponents.map(key => components.get(key)).join(' + ');
const xpLabel = totals => totals.xp == null ? '—' : `+${number(totals.xp)} XP`;
function selectedTotals(totals) {
  const xp = selectedComponents.length ? selectedComponents.reduce((sum, key) => sum + Number(totals[key] || 0), 0) : null;
  // Calculate once from raw component XP, never add rounded component rates.
  return { ...totals, xp, rate: xp != null && totals.runtimeSeconds > 0 ? xp * 3600 / totals.runtimeSeconds : null };
}
let selected = null, today = '', period = '7', appliedRange = null, data = null, version = 0, inFlight = null, requestSequence = 0, expanded = false, signature = '';
const dateOffset = (date, offset) => { const value = new Date(`${date}T12:00:00Z`); value.setUTCDate(value.getUTCDate() + offset); return value.toISOString().slice(0, 10); };

function queryRange() {
  return period === 'custom' && appliedRange ? appliedRange : { from: dateOffset(today, 1 - Number(period === 'custom' ? 7 : period)), to: today };
}
export function showStatistics(id, backendNow) {
  const changed = selected !== id;
  today = backendNow.slice(0, 10); // Calendar dates belong to the backend, not the browser timezone.
  if (changed) {
    selected = id; version++; inFlight = null; data = null; displayData = null; signature = ''; expanded = false;
    if (id) {
      $('statistics-account').textContent = id;
      $('statistics-zone').textContent = '';
      $('statistics-metrics').innerHTML = ''; $('efficiency-chart').innerHTML = '';
      $('statistics-rows').innerHTML = ''; $('statistics-period').textContent = '';
      $('statistics-expand').hidden = true; $('statistics-legacy').hidden = true;
      $('chart-readout').textContent = '正在读取每日记录…';
    }
  }
  $('statistics-from').max = today; $('statistics-to').max = today;
  if (selected) refreshStatistics();
}
export async function refreshStatistics() {
  if (!selected) return;
  const { from, to } = queryRange(), id = selected, token = version;
  const key = JSON.stringify([id, from, to, token]);
  if (inFlight === key) return;
  const sequence = ++requestSequence;
  inFlight = key;
  if (!data) $('statistics-status').textContent = '正在读取每日统计…';
  $('statistics-retry').hidden = true;
  try {
    const result = await request(`${accountPath(id)}/statistics?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`);
    if (token !== version || id !== selected || sequence !== requestSequence) return;
    data = result;
    $('statistics-status').textContent = '';
    const nextSignature = JSON.stringify(result);
    if (signature !== nextSignature) { renderStatistics(); signature = nextSignature; }
  } catch (error) {
    if (token !== version || id !== selected || sequence !== requestSequence) return;
    $('statistics-status').textContent = `${error.message}${data ? ' 当前显示上次读取的统计。' : ''}`;
    $('statistics-retry').hidden = false;
  } finally { if (inFlight === key) inFlight = null; }
}
function renderStatistics() {
  displayData = { ...data,
    todayStats: selectedTotals(data.todayStats), weekStats: selectedTotals(data.weekStats),
    historicalStats: selectedTotals(data.historicalStats), periodStats: selectedTotals(data.periodStats),
    days: data.days.map(day => ({ ...day, totals: selectedTotals(day.totals) })) };
  $('statistics-zone').textContent = `后端日期 · ${data.today}`;
  $('statistics-zone').title = `按后端时区 ${data.timeZone} 划分日期`;
  $('statistics-metrics').innerHTML = [
    ['今日效率', displayData.todayStats, true], ['近7天效率', displayData.weekStats, false], ['历史累计效率', displayData.historicalStats, false]
  ].map(([label, totals, current]) => `<div><span>${label}${current ? ' <em>统计中</em>' : ''}</span><strong>${rate(totals.rate)} <small>XP/小时</small></strong><p>${xpLabel(totals)} · ${duration(totals.runtimeSeconds)}</p></div>`).join('');
  $('statistics-period').textContent = `${data.from} — ${data.to}`;
  $('statistics-legacy').hidden = !data.hasLegacy;
  drawChart(); renderRows();
}
function dayDescription(day) {
  return `${day.date}${day.isToday ? ' 今天 · 统计中' : ''}（${componentLabel()}）：效率 ${rate(day.totals.rate)} XP/小时，经验 ${xpLabel(day.totals)}，运行 ${duration(day.totals.runtimeSeconds)}`;
}
function drawChart() {
  const data = displayData;
  const focusedDate = document.activeElement?.dataset.date;
  if (!selectedComponents.length) {
    $('efficiency-chart').innerHTML = '<div class="empty"><h3>请选择至少一项经验成分</h3><p>勾选对局、任务或其他，查看对应效率。</p></div>';
    $('chart-readout').textContent = '尚未选择经验成分。'; return;
  }
  const days = data.days, valid = days.filter(day => day.totals.rate != null);
  if (!valid.length) {
    $('efficiency-chart').innerHTML = '<div class="empty"><h3>这段时间暂无效率数据</h3><p>有有效经验采样和运行时长后，将在这里显示每日曲线。</p></div>';
    $('chart-readout').textContent = '无运行时长的日期不计算效率。'; return;
  }
  const width = 1200, left = 72, right = 1160, top = 34, bottom = 260;
  const max = Math.max(...valid.map(day => day.totals.rate), data.periodStats.rate || 0, 1);
  const magnitude = 10 ** Math.floor(Math.log10(max / 4));
  const step = Math.ceil(max / 4 / magnitude) * magnitude, ceiling = step * 4;
  const x = i => days.length === 1 ? (left + right) / 2 : left + (right - left) * i / (days.length - 1);
  const y = value => bottom - (bottom - top) * value / ceiling;
  let path = '', connected = false;
  days.forEach((day, i) => {
    if (day.totals.rate == null) { connected = false; return; }
    path += `${connected ? 'L' : 'M'}${x(i).toFixed(2)},${y(day.totals.rate).toFixed(2)} `; connected = true;
  });
  const labelEvery = Math.max(1, Math.ceil(days.length / 8));
  $('efficiency-chart').innerHTML = `<svg viewBox="0 0 ${width} 310" role="img" aria-label="${escape(data.from)} 至 ${escape(data.to)} ${componentLabel()} 每日效率，缺失日期断开。下方提供每日明细。">
    <text x="6" y="16" class="chart-label">XP/小时</text>
    ${Array.from({ length: 5 }, (_, i) => `<line class="chart-grid" x1="${left}" x2="${right}" y1="${y(step * i)}" y2="${y(step * i)}"/><text x="${left - 12}" y="${y(step * i) + 4}" text-anchor="end" class="chart-label">${number(step * i)}</text>`).join('')}
    ${data.periodStats.rate == null ? '' : `<line class="chart-average" x1="${left}" x2="${right}" y1="${y(data.periodStats.rate)}" y2="${y(data.periodStats.rate)}"/>`}
    <path class="chart-path" d="${path}"/>
    ${days.map((day, i) => `${day.totals.rate == null ? '' : `<circle class="chart-point${day.isToday ? ' today' : ''}" tabindex="0" role="graphics-symbol" aria-label="${escape(dayDescription(day))}" data-date="${day.date}" cx="${x(i)}" cy="${y(day.totals.rate)}" r="5"><title>${escape(dayDescription(day))}</title></circle>`}${i % labelEvery === 0 || i === days.length - 1 ? `<text class="chart-label" x="${x(i)}" y="290" text-anchor="middle">${day.date.slice(5).replace('-', '.')}${day.isToday ? ' 今天' : ''}</text>` : ''}`).join('')}
  </svg>`;
  $('chart-readout').textContent = '悬停或聚焦数据点，查看当日经验和运行时长。';
  if (focusedDate) [...$('efficiency-chart').querySelectorAll('[data-date]')].find(el => el.dataset.date === focusedDate)?.focus({ preventScroll: true });
}
function renderRows() {
  const data = displayData;
  if (!data) return;
  const days = [...data.days].reverse(), shown = expanded ? days : days.slice(0, 7);
  $('statistics-rows').innerHTML = shown.map(day => `<tr><th scope="row">${day.date}${day.isToday ? ' 今天' : ''}</th><td class="numeric">${day.recorded ? xpLabel(day.totals) : '—'}</td><td class="numeric">${day.recorded ? duration(day.totals.runtimeSeconds) : '—'}</td><td class="numeric data">${rate(day.totals.rate)}</td><td class="numeric">${day.isToday ? '统计中' : !day.recorded ? '未记录' : day.totals.runtimeSeconds === 0 ? '无运行时长' : '已结算'}</td></tr>`).join('');
  $('statistics-expand').hidden = days.length <= 7;
  $('statistics-expand').textContent = expanded ? '收起明细' : `展开全部 ${days.length} 天`;
  $('statistics-expand').setAttribute('aria-expanded', String(expanded));
}
function changePeriod(value) {
  period = value; version++; inFlight = null; signature = ''; expanded = false;
  document.querySelectorAll('[data-period]').forEach(button => {
    button.classList.toggle('primary', button.dataset.period === period);
    button.setAttribute('aria-pressed', String(button.dataset.period === period));
  });
  refreshStatistics();
}
document.querySelectorAll('[data-period]').forEach(button => button.onclick = () => {
  const value = button.dataset.period; $('statistics-range').hidden = value !== 'custom'; showError('statistics-range-error', '');
  if (value === 'custom') {
    const range = queryRange(); $('statistics-from').value = range.from; $('statistics-to').value = range.to;
    $('statistics-from').focus(); return;
  }
  changePeriod(value);
});
$('statistics-range').onsubmit = event => {
  event.preventDefault(); const from = $('statistics-from').value, to = $('statistics-to').value;
  if (!from || !to || from > to || to > today || (Date.parse(to) - Date.parse(from)) / 86400000 >= 366) {
    showError('statistics-range-error', '请选择不晚于今天、起止顺序正确且不超过 366 天的日期范围。'); return;
  }
  showError('statistics-range-error', ''); appliedRange = { from, to }; changePeriod('custom');
};
$('statistics-expand').onclick = () => { expanded = !expanded; renderRows(); };
$('statistics-retry').onclick = refreshStatistics;
$('statistics-sources').addEventListener('change', () => {
  selectedComponents = [...$('statistics-sources').querySelectorAll('input:checked')].map(input => input.value);
  $('statistics-source-summary').textContent = selectedComponents.length ? `当前统计：${componentLabel()}` : '请选择至少一项';
  if (data) renderStatistics();
});
for (const eventName of ['pointerover', 'focusin', 'click']) $('efficiency-chart').addEventListener(eventName, event => {
  const date = event.target.closest('[data-date]')?.dataset.date;
  const day = displayData?.days.find(day => day.date === date);
  if (day) $('chart-readout').textContent = dayDescription(day);
});
