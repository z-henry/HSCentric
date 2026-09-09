---
name: HSCentric
description: 以账号、每日时段和运行记录组织操作的中文运行编排台。
colors:
  paper: "#f5f4ef"
  surface: "#fffefa"
  wash: "#eceee8"
  ink: "#23352f"
  muted: "#59655f"
  line: "#cbd1c8"
  accent: "#24634f"
  accent-hover: "#194b3c"
  selected: "#e2ece3"
  amber: "#9a530f"
  danger: "#a5332a"
  danger-wash: "#fae9e4"
  focus: "#b56816"
typography:
  display:
    fontFamily: '"HSCentric Display", "Noto Sans SC", sans-serif'
    fontSize: "2rem"
    fontWeight: 650
    lineHeight: 1.4
    letterSpacing: "-.025em"
  headline:
    fontFamily: '"HSCentric Display", "Noto Sans SC", sans-serif'
    fontSize: "1.2rem"
    fontWeight: 650
    lineHeight: 1.5
  title:
    fontFamily: '"HSCentric Display", "Noto Sans SC", sans-serif'
    fontSize: "1rem"
    lineHeight: 1.6
  body:
    fontFamily: '"Microsoft YaHei UI", "PingFang SC", "Noto Sans CJK SC", sans-serif'
    fontSize: "14px"
  label:
    fontFamily: '"Microsoft YaHei UI", "PingFang SC", "Noto Sans CJK SC", sans-serif'
    fontSize: ".85rem"
    lineHeight: 1.6
  data:
    fontFamily: '"Cascadia Mono", Consolas, monospace'
rounded:
  radius: ".25rem"
  dialog: "6px"
spacing:
  space-1: ".25rem"
  space-2: ".5rem"
  space-3: ".75rem"
  space-4: "1rem"
  space-5: "1.5rem"
  space-6: "2rem"
  space-7: "3rem"
components:
  button-default:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.radius}"
    padding: ".5rem 1rem"
  button-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.surface}"
    rounded: "{rounded.radius}"
    padding: ".5rem 1rem"
  button-primary-hover:
    backgroundColor: "{colors.accent-hover}"
  button-quiet:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    rounded: "{rounded.radius}"
    padding: ".5rem 1rem"
  button-danger:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.danger}"
    rounded: "{rounded.radius}"
    padding: ".5rem 1rem"
  field:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.radius}"
    padding: ".75rem"
  masthead:
    textColor: "{colors.ink}"
    padding: "0 3rem"
    height: "76px"
  state:
    textColor: "{colors.muted}"
  workspace:
    backgroundColor: "{colors.surface}"
  schedule-row:
    backgroundColor: "transparent"
    textColor: "{colors.ink}"
    padding: ".75rem 0"
    width: "100%"
  dialog:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.dialog}"
    width: "min(760px, calc(100vw - 32px))"
---

# Design System: HSCentric

## Overview

**Creative North Star: "中文运行编排台"**

暖白纸面承载紧凑的账号清单，深墨文字建立阅读顺序，青绿操作与琥珀时间标记帮助核对正在运行的编排。界面沉静、直接，保留 HSCentric、中控及现有中文操作术语。

身份、时间和状态通过对齐、细线与浅色选择面关联。信息密度服务于本机多账号操作；装饰不占据账号、时段和日志的阅读位置。

**Key Characteristics:**

- 平面分区与细线，保留连续的工作空间。
- 自托管中文标题字体，正文保持平台阅读习惯。
- 时间等宽、数值对齐，状态同时提供文字与形状。
- 编辑保留草稿，刷新保留选择与键盘位置。

本文件从 `HSCentric/WebUI/` 的最终源码提取；颜色与尺度以前置 tokens 为准。表面构图见 `.impeccable/surfaces/hscentric-webui-index-html.md`。审阅记录及五个视口截图位于 `.impeccable/review/`；最终 ship 结论覆盖已修复的字体、重连与焦点问题。sidecar 的色阶仅供面板预览，不增加运行界面色彩 tokens。

## Colors

主色属于低饱和青绿，暖白与灰绿构成中性色层次；时间、错误和焦点各自保留明确语义。

### Primary

- **青绿操作色（accent / accent-hover）**：主要保存、添加和启用动作；悬停加深。已连接及运行中状态沿用青绿。
- **浅绿选择面（selected）**：选中账号、一般通知及基础按钮按下反馈。

