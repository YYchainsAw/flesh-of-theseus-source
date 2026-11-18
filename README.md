# Flesh of Theseus · 忒修斯之人

[English](README.en.md) · [在 itch.io 在线试玩](https://yychainsaw.itch.io/flesh-of-theseus)

[![《忒修斯之人》封面](https://img.itch.zone/aW1nLzMyMDQ4MTY5LnBuZw==/508x254%23b/zp%2B%2BHa.png)](https://yychainsaw.itch.io/flesh-of-theseus)

《忒修斯之人》是 2026 学院路 Game Jam 的 2D 点击式解谜游戏。玩家在一座封闭空间中寻找散落的肢体，通过更换装备获得不同能力，解开谜题并走向不同结局。

## 项目职责

我（YYchainsAw）负责程序开发和项目管理。这个仓库展示游戏的核心状态、命令执行与规则接口，以及对应的 EditMode 测试。

## 代码阅读入口

| 入口 | 实现 |
| --- | --- |
| [GameSession.cs](src/CollisionJam.Core/GameSession.cs) | 统一处理拾取、装备、解谜和场景导航等命令。通过规则检查后提交状态，再向界面发送状态变更事件。 |
| [GameState.cs](src/CollisionJam.Core/GameState.cs) 和 [IGameStateView.cs](src/CollisionJam.Core/IGameStateView.cs) | 保存背包、装备槽、当前视图和解谜进度，通过只读接口供规则和界面查询。 |
| [规则接口](src/CollisionJam.Core/Contracts) | 将装备、拾取、导航与谜题的判定定义为独立接口，由具体玩法提供实现。 |
| [EditMode 测试](tests/CollisionJam.Core.EditModeTests) | 覆盖规则拒绝时的状态保留、成功后的单次通知、重复命令、回调重入和密码输入快照。 |

例如，玩家拖入一件肢体时，`TryEquip` 先检查物品与槽位，再调用 `CheckEquip`。规则允许后，`GameSession` 更新装备槽并触发 `StateChanged`。

## 游戏画面

[![《忒修斯之人》实机画面](https://img.itch.zone/aW1nLzMyMDk1NDQ3LnBuZw==/original/xiuDOY.png)](https://yychainsaw.itch.io/flesh-of-theseus)

## 开发环境

原游戏使用 Unity `2022.3.62f3c1`。[核心源码](src/CollisionJam.Core)为纯 C# 程序集，[测试程序集](tests/CollisionJam.Core.EditModeTests)使用 NUnit 和 Unity Test Framework。若要在 Unity 中运行测试，请将两个目录连同各自的 `.asmdef` 放入同一个已安装 Unity Test Framework 的项目的 `Assets/` 下，再在 Test Runner 中运行 EditMode 测试。

## 版权与使用范围

本仓库的源码仅公开用于作品展示和阅读，版权归各自权利人。你可以依照 GitHub 平台条款查看和 fork 本仓库。超出平台条款和适用法律所允许范围的复制、修改、再分发或商业使用，须事先取得相应权利人的授权。

游戏美术、音频和正式场景保留在原项目中，其权利归各自权利人。

## 团队

- YYchainsAw · 程序、项目管理
- FUSI14 · 策划
- Coldmushroom（菌寒）· 美术
