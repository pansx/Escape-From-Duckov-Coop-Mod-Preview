# 默认IP地址修改总结

## 修改内容

为了方便调试，已将默认IP地址从 `127.0.0.1` 修改为 `r.pansx.net`。

### 修改的文件：

1. **ModUI.cs**
   - 位置：`EscapeFromDuckovCoopMod/Main/UI/ModUI.cs`
   - 修改：`private string _manualIP = "127.0.0.1";` → `private string _manualIP = "r.pansx.net";`

2. **NetService.cs**
   - 位置：`EscapeFromDuckovCoopMod/Main/NetService.cs`
   - 修改：`public string manualIP = "127.0.0.1";` → `public string manualIP = "r.pansx.net";`

## 效果

- **UI界面**：打开模组UI时，手动连接的IP输入框将默认显示 `r.pansx.net`
- **网络服务**：NetService的默认IP也设置为 `r.pansx.net`
- **调试便利**：不再需要每次手动输入IP地址，直接点击连接即可

## 注意事项

1. **端口保持不变**：默认端口仍为 `9050`
2. **可以修改**：用户仍可在UI中手动修改IP地址
3. **重新编译**：修改后需要重新编译模组才能生效

## 恢复方法

如果需要恢复为本地调试，可以将IP地址改回：
- `r.pansx.net` → `127.0.0.1`

## 其他相关设置

- 默认端口：`9050`
- 广播间隔：`5秒`
- 同步间隔：`0.015秒` (约67fps)
- 重连冷却：`10秒`