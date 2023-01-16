# Town Of Host Edited

## 关于这个模组

该模组不隶属于我们之中或 Innersloth LLC，其中包含的内容未经 Innersloth LLC 认可或以其他方式赞助。 此处包含的部分材料是 Innersloth LLC 的财产。 © Innersloth LLC。

## 资源

### 很抱歉，由于目前模组还在开发中，故没有发布版本，有能力的话可以下载源码自行编译

AmongUs 版本: **2022.12.14**
**修改最新版: [点我（暂未发布）](https://github.com/KARPED1EM/TownOfHost/releases)**
官方原版: [点我](https://github.com/tukasa0001/TownOfHost/releases)

## 特点

一开始是为了帮群友解决模组损坏的问题，于是我从零摸索C语言，成功地解决这个问题后顺带加了点功能并修改了一些内容，于是有了这个版本并命名为 **Town Of Host Edited** 也就是 **TOHE**。

这个模组存在的本质意义是让我学习一些新的知识，所以我并不会及时地更新他。等我学习完成或退游后就会断更了。这是我第一次使用C语言，因此这个修改版的代码会很烂，并且可能伴随大量的Bug。这也是因为我并没有同伴协助我开发或测试，所有的开发均由我一人完成。因此我很抱歉给您带来了糟糕的游戏体验。当然如果您愿意使用我修改的版本，我非常感激！若您在使用过程中遇到问题或有什么意见，非常欢迎您提个 issue，或者是通过 **[QQ群：134830517](https://jq.qq.com/?_wv=1027&k=CBkjGf2Y)** 来联系我。

## 内容

### 1. 删除对原版客户端的非必要提示

### 2. 强制更新检查结果为成功以避免模组损坏，所以接下来请您及时注意是否有新版本发布

### 3. 游戏结果显示的顺序微调

### 4. 房主名称显示微调

   <img src="Images/HostName.webp" alt="HostName" style="zoom: 50%;" />

### 5. 增加自动警告或踢出使用违禁词的人的功能，可以在`./TOH_DATA/bannedwords.txt`自定义违禁词

   <img src="Images/AutoKickMenu.webp" alt="AutoKickMenu" style="zoom: 25%;" />

### 6. 增加自动踢出催开始的人的功能，需要在设置打开，如上图所示

   <img src="Images/AutoKickStart.png" alt="AutoKickStart" style="zoom:33%;" />

### 7. 隐藏游戏设置后自定义显示的内容，您可以在 `.\Language\SChinese.dat` 文件中修改 `Message.NowOverrideText` 词条。

   <img src="Images/DIYNow.png" alt="DIYNow" style="zoom:33%;" />

### 8. 增加修改等级的指令（仅限房主）

```聊天指令
#示例：修改等级为86级
/level 86
```

### 9. 简化部分指令

```聊天指令
#处决玩家
/exe
#房主消息
/s
```

### 10. 修改使查询职业技能可以使用中文

```聊天指令
#示例：查询术士的技能描述
/r 术士
```

### 11. 汉化部分没有中文的文本

### 12. 让100%概率的职业随机出现（不完全实现）

### 13. 修改部分汉化使翻译更合适

### 14. 更新至官方最新提交 #1241 以修复部分Bug

### 15. 增加或修改可用快捷键（以下）

|      快捷键       |          功能          |   权限   |
| :---------------: | :--------------------: | :------: |
| Shitf + M + Enter |        远程拍灯        | 仅限房主 |
| Shitf + R + Enter |        强制拍灯        | 仅限房主 |
| Shitf + O + Enter | 一键完成所有自己的任务 | 仅限房主 |
| Shitf + K + Enter |    修改击杀CD为0秒     | 仅限房主 |
| Shitf + E + Enter |      在会议上自杀      | 仅限房主 |
| Shitf + D + Enter |  打开飞艇地图的所有门  | 仅限房主 |
| Shitf + V + Enter |      关闭匿名投票      | 仅限房主 |

