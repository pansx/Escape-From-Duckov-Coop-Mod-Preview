AI相关的刷屏信息认不再有碑
4. 检查日志确户端都能看到墓碑的场景，确认所有客 多个客户端同时进入有墓见
3.连，查看墓碑是否仍然可的场景中，客户端断开重2. 在有墓碑存在
查看墓碑是否，重新进入 创建墓碑后退出游戏试建议

1.正确显示

## 测物品在客户端 墓碑- ✅提高调试体验

- ✅ 减少刷屏日志，的重叠问题免空墓碑和恢复墓碑
- ✅ 避有现有墓碑客户端重连后能看到所端
- ✅ 同步给所有客户确恢复并景加载时正✅ 墓碑在场效果

- 

## 预期服务端发送墓碑物品状态
4. 请求字典d` otByUi_cliLo
3. 注册到 `oxAt` 创建墓碑otb.SpawnDeadLoDeadLootBox 调用 `` 消息
2.D_LOOT_SPAWN户端接收 `DEA 客# 客户端处理
1.

##户端接收并创建本地墓碑给新客户端
5. 客WN` PAEAD_LOOT_S个发送 `D有墓碑
4. 逐 遍历当前场景的所3.中检测到新连接
onnected` 务端在 `OnPeerC连接到服务端
2. 服
1. 客户端## 客户端重连时 给所有客户端

#N`OOT_SPAWAD_L 广播 `DE游戏对象和物品
5. 创建墓碑文件恢复墓碑数据
4.从持久化
3. 内存中的空墓碑s`
2. 清理bstoneTomLoadScene `景时调用载场载时
1. 服务端加景加

### 场

## 工作流程[AI-SEED]`ECV]`
- ``[AI-REND]`
- 
- `[AI-S]` `[NOW AI`
-Y]`[AI-APPL刷屏日志：
- 关的
注释掉了AI相r/Mod.cs`
/Main/LoadeopModckovCoscapeFromDu `EIHandle.cs`,n/AI/AopMod/MaiomDuckovCo*: `EscapeFr文件*移除刷屏日志

**`

### 5. 
``
}e;on = trudsRestorati
    neetingInv))
{(exismptyentoryE IsInvgInv) ||t(existintExisoneGameObjec!DoesTombst= null || ngInv =sti空
if (exi是否为现有墓碑
}

// 检查nue;
    contiotUid}");stone.loUid={tombotombstone: lompty t] Skipping e"[TOMBSTONEebug.Log($
{
    DCount == 0)e.items.| tombstonms == null |ne.iteto碑
if (tombs过没有物品的墓 跳arp
//sh``c复的检测逻辑：

`碑恢

增强了墓4. 改进墓碑恢复逻辑`

### 
}
``否真的没有物品检查Inventory是 // )
{
   entoryntory invInveryEmpty(sInventoe bool I}

privat碑
 // 移除空墓
    }
   ;
        }ootUid)(ls.AddTombstone     empty {
             ))
 inventoryty(yEmpInventorIs= null || y =inventor (
        if)
    {ootByUidvLn _sr (var kv iach  fore;
  t>()st<ines = new LimptyTombston
    var ebstones()
{tyTomEmpupan Clerivate void
prp`csha

``清理方法：添加了空墓碑检测和`

();
``TombstonesupEmpty碑
Clean的墓物品为0存中 首先清理内
//harp

```cs理内存中的空墓碑：碑前，先清恢复墓er.cs`

在tManagLooice/rveSeenMain/ScckovCoopMod/capeFromDu*: `Es文件*

**墓碑避免重叠 清理空## 3.
#}
```

新客户端
    }WN消息给D_LOOT_SPA// 发送DEA    旋转信息
      // 获取墓碑位置和 {
      ByUid)
   ce._srvLootr.InstanManagein Lootch (var kv ea
    for发送给新客户端碑，// 遍历所有服务端的墓  
  eer)
{nt(NetPeer pNewClieTombstonesTo Syncivate void``csharp
pr法：

`Client` 方ewbstonesToNSyncTom
添加了 `
```
eer);nt(pwClienesToNesto户端
SyncTomb的客有墓碑给新连接/ 同步当前场景的所
/
```csharp有墓碑：
同步当前场景的所时，主动新客户端连接中，当ted` 方法ecPeerConns`

在 `OnetService.cpMod/Main/NCoopeFromDuckov件**: `Esca墓碑

**文. 客户端重连时同步 2##`

#d);
}
``rderebleOMethod.Reliaer, Deliveryll(writendToAger.SananetM
    
    ion);atne.rottombstoon(tQuaternier.Pu
    writposition);stone.ombV3cm(tr.Putte  wri
  otUid);tombstone.lor.Put(
    writed);ne.aiItombstoPut(
    writer.;(scene)Putiter.WN);
    wrAD_LOOT_SPA)Op.DE(byteer.Put(writt();
    ter.Rese
{
    wriy inventory)ne, InventorstooneData tombtored(TombstombstoneRes BroadcastTe voidp
privat
```cshar格式通知客户端：
PAWN` 消息OOT_S`DEAD_L，使用 red` 方法estobstoneRtTom`Broadcas``

添加了 dInv);
`storebstone, red(tomneRestoretoadcastTombs
Bro碑恢复信息给所有客户端
// 广播墓csharp

```立即广播给所有客户端：复墓碑后，法中，当成功恢stones` 方ombneTScead
在 `Lo
tManager.cs`e/LooicSceneServin/opMod/MaCoeFromDuckovcap**文件**: `Es恢复

景加载时广播墓碑 场
### 1.
## 修复方案

碑重叠的情况复的墓能存在老的空墓碑和新恢重叠问题**：可空墓碑墓碑信息
3. **播之后，客户端就收不到如果重连发生在墓碑广户端在场景加载后会重连，时机问题**：客. **客户端重连，没有同步给客户端
2端恢复墓碑行**：墓碑系统只在服务服务端执
1. **墓碑恢复只在# 根本原因分析
。

#无法看到墓碑中的物品户端正确恢复，但客务端日志显示墓碑虽然服户端重连后看不到物品，户报告墓碑系统在客## 问题描述

用修复

# 墓碑客户端同步