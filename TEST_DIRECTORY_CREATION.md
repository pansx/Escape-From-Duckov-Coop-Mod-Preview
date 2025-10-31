# 墓碑目录创建测试

## 测试步骤

1. **启动游戏并加载模组**
   - 检查控制台是否显示：`[TOMBSTONE] TombstonePersistence Init() called`
   - 检查是否显示：`[TOMBSTONE] TombstonePersistence initialized with path: ...`

2. **触发墓碑保存**
   - 杀死一个AI或玩家死亡
   - 观察控制台输出

3. **预期的正常输出**
   ```
   [TOMBSTONE] TombstonePersistence Init() called
   [TOMBSTONE] Initialized _streamingAssetsPath: C:/Program Files (x86)/Steam/steamapps/common/Escape from Duckov/Duckov_Data/StreamingAssets/TombstoneData
   [TOMBSTONE] Directory does not exist, creating: C:/Program Files (x86)/Steam/steamapps/common/Escape from Duckov/Duckov_Data/StreamingAssets/TombstoneData
   [TOMBSTONE] Successfully created directory: C:/Program Files (x86)/Steam/steamapps/common/Escape from Duckov/Duckov_Data/StreamingAssets/TombstoneData
   [TOMBSTONE] TombstonePersistence initialized with path: C:/Program Files (x86)/Steam/steamapps/common/Escape from Duckov/Duckov_Data/StreamingAssets/TombstoneData
   ```

4. **如果权限问题，预期的备用输出**
   ```
   [TOMBSTONE] Failed to ensure directory exists ...: System.UnauthorizedAccessException: ...
   [TOMBSTONE] Trying fallback path: C:/Users/.../AppData/LocalLow/InitLoader/Escape from Duckov/TombstoneData
   [TOMBSTONE] Created fallback directory: C:/Users/.../AppData/LocalLow/InitLoader/Escape from Duckov/TombstoneData
   [TOMBSTONE] Using fallback path: C:/Users/.../AppData/LocalLow/InitLoader/Escape from Duckov/TombstoneData
   ```

## 改进内容

### 1. 增强的目录创建逻辑
- 添加了详细的调试日志
- 验证目录创建是否成功
- 提供备用路径机制

### 2. 备用路径机制
- 如果 StreamingAssets 目录创建失败（权限问题）
- 自动切换到 persistentDataPath（用户数据目录）
- 确保墓碑数据能够正常保存

### 3. 更好的错误处理
- 捕获并记录所有异常
- 提供清晰的错误信息
- 不会因为目录创建失败而崩溃

## 文件位置

正常情况下：
```
C:/Program Files (x86)/Steam/steamapps/common/Escape from Duckov/Duckov_Data/StreamingAssets/TombstoneData/
```

备用情况下：
```
C:/Users/[用户名]/AppData/LocalLow/InitLoader/Escape from Duckov/TombstoneData/
```