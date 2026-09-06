# Web UI 迁移

HSCentric 以 Windows C# 后端提供本机 HTTP 服务，浏览器负责全部界面。原有 .NET Framework 4.8 业务、userinfo XML 配置及游戏插件接口继续使用。

## 构建与运行

安装 Visual Studio / Build Tools 的 .NET 桌面开发组件及 .NET Framework 4.8 targeting pack。仓库现有 `packages/` 提供原依赖。

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build.ps1
.\artifacts\release\HSCentric.exe
```

启动后默认打开 `http://127.0.0.1:17321/`，仅接受该地址的请求。沿用原项目的管理员运行清单，以便 HTTP 监听和原有游戏进程操作。

可用参数：

```powershell
# 不自动打开浏览器
.\artifacts\release\HSCentric.exe --no-browser

# 仅验证配置和界面；不恢复、启动或调度游戏进程
.\artifacts\release\HSCentric.exe --safe-mode --port=17322

# 通配监听，允许通过服务器的局域网 IP 或主机名访问
.\artifacts\release\HSCentric.exe --host=* --port=17321

# 也可明确指定服务器地址
.\artifacts\release\HSCentric.exe --host=192.168.1.10 --port=17321
```

`--host` 支持 `*`、IP 地址和主机名；`0.0.0.0` 作为 `*` 的别名。不传时使用 `127.0.0.1`。通配模式下，其他电脑打开 `http://服务器IP:17321/`；程序自动打开的本机浏览器仍使用 `127.0.0.1`，不会打开 `http://*:17321/`。`*` 使用 [HttpListener 的通配前缀](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-net-httplistener)，接收该端口未被更具体前缀处理的请求。

如果其他电脑无法连接，请检查服务器 Windows 防火墙是否允许所选 TCP 端口；程序不会自动修改防火墙。当前没有登录认证，能访问此端口的人可以操作中控，因此应将放行范围限制在可信网络。写请求仍校验当前页面来源和会话令牌。

关闭浏览器不停止后端。使用页面「运行设置 → 停止后端服务」或后端控制台 Ctrl+C 停止并保存配置；恢复需重新启动程序。原来关闭窗口隐藏到托盘的行为由浏览器与后台服务的独立生命周期替代。

## 开机启动

在网页“运行设置”中勾选“开机启动”，立即添加登录自启动；取消勾选立即移除。下次后端电脑上当前运行中控的 Windows 用户登录后，会启动当前目录的程序，参数为 `--host=* --port=17321 --no-browser`。

该选项作用于当前连接的后端电脑。它只设置登录后启动，不设置 Windows 自动登录，也不在未登录时启动。取消选项不会停止当前已运行的中控。移动运行目录后，请从新目录启动程序并重新勾选。安全模式仅显示状态，不修改此选项。

实现使用 Windows 登录计划任务，以满足程序的管理员权限要求；界面读取任务的实际状态，保存失败时保留原选项并显示错误。

后端未以管理员身份运行时，开机启动开关禁用，并提示在后端电脑上退出中控、右键 `HSCentric.exe` 选择“以管理员身份运行”。检测依据是后端进程实际获得的权限；直接调用保存接口也会被拒绝。

## 使用旧配置

1. 先退出旧版中控，备份其 `HSCentric.exe.Config`。
2. 将新构建目录完整复制到一个新的运行目录，其中包括 `WebUI/`、DLL 与 `Globle_LevelExpNeededList.json`。
3. 用备份的旧配置替换新目录中的 `HSCentric.exe.config`。不要用空白构建配置覆盖旧运行目录。
4. 从新目录启动，核对账号、HSMod 端口、时段和路径。

首次可使用安全模式检查。普通启动会沿用旧程序的进程恢复和自动调度行为。所有路径都指向后端所在电脑；浏览器表单填写完整路径，替代原本的 Windows 文件选择框。未配置战网路径时可在「运行设置」补齐。

## 功能位置

