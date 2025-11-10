---
inclusion: always
---

# 日志获取方法

## 推荐方法：使用批处理脚本 ⭐

**一键获取和分析日志**：

```bash
.\get-logs.bat
```

这个脚本会自动：

1. 获取主机端日志（clientId=1）
2. 获取客户端日志（clientId=2）
3. 转换为易读的文本格式（.txt 文件）
4. 搜索并显示 SetId 相关日志
5. 显示 JSON 消息日志

**生成的文件**：

-   `logs_host.txt` - 主机端日志（文本格式，易读）
-   `logs_client.txt` - 客户端日志（文本格式，易读）
-   `logs_host.json` - 主机端日志（JSON 格式，原始数据）
-   `logs_client.json` - 客户端日志（JSON 格式，原始数据）

直接读取log文件,不要使用指令筛选,禁止不执行bat就直接看老log文件
记得从后往前读,因为一般问题出现后开发者会马上终止游戏