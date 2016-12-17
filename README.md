# RegionTrigger for TShock

RegionTrigger is a **TShock-based** plugin aimed to trigger special events when players enter a region.

Also in **Chinese** now!

## Requirement:
- API Version: 2.0
- TShock Version: 4.3.21

## Commands:
- `/rt set-<property> <region> [--del] <value>` -- *Sets regions*
  ### Available properties:
  - Event: `event(e)`
  - Bans: `projban(pb), itemban(ib), tileban(tb)`
  - Messages: `entermsg(em), leavemsg(lm), messageinterval(msgitv/mi)`
  - Group: `tempgroup(tg)`

  **e.g.** `/rt set-event main-region nopvp`
          ` /rt set-tempgroup main-region admin`
  
- `/rt show <region>` -- *Gets information about a specific region*
- `/rt reload` -- *Reloads data in database*
- `/rt --help [page]` -- *Gets helps*

You can also find this plugin in [TShock offical forum][tshockco].

## Permission
- `regiontrigger.manage` -- *Manages regions' events.*
- `regiontrigger.bypass.tileban` -- *Use banned tiles in regions.*
- `regiontrigger.bypass.projban` -- *Use banned projectiles in regions.*
- `regiontrigger.bypass.itemban` -- *Use banned items in regions.*
- `regiontrigger.bypass.tempgroup` -- *Player won't be switched to tempgroup.*
- `regiontrigger.bypass.pvp` -- *Player will be able to toggle his PvP status.*
- `regiontrigger.bypass.nopvp` -- *Player will be able to toggle his PvP status.*
- `regiontrigger.bypass.private` -- *Enters private regions.*

## Available events now:
- `EnterMsg` - *Sends player a specific message when entering regions.*
- `LeaveMsg` - *Sends player a specific message when leaving regions.*
- `Message` - *Sends player in regions a specific message.*
- `TempGroup` - *Alters players' tempgroups when they are in regions.*
- `Itemban` - *Disallows players in specific regions from using banned items.*
- `Projban` - *Disallows players in specific regions from using banned projectiles.*
- `Tileban` - *Disallows players in specific regions from using banned tiles.*
- `Kill` - *Kills players entering to region.*
- `Godmode` - *Turns players' godmode on when they are in regions.*
- `Pvp` - *Turns players' PvP status on when they are in regions.*
- `NoPvp` - *Disallows players from enabling their PvP mode.*
- `Private` - *Disallows players without permission from entering region.*

   [tshockco]: <https://tshock.co/xf/index.php?resources/regiontrigger.157/>