| 原功能 | Web UI |
| --- | --- |
| 账号列表及经验、模式、等级 | 左侧账号状态表 |
| 新增、修改账号 | 添加账号 / 编辑配置 |
| 时段、队伍、策略、地图、领取选项 | 账号编辑中的每日编排 |
| 达标后换模式 | 账号编辑中的替代模式 |
| 启用、停用、启动一次、暂停八小时 | 选中账号下方操作 |
| 配置部署、关闭插件、备份、重置效率、删除 | 插件与维护 |
| 日志查看 | 运行记录、级别筛选、暂停滚动、导出 |
| 战网路径及退出 | 运行设置 |

备份现在会实际复制选中账号游戏目录的 `BepInEx/config/*.cfg` 到运行目录 `backups/` 下的独立时间戳目录，并显示保存位置。原按钮对应的脚本调用被注释，并未真正执行备份。

停用和暂停沿用旧逻辑：停止该账号后续自动调度，不保证立即结束已运行游戏。暂停计时与原版本一样在进程内生效，重启不恢复剩余暂停时间。删除仅移除中控配置；不会关闭已启动游戏。自动达标条件继续由 `TaskManager` 的现有逻辑决定。

## 后端边界

- `Program.cs`：启动、浏览器入口和退出。
- `BackendRuntime.cs`：独立检查循环、设置与有界日志缓冲。
- `WebUiServer.cs`：同源静态文件与 JSON API；写请求要求会话令牌，拒绝跨站 Origin。
- `WebModels.cs`：显式传输模型和字段验证；Token 不随账号读取返回，编辑留空保留旧值。
- `HSUnitManager` 的同一把锁保护调度和 API 对账号的操作，按稳定 ID 定位。运行检查较慢时请求可能等待；前端会显示超时并保留编辑草稿。
- 配置保存立即写入原 XML 文件。账号编辑使用配置指纹检测并发修改，避免静默覆盖。
- 静态文件采用白名单，配置、日志文件和源码不会作为静态资源公开。日志 API 隐藏当前账号 Token。
- 后端每次启动生成会话 ID，前端据此刷新写入令牌与日志游标；账号和时段编辑草稿保留。标题使用随程序分发的 Noto Sans SC 字体子集，许可证位于 `WebUI/fonts/OFL.txt`。
- 原可选 `rest_url` 接口保留原有用途，仅普通运行模式启动；本次并未扩展其权限模型。

## 验证

`scripts/test-login-startup.ps1` 验证首次读取不存在的启动任务，以及权限、服务等真实错误不会被当作“未启用”。在管理员 PowerShell 中加 `-Integration` 可验证 Windows 计划任务的启用、读取、重复启用和取消；测试使用随机名称的临时任务并在结束时清理，不执行任务，也不修改正式自启动设置。

`scripts/test-webui.cjs` 使用 Node.js 和 Playwright，连接默认 `127.0.0.1:17322` 的独立安全模式后端。它要求初始账号为空，创建明确标记的测试账号，测试接口、真实表单保存、配置冲突、Token 保留、跨日时段、日志导出与响应式布局。测试输出在 `.impeccable/review/`。

```powershell
# 使用独立测试目录；不对旧运行目录执行此测试
$env:NODE_PATH = '<安装了 playwright 的 node_modules 目录>'
$env:HSCENTRIC_TEST_RUNTIME_DIR = '<已启动安全模式后端的独立运行目录>'
node .\scripts\test-webui.cjs
```

`HSCENTRIC_TEST_RUNTIME_DIR` 可指定用于核对 XML 持久化的测试运行目录。`scripts/test-recovery.cjs` 是针对性回归，使用 `artifacts/verify/` 与上述测试账号，验证真实后端重启后的日志、令牌和草稿恢复，以及刷新时的键盘焦点。它会停止并重新启动该测试后端。

真实游戏启动、插件切换、战网更新和经验采集仍需在配置了游戏与插件的 Windows 环境中验证。安全模式测试不宣称这些集成已通过实际运行验证。
