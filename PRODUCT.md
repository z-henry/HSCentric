# HSCentric

<!-- impeccable:product-schema 1 -->

## Platform

web

界面迁移到浏览器；C# 后端仍运行于 Windows / .NET Framework 4.8，负责本机进程、插件配置、调度及日志。

## Users

根据现有中文界面与多账号功能推定：熟悉炉石、HsMod 及相关插件的中文使用者。具体用户规模与使用频率未确认。

## Product Purpose

集中配置多个炉石账号的运行模式和每日时段，查看运行状态、经验与日志，减少重复启动和切换操作。

## Operating Context

现有主窗口、账号配置窗口、模式配置窗口分别承担总览、账号与插件连接设置、时段及策略编辑。浏览器关闭后后端继续运行。

## Capabilities and Constraints

- 用户明确要求 Web UI 替换 WinForms，C# 仅作为后端。
- 迁移保留现有 userinfo XML 配置、模式名称及 HsMod、Mercenary、HearthBuddy 集成。
- 支持账号增删改、启用停用、暂停八小时、启动一次、插件配置部署/关闭、经验效率重置。
- 支持每日时段（包含跨日）、队伍、策略、佣兵地图、核心/总数、齿轮、刷新任务和领取选项，以及达标后的替代模式。
- 默认通过本机地址访问；可用 `--host=*` 开启局域网访问，所操作的进程和路径仍位于后端电脑。多用户权限体系与跨平台后端未纳入本次迁移。
- 运行设置提供单一“开机启动”选项，控制后端电脑的当前 Windows 用户登录后是否自动启动中控。
- 现有游戏及插件的实时兼容性须在真实环境中验证，不能从代码存在推定可用。

## Brand Commitments

保留 HSCentric 与“中控”名称、中文操作术语。用户明确排除常见 SaaS dashboard 模板。

## Evidence on Hand

- Git 历史中的 `HSCentric/MainForm.cs`、`HSUnitForm.cs`、`TaskForm.cs`：迁移前功能与字段；这些窗口已由 Web UI 替换。
- `HSCentric/WebUI/` 与 `HSCentric/WebModels.cs`：当前界面与传输字段。
- `HSCentric/HSUnitManager.cs`、`TaskManager.cs`：调度和配置行为。
- `HSCentric/Const.cs`：模式与策略名称。
- `README.md`：使用方式和历史限制；其中问题清单需以当前代码及验证结果为准。
- 未提供真实用户案例、产品性能指标或设计参考文档。

## Product Principles

- 账号、执行时段与当前状态必须能互相定位。
- 真实后端状态驱动界面，连接失败不能伪装成成功或零账号。
- 配置编辑在保存前保留草稿，失败后保留输入。
- 保持配置与插件兼容；界面迁移不改写业务模式含义。
- 本机调度不依赖浏览器保持打开。
