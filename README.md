# Flesh of Theseus · 忒修斯之人

《忒修斯之人》是 2026 学院路 Game Jam 的 2D 点击式解谜游戏。玩家在一座封闭空间中寻找散落的肢体，通过更换装备获得不同能力，解开谜题并走向不同结局。

**[在 itch.io 在线试玩](https://yychainsaw.itch.io/flesh-of-theseus)**

## 项目职责

我（YYchainsAw）负责程序开发和项目管理。这个仓库展示游戏的核心状态、命令执行与规则接口，以及对应的 EditMode 测试。

## 代码阅读入口

| 入口 | 实现 |
| --- | --- |
| [GameSession.cs](Assets/Scripts/Core/GameSession.cs) | 统一处理拾取、装备、解谜和场景导航等命令。通过规则检查后提交状态，再向界面发送状态变更事件。 |
| [GameState.cs](Assets/Scripts/Core/GameState.cs) 和 [IGameStateView.cs](Assets/Scripts/Core/IGameStateView.cs) | 保存背包、装备槽、当前视图和解谜进度，通过只读接口供规则和界面查询。 |
| [Contracts](Assets/Scripts/Core/Contracts) | 将装备、拾取、导航与谜题的判定定义为独立接口，由具体玩法提供实现。 |
| [EditMode 测试](Assets/Tests/EditMode) | 覆盖规则拒绝时的状态保留、成功后的单次通知、重复命令、回调重入和密码输入快照。 |

例如，玩家拖入一件肢体时，`TryEquip` 先检查物品与槽位，再调用 `CheckEquip`。规则允许后，`GameSession` 更新装备槽并触发 `StateChanged`。

## 开发环境

原游戏使用 Unity `2022.3.62f3c1`。`CollisionJam.Core` 为纯 C# 程序集，EditMode 测试使用 NUnit 和 Unity Test Framework。此仓库提供源码快照，完整游戏通过 itch.io 试玩。

## 版权与使用范围

本仓库的源码仅公开用于作品展示和阅读，版权归各自权利人。你可以依照 GitHub 平台条款查看和 fork 本仓库。超出平台条款和适用法律所允许范围的复制、修改、再分发或商业使用，须事先取得相应权利人的授权。

游戏美术、音频和正式场景保留在原项目中，其权利归各自权利人。

## 团队

- YYchainsAw · 程序、项目管理
- FUSI14 · 策划
- HLZY20 · 程序
- Coldmushroom（菌寒）· 美术
