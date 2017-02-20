# RegionTrigger for TShock

RegionTrigger 是基于**TShock**的插件，用途是设定区域内事件。

## 安装需要
- API版本: 2.0
- TShock版本: 4.3.22

## 指令教程
- `/rt set-<属性> <区域名> [--del] <值>` -- *Sets regions*
>  ### 可用属性 （括号内为简写）
>  - 事件: `event(e)`
>  - 封禁: `projban(pb), itemban(ib), tileban(tb)`
>  - 信息: `entermsg(em), leavemsg(lm), messageinterval(msgitv/mi)`
>  - 组: `tempgroup(tg)`

  **例子** `/rt set-event main-region nopvp` `//给main-region区域加上事件nopvp`
          ` /rt set-tempgroup main-region admin` `//设定main-region区域的临时组为admin`
  
- `/rt show <区域名>` -- *获取区域的信息*
- `/rt reload` -- *重新加载数据库数据*
- `/rt --help [页码]` -- *获取帮助*

本插件在[TShock官方论坛][tshockco]上有发布。

## 权限
- `regiontrigger.manage` -- *管理区域事件*
- `regiontrigger.bypass.tileban` -- *区域内使用被禁止的物块*
- `regiontrigger.bypass.projban` -- *区域内使用被禁止的抛射体*
- `regiontrigger.bypass.itemban` -- *区域内使用被禁止的物品*
- `regiontrigger.bypass.tempgroup` -- *跳过区域内换临时组*
- `regiontrigger.bypass.pvp` -- *跳过区域强制pvp*
- `regiontrigger.bypass.nopvp` -- *跳过区域禁止pvp*
- `regiontrigger.bypass.private` -- *可以进入禁止进入的区域*
- `regiontrigger.bypass.tempperm` -- *跳过区域内临时权限*

## 可用事件
- `EnterMsg` - *进入区域时发送消息*
- `LeaveMsg` - *离开区域时发送消息*
- `Message` - *以特定间隔区域内玩家发送消息*
- `TempGroup` - *应用区域内临时组*
- `Itemban` - *区域内禁用特定物品*
- `Projban` - *区域内禁用特定抛射体*
- `Tileban` - *区域内禁放特定物块*
- `Kill` - *杀死进入区域的玩家*
- `Godmode` - *区域内玩家无敌*
- `Pvp` - *区域内强制PvP*
- `NoPvp` - *区域内禁止PvP*
- `InvariantPvp` - *区域内禁止改变PvP状态*
- `Private` - *禁止进入区域*
- `TempPermission` - *区域内玩家获得临时权限*

   [tshockco]: <https://tshock.co/xf/index.php?resources/regiontrigger.157/>