### Secondary

- **琥珀时间色（amber）**：等待状态与当前时间指针。
- **赭色焦点（focus）**：键盘焦点轮廓；不充当运行状态。
- **砖红错误色（danger / danger-wash）**：错误消息、错误日志与危险操作。错误通知使用浅红背景。

### Neutral

- **暖纸（paper）**：页面底色。
- **近白工作面（surface）**：工作区、输入框和弹窗。
- **灰绿衬面（wash）**：日志、只读字段与悬停反馈。
- **深墨（ink）**：主文字；短暂提示反转为深墨底与近白字。
- **灰绿辅文（muted）**：单位、说明、次要状态与表头。
- **灰绿细线（line）**：表格、分区、边框与分隔。

**The 状态可读 Rule.** 颜色必须与文字同行；停用状态另用空心点，选中账号同时保留按钮的 `aria-pressed` 状态。

## Typography

**Display Font:** HSCentric Display（Noto Sans SC 自托管子集），后备为 Noto Sans SC、sans-serif。

**Body Font:** Microsoft YaHei UI、PingFang SC、Noto Sans CJK SC、sans-serif。

**Label/Mono Font:** Cascadia Mono、Consolas、monospace，用于时间和测量数据。

标题采用紧凑的中文无衬线字形。正文、控件与说明保持既有平台字栈；表格通过字号和字重区分主值、次值与单位，不使用巨大指标数值建立层级。

### Hierarchy

- **Display**：页面标题使用 display 角色；手机端收至（1.7rem）。
- **Headline**：章节与弹窗标题使用 headline 角色；账号详情名称为（1.45rem）。
- **Title**：时段编辑与分组小标题使用 title 角色。
- **Body**：根字号保持 body 角色；段落行高为（1.75），表格为（.9rem）。
- **Label**：表单标签使用 label 角色；说明与状态多为（.8rem），表格次值为（.75rem）。
- **Data**：时间与数据使用等宽字栈和等宽数字；数值列右对齐，单位降为辅文。

**The 字体分工 Rule.** 品牌与标题使用自托管显示字体；正文与等宽数据维持各自字栈，避免全界面统一成展示字。

字体文件为 `fonts/noto-sans-sc-display.woff2`，固定字重（650），采用 `font-display: swap` 并关闭标题字体合成。子集包含 GB2312、ASCII、中文标点及当前界面文字，罕见账号字由后备字形承接；分发时保留 `fonts/OFL.txt`，来源与制作过程见 `fonts/README.md`。

## Layout

2026-09-09 更新：账号区初始为全宽五列表格（账号、状态、模式、时间段、经验效率），每列单行显示；表头圆形感叹号说明效率来源。点击整行或用 Enter / 空格选中后，表格向左压缩，右侧展开账号编排，下方展开所选账号的效率趋势和每日明细。再次选择同一行或点击「收起」回到默认状态。统计指标在一条平面分区中并排展示，沿用青绿折线与灰色加权均线；缺失日期断开。

展开采用约480–590ms的分步过渡，表格先收窄，详情与统计随后淡入；收起约400–470ms，支持中途反向。按用户明确要求默认开启动画，不提供勾选开关；该选择覆盖下文早期版本的减少动态效果约定。窄屏保持单列顺序展开、表格和曲线局部横向滚动。此次按用户要求不再生成预览或截图，以无界面浏览器的布局、交互和数据检查验证。

根字号决定 rem 尺度；间距直接采用已有 space-1 至 space-7。控件与行内元素以 space-2/3 分隔，分组以 space-4/5 留白，主要区块使用 space-6/7。正文容器居中，最大宽度为（1720px）。

运行工作区默认全宽表格，右栏折叠；选中后列宽为 `minmax(0, 1fr) minmax(0, .588235fr)`。宽度不超过（1100px）时按内容顺序堆叠，账号控制在表格下方展开，原竖分隔改为横分隔。账号列表的列结构在展开前后保持一致。

宽度不超过（600px）时，页标题、表单和通知改为单列，顶栏状态另起一行；表格在有名称、可聚焦的局部区域横向滚动，保留列间比较。账号操作换行并扩展宽度，日志保留时间、级别、消息三列；长消息与账号名可换行。

