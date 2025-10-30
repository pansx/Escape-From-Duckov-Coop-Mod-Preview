---
inclusion: always
---

# 编译环境配置指南

## 环境变量配置

### 正确的环境变量设置
项目使用 `DUCKOV_GAME_DIRECTORY` 环境变量，而不是 `DUCKOV_GAME_MANAGED`。

**重要说明**：
- README文档中提到的 `DUCKOV_GAME_MANAGED` 是错误的
- 实际需要设置的是 `DUCKOV_GAME_DIRECTORY`
- 系统会通过 `Directory.Build.props` 自动计算 `DUCKOV_GAME_MANAGED` 路径

### 环境变量层次结构
```
DUCKOV_GAME_DIRECTORY (用户设置)
  ↓
GameDirectory (MSBuild属性)
  ↓
DUCKOV_DATA_DIRECTORY = $(GameDirectory)\Duckov_Data
  ↓
DUCKOV_GAME_MANAGED = $(DUCKOV_DATA_DIRECTORY)\Managed
```

### 配置步骤
1. 运行 `SetEnvVars_Permanent.bat` 脚本
2. 输入游戏根目录路径，例如：`C:\Steam\steamapps\common\Escape from Duckov`
3. 脚本会设置 `DUCKOV_GAME_DIRECTORY` 环境变量
4. 重启 Visual Studio 以加载新的环境变量

## 编译问题诊断

### 常见编译错误
1. **找不到类型或命名空间** - 通常是环境变量未设置或路径错误
2. **缺少程序集引用** - 游戏DLL文件不存在或路径不正确
3. **权限问题** - 需要确保对游戏目录有读取权限

### 验证环境配置
```cmd
# 检查环境变量
echo %DUCKOV_GAME_DIRECTORY%

# 验证路径存在
dir "%DUCKOV_GAME_DIRECTORY%\Duckov_Data\Managed"
```

### 依赖文件检查
确保以下文件存在于 `Shared` 目录：
- `0Harmony.dll`
- `LiteNetLib.dll`

确保游戏 Managed 目录包含所有必需的Unity和游戏DLL文件。

## 文档错误修正

### README文档中的错误
- README中提到设置 `DUCKOV_GAME_MANAGED` 是错误的
- 应该设置 `DUCKOV_GAME_DIRECTORY`
- 脚本 `SetEnvVars_Permanent.bat` 是正确的，设置的就是 `DUCKOV_GAME_DIRECTORY`

### 建议的文档修正
README文档需要更新以反映正确的环境变量名称和配置流程。

## 编译脚本

项目提供了多个编译脚本以简化开发流程：

### 1. Build.bat - 完整编译脚本
- **功能**: 完整的编译流程，包含环境检查和错误处理
- **特点**: 
  - 自动检查环境变量和依赖文件
  - 清理旧的编译输出
  - 显示详细的编译信息
  - 自动复制到模组目录（如果配置了DUCKOV_MODS_DIRECTORY）
- **使用**: 双击运行或在命令行执行 `Build.bat`

### 2. QuickBuild.bat - 快速编译脚本
- **功能**: 快速编译，适合频繁测试
- **特点**:
  - 自动设置默认游戏路径
  - 静默编译，只显示结果
  - 编译速度快
- **使用**: 双击运行或在命令行执行 `QuickBuild.bat`

### 3. BuildDebug.bat - 调试编译脚本
- **功能**: 编译Debug版本，包含调试符号
- **特点**:
  - 生成包含调试信息的PDB文件
  - 详细的编译输出
  - 适合开发和调试
- **使用**: 双击运行或在命令行执行 `BuildDebug.bat`

## 编译验证结果

✅ **编译成功验证**
- 环境变量: `DUCKOV_GAME_DIRECTORY="C:\SteamLibrary\steamapps\common\Escape from Duckov"`
- 编译命令: `dotnet build EscapeFromDuckovCoopMod.sln --configuration Release`
- 结果: 编译成功，仅有17个警告（主要是未使用的字段和无法访问的代码）
- 输出: `EscapeFromDuckovCoopMod.dll` 成功生成

### 编译警告分析
编译过程中的17个警告主要包括：
1. **MSB3245警告**: System程序集引用警告（不影响功能）
2. **CS1998警告**: 异步方法缺少await运算符
3. **CS0162警告**: 检测到无法访问的代码
4. **CS0169/CS0414警告**: 未使用的字段

这些警告不影响模组的正常功能，但可以在后续开发中进行优化。

### 推荐的编译流程
1. **首次编译**: 使用 `Build.bat` 进行完整检查
2. **日常开发**: 使用 `QuickBuild.bat` 快速编译测试
3. **调试问题**: 使用 `BuildDebug.bat` 生成调试版本