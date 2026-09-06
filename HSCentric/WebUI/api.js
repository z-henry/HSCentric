let token = '';
export async function request(path, { method = 'GET', body, ...rest } = {}) {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), 30000);
  try {
    const response = await fetch(`/api${path}`, { method, cache: 'no-store', signal: controller.signal,
      headers: { ...(method === 'GET' ? {} : { 'Content-Type': 'application/json', 'X-HSCentric-Token': token }) },
      ...(body === undefined ? {} : { body: JSON.stringify(body) }), ...rest });
    const data = await response.json();
    if (!response.ok) throw new Error(data.error || `请求失败 (${response.status})`);
    return data;
  } catch (error) {
    if (error.name === 'AbortError') throw new Error('后端响应超时，可能正在启动游戏；请稍后重试。');
    if (error instanceof TypeError) throw new Error('无法连接后端。请确认 HSCentric 正在运行且网络可达，再重新连接。');
    throw error;
  } finally { clearTimeout(timer); }
}
export async function connect() { const meta = await request('/meta'); token = meta.csrfToken; return meta; }
export function accountPath(id) { return `/accounts/${encodeURIComponent(id)}`; }