弹窗采用头部、可滚动正文和不收缩的操作栏。常规最大高度为 `calc(100dvh - 48px)`；手机为 `calc(100dvh - 16px)`，宽度也保留视口内侧间距。保存与取消位于正文滚动区外。

## Elevation & Depth

常驻界面通过近白工作面、灰绿衬面和细线区分深度，不给表格行或分区增加阴影。柔和阴影仅用于短暂提示与模态弹窗；弹窗背后的半透明深色遮罩建立操作层级。精确阴影值与遮罩 CSS 记录在 sidecar。

**The 平面工作区 Rule.** 常驻工作面保持平坦，只有脱离文档流的提示和弹窗使用投影。

## Shapes

按钮与输入框沿用 radius token 的轻微圆角；弹窗沿用独立 dialog 圆角。表格、时段行和工作区保持直边，以（1px）边框组织内容。状态点为小圆点；图标与时段条采用原生 SVG 线条和矩形。

## Components

### Buttons

按钮紧凑、明确。常规按钮使用近白底、细线边框与（600）字重；主按钮使用青绿底，quiet 按钮采用透明底和透明边框，danger 变体用砖红文字并在悬停时显示浅红衬面。基础最小高度为（40px），手机常规按钮提升至（44px）；控件的实际例外由组件源码保留。

悬停和按下只改变颜色；禁用时降低透明度（.5）并使用不可用指针。颜色过渡使用（160ms，cubic-bezier(.16, 1, .3, 1)）；减少动态效果时取消过渡。键盘焦点以（2px）赭色轮廓和（3px）偏移显示。

### Cards / Containers

工作区使用连续背景和分隔线。账号、详情、编排与日志用标题和间距形成层级；日志采用灰绿衬面与固定高度（212px）的滚动区域。选中行铺浅绿背景，悬停行铺灰绿背景，内容与列对齐保持稳定。

### Inputs / Fields

输入框和选择器使用近白底、（1px，#909f93）边框与 field token 的内边距，最小高度（42px）。只读字段切换灰绿衬面；复选框保持原生控件，青绿强调色。搜索图标在左侧，输入文字为图标留出空间。错误说明显示在对应编辑区域，保存失败保持字段输入并将焦点移到错误提示。

### Navigation

顶部身份栏由线性 SVG 标志、品牌链接、连接状态、时钟与运行设置组成。跳转链接仅在聚焦时出现，直达账号列表；插件维护使用原生折叠摘要。手机隐藏品牌副标，连接状态与时钟在下一行排列。

### Status

状态以小点和中文文字组成，不使用独立胶囊背景。运行中为实心青绿点，等待为琥珀点，停用为空心辅文色点。连接失败显示明确错误及重连动作；已有状态被标明为上次连接的数据。

### Daily Schedule

SVG 时段条对应每日（00:00–24:00），任务采用三个既有绿色阶轮换，当前时间以琥珀线与点标记。跨日任务在时间轴两端分段，并在文字范围中显示“次日”。每段任务都有对应的原生按钮行，显示时间、模式、队伍和策略；条形和按钮行均进入相同任务的编辑。刷新按账号或任务身份恢复精确按钮焦点，也保留维护摘要的焦点与展开状态。

### Editor & Feedback

账号编辑将连接信息、每日编排、替代模式放在同一弹窗。时段先加入草稿，再保存账号；未完成的时段编辑与失败输入保持可见。未保存时关闭会请求确认，保存期间禁用提交。后台会话变化刷新元数据并重置旧日志游标，保留正在编辑的账号和时段草稿。日志暂停只停止自动滚动，仍继续接收记录。

## Do's and Don'ts

### Do:

- **Do** 以账号、时段和状态的对齐组织内容，保留连续工作面。
- **Do** 延用自托管标题字、平台正文和等宽时间的分工。
- **Do** 同时提供状态文字、焦点轮廓和原生键盘控件。
- **Do** 在窄屏保留表格局部滚动与可达的弹窗操作栏。
- **Do** 在刷新、重连和保存失败后保留用户的选择、焦点与草稿。

### Don't:

- **Don't** 将账号编排替换为通用 SaaS 指标卡片模板。
- **Don't** 用颜色独自表达运行状态或错误。
- **Don't** 在普通工作分区添加投影或装饰纹理。
- **Don't** 将加载失败展示为成功连接、空账号或清空的编辑草稿。
- **Don't** 将截图里的测试账号、演示数值或 sidecar 预览色阶当作产品默认内容。
