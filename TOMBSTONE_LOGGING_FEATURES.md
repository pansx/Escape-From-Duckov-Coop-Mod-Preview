# 墓碑持久化系统 - 详细日志功能

## 🔍 新增日志功能

为了更好地调试和监控墓碑持久化系统，我添加了详细的日志输出功能。

### 📋 日志类型

#### 1. 用户数据加载日志
```
[TOMBSTONE] Reading tombstone file: {filePath}
[TOMBSTONE] Successfully loaded user data from file: userId={userId}, tombstones={count}
[TOMBSTONE] File tombstone {i}: lootUid={lootUid}, sceneId={sceneId}, items={count}, position={position}
[TOMBSTONE]   Item {j}: position={position}, typeId={typeId}, stack={stack}
```

#### 2. 场景墓碑查询日志
```
[TOMBSTONE] Getting scene tombstones for userId={userId}, sceneId={sceneId}
[TOMBSTONE] Found {count} tombstones for userId={userId}, sceneId={sceneId}
[TOMBSTONE] Scene tombstone {i}: lootUid={lootUid}, items={count}, position={position}
[TOMBSTONE]   Item summary: TypeID:594x1, TypeID:18x1, ...
```

#### 3. 场景加载恢复日志
```
[TOMBSTONE] Scanning tombstone directory: {path}
[TOMBSTONE] Found {count} tombstone files in directory
[TOMBSTONE] Processing file: {fileName} (size: {bytes} bytes) for userId: {userId}
[TOMBSTONE] User {userId} has {count} tombstones in scene {sceneId}
[TOMBSTONE] ✓ Successfully restored tombstone: userId={userId}, lootUid={lootUid}, items={count}
```

#### 4. 恢复总结日志
```
[TOMBSTONE] === RESTORATION SUMMARY ===
[TOMBSTONE] Scene: {sceneId}
[TOMBSTONE] Files processed: {count}
[TOMBSTONE] Tombstones restored: {count}
[TOMBSTONE] Total items restored: {count}
[TOMBSTONE] Memory dictionary size: {count}
```

#### 5. 墓碑创建日志
```
[TOMBSTONE] Creating tombstone in scene: lootUid={lootUid}, position={position}, items={count}
[TOMBSTONE]   Creating item {i}: pos={position}, typeId={typeId}, stack={stack}
[TOMBSTONE] ✓ Successfully created tombstone: lootUid={lootUid}, rebuiltItems={count}
```

### 🎯 日志符号说明

- `✓` - 成功操作
- `✗` - 失败操作  
- `⚠` - 警告信息

### 📊 监控信息

#### 用户数据统计
- 用户ID和对应的墓碑数量
- 每个墓碑的物品数量和类型
- 文件大小和读取状态

#### 场景恢复统计
- 处理的文件数量
- 恢复的墓碑数量
- 恢复的物品总数
- 内存字典大小

#### 物品详细信息
- 物品类型ID (TypeID)
- 物品堆叠数量 (Stack)
- 物品在容器中的位置
- 物品类型汇总统计

### 🔧 调试用途

#### 1. 问题诊断
通过日志可以快速定位：
- 墓碑数据是否正确保存
- 场景切换时数据是否正确加载
- 物品数据是否完整

#### 2. 性能监控
- 文件读取耗时
- 数据恢复效率
- 内存使用情况

#### 3. 数据验证
- 验证物品数量是否匹配
- 检查场景ID是否正确
- 确认用户ID映射关系

### 📝 日志示例

```
[TOMBSTONE] Scanning tombstone directory: C:\...\StreamingAssets\TombstoneData
[TOMBSTONE] Found 2 tombstone files in directory
[TOMBSTONE] Processing file: Client_7925a9d2_tombstones (size: 1024 bytes) for userId: Client:7925a9d2
[TOMBSTONE] Getting scene tombstones for userId=Client:7925a9d2, sceneId=Level_GroundZero
[TOMBSTONE] Successfully loaded user data from file: userId=Client:7925a9d2, tombstones=1
[TOMBSTONE] File tombstone 0: lootUid=2, sceneId=Level_GroundZero, items=6, position=(332.15, -7.89, 158.87)
[TOMBSTONE]   Item 0: position=0, typeId=594, stack=1
[TOMBSTONE]   Item 1: position=1, typeId=18, stack=1
[TOMBSTONE] Found 1 tombstones for userId=Client:7925a9d2, sceneId=Level_GroundZero
[TOMBSTONE] Scene tombstone 0: lootUid=2, items=6, position=(332.15, -7.89, 158.87)
[TOMBSTONE]   Item summary: TypeID:594x1, TypeID:18x1, TypeID:888x1, TypeID:133x1
[TOMBSTONE] Restoring tombstone: userId=Client:7925a9d2, lootUid=2, items=6
[TOMBSTONE] Creating tombstone in scene: lootUid=2, position=(332.15, -7.89, 158.87), items=6
[TOMBSTONE]   Creating item 0: pos=0, typeId=594, stack=1
[TOMBSTONE]   Creating item 1: pos=1, typeId=18, stack=1
[TOMBSTONE] ✓ Successfully restored tombstone: userId=Client:7925a9d2, lootUid=2, items=6
[TOMBSTONE] === RESTORATION SUMMARY ===
[TOMBSTONE] Scene: Level_GroundZero
[TOMBSTONE] Files processed: 2
[TOMBSTONE] Tombstones restored: 1
[TOMBSTONE] Total items restored: 6
[TOMBSTONE] Memory dictionary size: 1
```

### 🎯 使用建议

1. **测试时关注的日志**：
   - 查看 `RESTORATION SUMMARY` 确认数据恢复情况
   - 检查 `Item summary` 验证物品类型和数量
   - 观察 `✓` 和 `✗` 符号判断操作成功率

2. **问题排查**：
   - 如果墓碑物品为0，检查文件读取和物品创建日志
   - 如果场景ID不匹配，查看场景墓碑查询日志
   - 如果用户ID错误，检查用户数据加载日志

这些详细的日志将帮助快速定位和解决墓碑持久化系统中的任何问题。