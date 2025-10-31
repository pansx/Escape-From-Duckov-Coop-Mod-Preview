# Base64文件名编码测试

## 编码示例

### 原始用户ID → 编码后的文件名

1. `Client:c19ee733` → `user_Q2xpZW50OmMxOWVlNzMz_tombstones.json`
2. `Host:9050` → `user_SG9zdDo5MDUw_tombstones.json`
3. `Player<123>` → `user_UGxheWVyPDEyMz4_tombstones.json`
4. `User/Name*` → `user_VXNlci9OYW1lKg_tombstones.json`

### 编码过程

1. **UTF-8编码**：将字符串转换为字节数组
2. **Base64编码**：将字节数组转换为Base64字符串
3. **文件名安全化**：
   - `/` → `_`
   - `+` → `-`
   - 移除填充字符 `=`
4. **添加前缀**：`user_` 前缀标识这是编码后的ID

### 解码过程

1. **检查前缀**：确认是 `user_` 开头的编码ID
2. **恢复Base64字符**：
   - `_` → `/`
   - `-` → `+`
   - 添加必要的 `=` 填充
3. **Base64解码**：转换为字节数组
4. **UTF-8解码**：恢复原始字符串

## 优势

1. **完全可逆**：可以准确恢复原始用户ID
2. **文件名安全**：不包含任何非法字符
3. **唯一性保证**：不同的用户ID绝对不会产生相同的文件名
4. **向后兼容**：可以处理旧格式的文件名

## 测试用例

```csharp
// 测试用例
var testCases = new[]
{
    "Client:c19ee733",
    "Host:9050", 
    "Player<123>",
    "User/Name*",
    "Test?File|Name",
    "Normal_User_123"
};

foreach (var userId in testCases)
{
    var encoded = EncodeUserIdForFileName(userId);
    var decoded = DecodeUserIdFromFileName(encoded + "_tombstones.json");
    
    Debug.Log($"Original: {userId}");
    Debug.Log($"Encoded:  {encoded}");
    Debug.Log($"Decoded:  {decoded}");
    Debug.Log($"Match:    {userId == decoded}");
    Debug.Log("---");
}
```

## 预期结果

所有测试用例都应该满足：`原始ID == 解码后的ID`

这确保了墓碑数据的正确关联和持久化。