# TeamSoda.MiniLocalizor 模块分析

## 概述

MiniLocalizor 是一个轻量级的本地化系统，负责游戏的多语言支持，基于 CSV 文件格式进行本地化数据管理。

## 核心组件

### ILocalizationProvider (本地化提供者接口)

- **功能**：定义本地化数据访问的统一接口

- **方法**：`Get(string key)` - 根据键获取本地化文本

- **设计**：接口隔离原则，只定义必要的访问方法

### CSVFileLocalizor (CSV 文件本地化器)

- **功能**：基于 CSV 文件的本地化实现

- **特性**：- 支持 Unity 的 SystemLanguage 枚举

  - 自动从文件名推断语言类型

  - 内存字典缓存提高访问性能

  - 支持转义字符处理

#### 关键功能

- **文件路径管理**：- 默认路径: `StreamingAssets/Localization/{Language}.csv`

  - 支持自定义路径

- **字典构建**：- 使用 MiniExcelLibs 读取 CSV 文件

  - 内存缓存所有本地化条目

  - 异常处理和错误日志

- **文本处理**：- `ConvertFromEscapes()`: 转义字符解码

  - `ConvertToEscapes()`: 转义字符编码

### DataEntry (数据条目)

- **功能**：本地化数据的基本单元

- **属性**：- `key`: 本地化键值

  - `value`: 本地化文本内容

  - `version`: 版本信息

  - `sheet`: 工作表信息

#### 版本管理

- **版本比较**：`IsNewerThan(string version)` 方法

- **版本格式**：支持数字版本和 `#` 前缀的特殊版本

- **版本优先级**：`#` 前缀版本优先于普通数字版本

## 技术实现

### 文件格式

- **CSV 格式**：使用标准 CSV 格式存储本地化数据

- **编码支持**：支持 UTF-8 编码

- **Excel 兼容**：可以使用 Excel 等工具编辑

### 依赖库

- **MiniExcelLibs**：用于 CSV 文件读取

- **System.Text.RegularExpressions**：用于转义字符处理

- **Unity Engine**：集成 Unity 的语言系统

### 性能优化

- **内存缓存**：一次性加载所有本地化数据到内存

- **字典查找**：O(1) 时间复杂度的键值查找

- **延迟加载**：仅在需要时构建字典

## 错误处理

### 文件不存在处理

- 自动创建空文件

- 记录警告日志

- 优雅降级处理

### 读取异常处理

- 捕获文件访问异常

- 提供用户友好的错误信息

- 建议关闭外部编辑软件

### 数据验证

- 检查键值是否为空

- 过滤无效数据条目

- 防止重复键值覆盖

## 使用模式

### 初始化方式

```csharp
// 通过语言类型初始化
var localizor = new CSVFileLocalizor(SystemLanguage.Chinese);

// 通过文件路径初始化
var localizor = new CSVFileLocalizor("path/to/localization.csv");

```

### 文本获取

```csharp
string localizedText = localizor.Get("UI_MainMenu_Title");

```

### 键值检查

```csharp
bool hasKey = localizor.HasKey("UI_Settings_Audio");

```

## 设计优势

### 简单易用

- 最小化的 API 设计

- 直观的键值对访问

- 标准的 CSV 格式

### 扩展性

- 接口设计便于替换实现

- 支持多种初始化方式

- 版本管理支持增量更新

### 性能友好

- 内存缓存减少 I/O 操作

- 高效的字典查找

- 一次性加载策略

## 开发建议

### 本地化键值命名

- 使用层次化命名: `UI_Menu_Button_Start`

- 保持一致的命名规范

- 避免特殊字符和空格

### 文件管理

- 为每种语言创建独立的 CSV 文件

- 使用版本控制管理本地化文件

- 定期备份本地化数据

### 性能考虑

- 避免频繁重建字典

- 合理使用缓存机制

- 监控内存使用情况
