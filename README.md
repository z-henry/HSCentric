# HSCentric

### 说明

1. 需要配合[HsMod(author:Pik-4)](https://github.com/Pik-4/HsMod)使用，HsMod配置如下

|  key   | value  |
|  ----  | ----  |
| 自动开盒  | true |
| 结算展示  | false |
| 应用焦点  | false |
| 报错退出  | true |
| 弹出消息  | false |
| 自动领奖  | true |
| HsMod状态  | true |
| 对手卡牌特效  | false |
| 金卡特效  | Disabled |
| 钻石卡特效  | Disabled |

2. 佣兵部分，需要配合Mercenary使用，工作在[推荐方式]下
3. 传统对战部分，需要配合[HearthBuddy(author:琴弦上的宇宙)](https://gitee.com/UniverseString/Hearthstone-myHearthbuddy)使用

### 切换原理
切换`io.github.jimowushuang.hs.cfg`配置，启动炉石、HearthBuddy

1. `传统模式`切换到`佣兵模式`
配置切换

|  key   | value  |
|  ----  | ----  |
| 插件开关  | `true` |
| 插件运行模式  | `挂机收菜、刷图等` |
| 使用的队伍名称  | `你的佣兵队伍` |
| 战斗策略  | `你的佣兵策略` |

启动炉石

2. `佣兵模式`切换到`佣兵模式`
配置切换

|  key   | value  |
|  ----  | ----  |
| 插件开关  | `true` |
| 插件运行模式  | `挂机收菜、刷图等` |
| 使用的队伍名称  | `你的佣兵队伍` |
| 战斗策略  | `你的佣兵策略` |

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
2. 多个炉石同时启动不delay，插件可能无法加载（目前delay 5秒）
3. 兄弟不识别佣兵模式，所以在挂传统模式前，挂pvp，并且间隔10分钟左右（pve不操作就不会结算，而pvp超时对手就把你解决了）
4. 日志无法定时清理
