---
inclusion: manual
---

# 日志获取方法

## 概述

项目使用远程日志服务器收集游戏日志，可以通过HTTP API获取不同客户端的日志。

## API端点

- **基础URL**: `http://127.0.0.1:8080`
- **日志API**: `/api/logs`
- **参数**:
  - `clientId`: 客户端ID（1=机器1，2=机器2）
  - `page`: 页码（从0开始）
  - `size`: 每页条数

## PowerShell获取日志脚本

### 获取日志

```powershell
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

Invoke-WebRequest -UseBasicParsing -Uri "http://127.0.0.1:8080/api/logs?clientId=1&page=0&size=30" `
  -WebSession $session `
  -Headers @{
    "Accept"="application/json, text/plain, */*"
    "Accept-Encoding"="gzip, deflate, br, zstd"
    "Authorization"="Bearer eyJhbGciOiJIUzM4NCJ9.eyJyb2xlIjoiQURNSU4iLCJ1c2VySWQiOjEsInN1YiI6ImFkbWluIiwiaWF0IjoxNzYyNTQyMzQ0LCJleHAiOjE3NjI2Mjg3NDR9.I5jmhmr1GzkNXkwaHdAw4pmcvfTvdKDwhBfE8QF8I-6RO3NaSDAbx_qjT4aRDlfI"
    "Cache-Control"="no-cache"
    "Referer"="http://127.0.0.1:8080/"
    "origin"="http://127.0.0.1"
  }
```

