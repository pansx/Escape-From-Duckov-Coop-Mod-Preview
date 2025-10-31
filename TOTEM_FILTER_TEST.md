# 图腾过滤测试

## 测试目标
验证新的物品过滤系统是否能正确过滤掉图腾和其他不可掉落物品。

## 测试步骤
1. 玩家携带以下物品：
   - 普通物品（应该掉落）
   - 图腾类物品（应该被过滤）
   - 近战武器（应该被过滤）
   - 特殊物品（应该被过滤）

2. 玩家死亡，触发墓碑创建

3. 检查日志输出，确认过滤逻辑正常工作

## 预期日志输出

### 过滤开始
```
[TOMBSTONE] Filtering non-droppable items from death inventory
[TOMBSTONE] Starting item filtering for inventory with X slots
```

### 物品过滤详情
```
[TOMBSTONE] Filtering totem/special item: TypeID=XXX, Name=图腾名称
[TOMBSTONE] Filtering melee weapon: TypeID=XXX, Name=武器名称
[TOMBSTONE] Item allowed to drop: TypeID=XXX, Name=普通物品名称
[TOMBSTONE] Filtered non-droppable item: TypeID=XXX, Name=被过滤物品名称
```

### 过滤结果
```
[TOMBSTONE] Item filtering complete: X droppable items out of Y total items (Z filtered)
[TOMBSTONE] After filtering: X droppable items remain
```

## 关键检查点
1. **图腾过滤**：包含 "totem", "图腾", "charm", "amulet", "talisman", "护身符", "符咒" 等关键词的物品应该被过滤
2. **近战武器过滤**：包含 "knife", "sword", "axe", "hammer", "blade", "melee", "刀", "剑", "斧", "锤" 等关键词的物品应该被过滤
3. **系统物品过滤**：包含 "starter", "default", "basic", "tutorial", "quest", "mission", "初始", "默认", "基础", "教程", "任务", "剧情" 等关键词的物品应该被过滤

## 测试结果验证
- [ ] 图腾不再出现在墓碑中
- [ ] 近战武器不再出现在墓碑中
- [ ] 普通物品正常掉落到墓碑中
- [ ] 日志显示正确的过滤统计信息
- [ ] 没有异常或错误日志

## 故障排除
如果过滤不生效：
1. 检查物品名称是否包含预期的关键词
2. 检查是否有异常阻止了过滤逻辑执行
3. 确认 `TombstonePersistence.FilterDroppableItems` 方法被正确调用
4. 检查物品的 TypeID 是否在黑名单中

## 扩展测试
可以通过修改关键词列表来测试不同类型的物品过滤：
- 添加新的图腾关键词
- 添加特定的 TypeID 到黑名单
- 测试中英文关键词匹配