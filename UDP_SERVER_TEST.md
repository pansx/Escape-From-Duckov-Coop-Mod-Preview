# UDP服务器测试说明

## 概述

DirectP2PNetwork现在已经重写为与NetService集成，使用统一的9050端口。

## 测试步骤

### 1. 编译验证
```bash
./build.bat
```
✅ 编译成功，无错误

### 2. 测试客户端
运行 `quick_test_client.py` 来测试UDP连接：
```bash
python quick_test_client.py
```

### 3. 预期行为

**游戏未运行时:**
- 连接被拒绝（正常行为）
- 错误: `[WinError 10054] 远程主机强迫关闭了一个现有的连接`

**游戏运行且NetService启动时:**
- 应该能够发送UDP消息到9050端口
- NetService会处理聊天消息

## 实现要点

### DirectP2PNetwork集成
- 使用9050端口（与NetService保持一致）
- 不再创建独立的UDP服务器
- 通过NetService发送和接收消息
- 支持主机和客户端模式

### 消息格式
- 操作码255标识聊天消息
- JSON格式的ChatMessage数据
- 兼容NetService的消息协议

### 测试客户端功能
- 发送主机发现请求
- 发送聊天消息测试
- 验证消息格式和大小

## 下一步

需要在游戏运行时测试完整的聊天功能：
1. 启动游戏
2. 创建房间（启动NetService主机模式）
3. 运行测试客户端验证UDP消息接收
4. 测试聊天消息的显示和处理