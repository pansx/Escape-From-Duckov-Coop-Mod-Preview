# HTTP 服务器测试脚本 (PowerShell)
# 用于测试 Escape From Duckov Coop Mod 的 HTTP API

$PORT = 9050
$BASE_URL = "http://localhost:$PORT"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  HTTP 服务器测试脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 测试函数
function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [int]$Number,
        [int]$Total
    )
    
    Write-Host "[$Number/$Total] 测试 $Name" -ForegroundColor Yellow
    Write-Host "URL: $Url" -ForegroundColor Gray
    Write-Host ""
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method Get -ErrorAction Stop
        Write-Host "✓ 成功" -ForegroundColor Green
        Write-Host ""
        
        # 格式化输出 JSON
        if ($response -is [string]) {
            Write-Host $response
        } else {
            $response | ConvertTo-Json -Depth 10 | Write-Host
        }
        
        Write-Host ""
        return $true
    }
    catch {
        Write-Host "✗ 失败" -ForegroundColor Red
        Write-Host "错误: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        return $false
    }
}

# 测试 1: 根路径
$success1 = Test-Endpoint -Name "根路径 /" -Url "$BASE_URL/" -Number 1 -Total 4
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host ""

# 测试 2: 状态端点
$success2 = Test-Endpoint -Name "状态端点 /api/status" -Url "$BASE_URL/api/status" -Number 2 -Total 4
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host ""

# 测试 3: 玩家列表
$success3 = Test-Endpoint -Name "玩家列表 /api/players" -Url "$BASE_URL/api/players" -Number 3 -Total 4
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host ""

# 测试 4: 404 错误
Write-Host "[4/4] 测试 404 错误处理" -ForegroundColor Yellow
Write-Host "URL: $BASE_URL/api/notfound" -ForegroundColor Gray
Write-Host ""
try {
    $response = Invoke-RestMethod -Uri "$BASE_URL/api/notfound" -Method Get -ErrorAction Stop
    Write-Host "✓ 收到响应（应该是 404 错误）" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 10 | Write-Host
}
catch {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "✓ 正确返回 404 错误" -ForegroundColor Green
        # 尝试读取错误响应体
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            $reader.Close()
            Write-Host $responseBody
        }
        catch {
            Write-Host "无法读取错误响应体" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "✗ 意外的错误: $($_.Exception.Message)" -ForegroundColor Red
    }
}
Write-Host ""
Write-Host "----------------------------------------" -ForegroundColor Gray
Write-Host ""

# 总结
Write-Host "测试完成！" -ForegroundColor Cyan
Write-Host ""

$successCount = @($success1, $success2, $success3) | Where-Object { $_ -eq $true } | Measure-Object | Select-Object -ExpandProperty Count
$totalTests = 3

if ($successCount -eq $totalTests) {
    Write-Host "✓ 所有测试通过 ($successCount/$totalTests)" -ForegroundColor Green
}
elseif ($successCount -eq 0) {
    Write-Host "✗ 所有测试失败 (0/$totalTests)" -ForegroundColor Red
    Write-Host ""
    Write-Host "可能的原因：" -ForegroundColor Yellow
    Write-Host "1. 游戏未启动" -ForegroundColor Gray
    Write-Host "2. 联机服务未开启（需要在游戏中按 = 键并启动主机/客户端）" -ForegroundColor Gray
    Write-Host "3. 端口 $PORT 被占用或被防火墙阻止" -ForegroundColor Gray
}
else {
    Write-Host "⚠ 部分测试通过 ($successCount/$totalTests)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "提示：" -ForegroundColor Cyan
Write-Host "- 可以在浏览器中访问: $BASE_URL/" -ForegroundColor Gray
Write-Host "- 使用 Ctrl+C 可以随时中断脚本" -ForegroundColor Gray
Write-Host ""

# 询问是否在浏览器中打开
$openBrowser = Read-Host "是否在浏览器中打开 HTTP 服务器？(Y/N)"
if ($openBrowser -eq "Y" -or $openBrowser -eq "y") {
    Start-Process $BASE_URL
    Write-Host "已在默认浏览器中打开" -ForegroundColor Green
}

Write-Host ""
Write-Host "按任意键退出..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
