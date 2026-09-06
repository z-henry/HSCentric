import { request, accountPath } from './api.js';
import { $, escape, range, showError, confirmAction, toast } from './view.js';
const form = $('account-form');
let draft, originalId, activeTask, special, dirty = false, onSaved, saving = false;
let metadataSignature = '';
const blankTask = () => ({ mode: '一条龙', start: '08:00:00', stop: '12:00:00', teamName: '初始队伍', strategyName: 'PVE策略', map: '2-5', numCore: 0, numTotal: 6, scale: false, refreshQuest: false, claimAchievement: false, claimReward: false });
const values = { mode: 'mode', start: 'start', stop: 'stop', team: 'teamName', strategy: 'strategyName', map: 'map', core: 'numCore', total: 'numTotal', scale: 'scale', quest: 'refreshQuest', achievement: 'claimAchievement', reward: 'claimReward' };

export function setupEditor(meta, saved) {
  onSaved = saved;
  const signature = JSON.stringify([meta.modes, meta.strategies]);
  if (signature === metadataSignature) return;
  const mode = $('task-mode').value;
  $('task-mode').innerHTML = meta.modes.map(m => `<option>${escape(m)}</option>`).join('');
  $('strategies').innerHTML = meta.strategies.map(s => `<option value="${escape(s)}"></option>`).join('');
  if (meta.modes.includes(mode)) $('task-mode').value = mode;
  metadataSignature = signature;
}
export async function openEditor(id = null, taskIndex = null) {
  originalId = id;
  draft = id ? await request(accountPath(id)) : { id: '', token: '', enable: false, hsPath: '', hbPath: '', hsModPort: 58744, switchTask: false, tasks: [], specialTask: null };
  dirty = false; saving = false; form.reset();
  for (const name of ['id', 'token', 'enable', 'hsPath', 'hbPath', 'hsModPort', 'switchTask']) {
    const field = form.elements.namedItem(name);
    if (field.type === 'checkbox') field.checked = draft[name]; else field.value = draft[name] ?? '';
  }
  form.elements.namedItem('id').readOnly = Boolean(id);
  form.elements.namedItem('token').required = !id;
  $('token-hint').textContent = id ? '留空保留当前 Token；输入新值才会替换。' : '用于启动炉石，请填写当前 Token。';
  $('editor-title').textContent = id ? `编辑 ${id}` : '添加账号';
  $('editor-save').disabled = false; $('editor-save').textContent = '保存账号';
  showError('editor-error', ''); $('task-editor').hidden = true; renderDraft();
  $('account-dialog').showModal();
  if (taskIndex !== null) editTask(taskIndex); else form.elements.namedItem(id ? 'hsPath' : 'id').focus();
}
function markDirty() { dirty = true; $('draft-status').textContent = '有未保存的更改'; }
function renderDraft() {
  $('draft-tasks').innerHTML = draft.tasks.length ? `<ol class="draft-list">${draft.tasks.map((t, i) => `<li><button type="button" data-edit-task="${i}"><span class="data">${escape(range(t))}</span><strong>${escape(t.mode)}</strong><small>${escape(t.teamName)}</small></button><button type="button" data-remove-task="${i}" class="quiet danger" aria-label="移除 ${escape(range(t))} ${escape(t.mode)}">移除</button></li>`).join('')}</ol>` : '<p class="draft-empty">尚无时段。至少添加一个时段后才能保存账号。</p>';
  $('special-summary').textContent = draft.specialTask ? `${draft.specialTask.mode} · ${draft.specialTask.teamName}` : '未设置替代模式';
  $('special-clear').disabled = !draft.specialTask;
  $('draft-status').textContent = dirty ? '有未保存的更改' : '尚未保存';
}
function editTask(index = null, isSpecial = false) {
  activeTask = index; special = isSpecial;
  const task = isSpecial ? draft.specialTask || blankTask() : index === null ? blankTask() : draft.tasks[index];
  for (const [suffix, key] of Object.entries(values)) {
    const field = $(`task-${suffix}`);
    if (field.type === 'checkbox') field.checked = task[key]; else field.value = task[key] ?? '';
  }
  $('task-title').textContent = isSpecial ? '替代模式' : index === null ? '添加时段' : '编辑时段';
  $('task-apply').textContent = isSpecial ? '保存替代模式到草稿' : '保存时段到草稿';
  $('task-start-label').hidden = isSpecial; $('task-stop-label').hidden = isSpecial;
  showError('task-error', ''); $('task-editor').hidden = false; updateTaskFields();
  $('task-editor').scrollIntoView({ block: 'nearest' }); $('task-mode').focus();
}
function updateTaskFields() {
  const mode = $('task-mode').value, buddy = ['狂野', '标准', '经典', '休闲', '幻变'].includes(mode);
  document.querySelectorAll('[data-task-field="team"]').forEach(e => e.hidden = mode === '酒馆');
  document.querySelectorAll('[data-task-field="merc"]').forEach(e => e.hidden = buddy || mode === '酒馆');
}
function applyTask() {
  const task = {};
  for (const [suffix, key] of Object.entries(values)) {
    const field = $(`task-${suffix}`);
    task[key] = field.type === 'checkbox' ? field.checked : field.type === 'number' ? Number(field.value) : field.value.trim();
  }
  if (!task.start || !task.stop || !special && task.start === task.stop) return showError('task-error', '请输入不同的开始与停止时间。');
  if (task.mode !== '酒馆' && (!task.teamName || !task.strategyName)) return showError('task-error', '请填写队伍和策略名称。');
  if (task.numCore < 0 || task.numTotal < 1 || task.numTotal > 6 || task.numCore > task.numTotal || !Number.isInteger(task.numCore) || !Number.isInteger(task.numTotal)) return showError('task-error', '总数须为 1–6 的整数，核心人数不能超过总数。');
  if (special) draft.specialTask = task;
  else if (activeTask === null) draft.tasks.push(task);
  else draft.tasks[activeTask] = task;
  markDirty(); renderDraft(); $('task-editor').hidden = true; (special ? $('special-edit') : $('task-add')).focus();
}
async function closeEditor() {
  if (saving) return;
  if ((dirty || !$('task-editor').hidden) && !await confirmAction('放弃未保存的更改？', '账号配置和时段草稿尚未保存，关闭后将丢弃这些修改。', '放弃更改')) return;
  $('account-dialog').close();
}
form.addEventListener('input', markDirty);
form.addEventListener('submit', async event => {
  event.preventDefault();
  if (!$('task-editor').hidden) { showError('editor-error', '请先将正在编辑的时段保存到草稿，或取消时段编辑。'); $('editor-error').focus(); return; }
  if (saving) return;
  for (const key of ['id', 'token', 'enable', 'hsPath', 'hbPath', 'hsModPort', 'switchTask']) {
    const field = form.elements.namedItem(key);
    draft[key] = field.type === 'checkbox' ? field.checked : field.type === 'number' ? Number(field.value) : field.value.trim();
  }
  saving = true; $('editor-save').disabled = true; $('editor-save').textContent = '正在保存…'; showError('editor-error', '');
  try {
    const result = await request(originalId ? accountPath(originalId) : '/accounts', { method: originalId ? 'PUT' : 'POST', body: draft });
    dirty = false; $('account-dialog').close(); toast('账号与每日编排已保存。'); await onSaved(result.id);
  } catch (error) { showError('editor-error', error.message); $('editor-error').focus(); }
  finally { saving = false; $('editor-save').disabled = false; $('editor-save').textContent = '保存账号'; }
});
$('draft-tasks').addEventListener('click', event => {
  const edit = event.target.closest('[data-edit-task]'); const remove = event.target.closest('[data-remove-task]');
  if (edit) editTask(Number(edit.dataset.editTask));
  if (remove) { draft.tasks.splice(Number(remove.dataset.removeTask), 1); $('task-editor').hidden = true; markDirty(); renderDraft(); $('task-add').focus(); }
});
$('task-add').onclick = () => editTask();
$('special-edit').onclick = () => editTask(null, true);
$('special-clear').onclick = () => { draft.specialTask = null; markDirty(); renderDraft(); };
$('task-mode').onchange = updateTaskFields;
$('task-apply').onclick = applyTask;
$('task-cancel').onclick = () => { $('task-editor').hidden = true; $('task-add').focus(); };
$('editor-close').onclick = closeEditor; $('editor-cancel').onclick = closeEditor;
$('account-dialog').addEventListener('cancel', event => { event.preventDefault(); closeEditor(); });
window.addEventListener('beforeunload', event => { if ($('account-dialog').open && dirty) { event.preventDefault(); event.returnValue = ''; } });
