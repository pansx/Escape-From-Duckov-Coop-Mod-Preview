# 本地化测试指南

## 问题诊断步骤

### 1. 检查构建输出
构建完成后，检查以下目录是否存在本地化文件：
```
EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\Localization\zh-CN.json
```

### 2. 检查游戏日志
启动游戏后，在控制台或日志文件中查找以下信息：
```
[CoopLocalization] Initialized with language: zh-CN
[CoopLocalization] Detected system language: ChineseSimplified
[CoopLocalization] Selected language code: zh-CN
[CoopLocalization] Attempting to load translations from: [路径]
[CoopLocalization] Loaded X translations from [路径]
```

### 3. 如果本地化文件未找到
日志会显示：
```
[CoopLocalization] Translation file not found: [路径], using fallback translations
[CoopLocalization] Localization directory does not exist: [路径]
```

### 4. 手动复制本地化文件
如果自动复制失败，可以手动复制：
1. 从项目根目录的 `Localization` 文件夹
2. 复制到模组DLL所在目录的 `Localization` 子文件夹

### 5. 强制使用中文
如果系统语言检测有问题，可以在代码中强制设置：
```csharp
// 在 CoopLocalization.Initialize() 后添加
CoopLocalization.SetLanguage("zh-CN");
```

## 常见问题

### 问题1：构建后没有本地化文件
**解决方案**：运行 `CopyLocalization.bat` 手动复制文件

### 问题2：系统语言检测错误
**解决方案**：检查 `LocalizationManager.CurrentLanguage` 的值，可能需要调整语言检测逻辑

### 问题3：JSON解析失败
**解决方案**：检查 `zh-CN.json` 文件格式是否正确，确保UTF-8编码

### 问题4：模组加载路径问题
**解决方案**：确保本地化文件与DLL在同一目录下的 `Localization` 子文件夹中

## 验证方法

1. 启动游戏
2. 按 P 键打开模组UI
3. 检查界面文字是否为中文
4. 如果仍为英文，查看控制台日志进行诊断