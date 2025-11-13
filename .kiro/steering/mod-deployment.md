---
inclusion: always
---

# Mod 部署流程

## 概述

本文档描述如何将构建好的 Mod DLL 文件部署到游戏目录。

## 部署方式

### 方式一：API 上传（推荐）⭐

使用本地 API 服务自动上传和部署 DLL 文件。

#### 前置条件

- 本地 API 服务运行在 `http://localhost:8080`
- 拥有有效的 Bearer Token

#### 上传命令

```bash
curl.exe -X POST "http://localhost:8080/api/mods/upload" `
  -H "Authorization: Bearer <TOKEN>" `
  -F "file=@EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll"
```

#### 当前 Token

```
eyJhbGciOiJIUzM4NCJ9.eyJyb2xlIjoiQURNSU4iLCJ1c2VySWQiOjEsInN1YiI6ImFkbWluIiwiaWF0IjoxNzYyNjYwNDcyLCJleHAiOjE3NjI3NDY4NzJ9.6d2LQ-bKOVPo85-CepRmCoZFx7M7G4mPPlTA7AmvK1NyiUccL79y184qa5mVK-bO
```

**注意**: Token 有效期为 24 小时，过期后需要重新获取。

#### 完整上传命令（复制即用）

```bash
curl.exe -X POST "http://localhost:8080/api/mods/upload" -H "Authorization: Bearer eyJhbGciOiJIUzM4NCJ9.eyJyb2xlIjoiQURNSU4iLCJ1c2VySWQiOjEsInN1YiI6ImFkbWluIiwiaWF0IjoxNzYyNjYwNDcyLCJleHAiOjE3NjI3NDY4NzJ9.6d2LQ-bKOVPo85-CepRmCoZFx7M7G4mPPlTA7AmvK1NyiUccL79y184qa5mVK-bO" -F "file=@EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll"
```

#### 响应示例

成功上传后会返回：

```json
{
  "artifactPath": "C:/SteamLibrary/steamapps/common/Escape from Duckov/Duckov_Data/Mods/EscapeFromDuckovCoopMod/EscapeFromDuckovCoopMod.dll",
  "latestVersion": "EscapeFromDuckovCoopMod-20251109-1215"
}
```

#### 优点

- ✅ 自动部署到正确位置
- ✅ 自动生成版本号
- ✅ 无需手动复制文件
- ✅ 支持版本管理


---

## 完整工作流程

### 1. 修改代码

编辑 C# 源文件，实现新功能或修复 Bug。

### 2. 检查诊断

```bash
# 使用 getDiagnostics 工具检查语法错误
getDiagnostics(["EscapeFromDuckovCoopMod/Main/UI/WaitingSynchronizationUI.cs"])
```

### 3. 编译项目

```bash
dotnet build EscapeFromDuckovCoopMod.sln --configuration Release
```

**期望输出**:
```
在 X.X 秒内生成 成功，出现 XX 警告
```

### 4. 部署 DLL

使用 API 上传（推荐）：

```bash
curl.exe -X POST "http://localhost:8080/api/mods/upload" -H "Authorization: Bearer eyJhbGciOiJIUzM4NCJ9.eyJyb2xlIjoiQURNSU4iLCJ1c2VySWQiOjEsInN1YiI6ImFkbWluIiwiaWF0IjoxNzYyNjYwNDcyLCJleHAiOjE3NjI3NDY4NzJ9.6d2LQ-bKOVPo85-CepRmCoZFx7M7G4mPPlTA7AmvK1NyiUccL79y184qa5mVK-bO" -F "file=@EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll"
```

### 5. 测试验证

启动游戏，测试新功能是否正常工作。

---

## 故障排查

### Token 过期

**症状**: 上传时返回 401 Unauthorized

**解决**: 
1. 联系管理员获取新的 Token
2. 更新本文档中的 Token
3. 重新执行上传命令

### API 服务未运行

**症状**: 连接被拒绝或超时

**解决**:
1. 检查 API 服务是否运行：`curl http://localhost:8080/health`
2. 启动 API 服务
3. 重新执行上传命令

### 文件路径错误

**症状**: 找不到文件

**解决**:
1. 确认 DLL 文件已构建：`Get-ChildItem EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\*.dll`
2. 检查当前工作目录是否正确
3. 使用绝对路径

---

## 快速参考

### 一键构建并部署

```bash
# 编译
dotnet build EscapeFromDuckovCoopMod.sln --configuration Release

# 上传
curl.exe -X POST "http://localhost:8080/api/mods/upload" -H "Authorization: Bearer eyJhbGciOiJIUzM4NCJ9.eyJyb2xlIjoiQURNSU4iLCJ1c2VySWQiOjEsInN1YiI6ImFkbWluIiwiaWF0IjoxNzYzMDU4ODk5LCJleHAiOjE3NjMxNDUyOTl9.l_yTsyYh0c_Rw9R-nsIfRnCy4pSDMFm9jMZjJ_G5vQpFgU1cYyedQby0OdoO-iWU" -F "file=@EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll"
```

### 检查部署结果

```bash
# 查看目标文件信息
Get-Item "C:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Mods\EscapeFromDuckovCoopMod\EscapeFromDuckovCoopMod.dll" | Select-Object Name, Length, LastWriteTime
```

---

## 注意事项

1. **游戏必须关闭**: 部署前确保游戏已关闭，否则文件可能被占用
2. **备份重要版本**: 重大更新前建议备份当前可用的 DLL
3. **版本号管理**: API 上传会自动生成版本号（格式：`ModName-YYYYMMDD-HHMM`）
4. **Token 安全**: 不要将 Token 提交到公共仓库

---

*最后更新: 2025-11-09*
