# HSCentric

### 说明

1. 需要配合HsMod(author:Pik-4)使用，HsMod配置如下

|  key   | value  |
|  ----  | ----  |
| 自动开盒  | true |
| 结算展示  | false |
| 应用焦点  | false |
| 报错退出  | true |
| 弹出消息  | false |
| 自动领奖  | true |
| HsMod状态  | true |

2. 佣兵部分，需要配合Mercenary使用，工作在[推荐方式]下
3. 传统对战部分，需要配合HearthBuddy(author:琴弦上的宇宙)使用

### 切换原理
切换`io.github.jimowushuang.hs.cfg`配置，启动炉石、HearthBuddy

1. `传统模式`切换到`佣兵模式`
配置切换

|  key   | value  |
|  ----  | ----  |
| 插件开关  | `true` |
| 插件运行模式  | `false` |
| 使用的队伍名称  | `false` |
| 战斗策略  | `true` |

启动炉石

2. `佣兵模式`切换到`佣兵模式`
配置切换

|  key   | value  |
|  ----  | ----  |
| 插件开关  | `true` |
| 插件运行模式  | `false` |
| 使用的队伍名称  | `false` |
| 战斗策略  | `true` |

启动炉石

3. `佣兵模式`切换到`传统模式`
配置切换

|  key   | value  |
|  ----  | ----  |
| 插件开关  | `false` |

启动炉石、HearthBuddy

4. `传统模式`切换到`传统模式`
目前暂不支持

### 已知问题
1. 无法进行传统对战之间的相邻切换（经典、休闲、标准、狂野）
2. 未能找到一款configurationUI支持自定义插件路径(F1/F5失效)
