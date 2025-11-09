@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo 获取并分析游戏日志
echo ========================================
echo.

set "AUTH_TOKEN=Bearer eyJhbGciOiJIUzM4NCJ9.eyJyb2xlIjoiQURNSU4iLCJ1c2VySWQiOjEsInN1YiI6ImFkbWluIiwiaWF0IjoxNzYyNTQyMzQ0LCJleHAiOjE3NjI2Mjg3NDR9.I5jmhmr1GzkNXkwaHdAw4pmcvfTvdKDwhBfE8QF8I-6RO3NaSDAbx_qjT4aRDlfI"

echo [1/4] 获取主机端日志 (clientId=1)...
curl -s -X GET "http://127.0.0.1:8080/api/logs?clientId=1&page=0&size=500" ^
  -H "Accept: application/json" ^
  -H "Authorization: %AUTH_TOKEN%" ^
  -H "Cache-Control: no-cache" ^
  -o "logs_host.json"

if %errorlevel% equ 0 (
    echo ✓ 主机端日志已保存
) else (
    echo ✗ 获取主机端日志失败
    goto :end
)

echo.
echo [2/4] 获取客户端日志 (clientId=2)...
curl -s -X GET "http://127.0.0.1:8080/api/logs?clientId=2&page=0&size=500" ^
  -H "Accept: application/json" ^
  -H "Authorization: %AUTH_TOKEN%" ^
  -H "Cache-Control: no-cache" ^
  -o "logs_client.json"

if %errorlevel% equ 0 (
    echo ✓ 客户端日志已保存
) else (
    echo ✗ 获取客户端日志失败
    goto :end
)

echo.
echo [3/4] 转换主机端日志为文本格式...
powershell -NoProfile -Command "$logs = Get-Content logs_host.json | ConvertFrom-Json; $logs | ForEach-Object { $_.content } | Out-File -FilePath logs_host.txt -Encoding UTF8"
if %errorlevel% equ 0 (
    echo ✓ 已保存到 logs_host.txt
) else (
    echo ✗ 转换失败
)

echo.
echo [4/4] 转换客户端日志为文本格式...
powershell -NoProfile -Command "$logs = Get-Content logs_client.json | ConvertFrom-Json; $logs | ForEach-Object { $_.content } | Out-File -FilePath logs_client.txt -Encoding UTF8"
if %errorlevel% equ 0 (
    echo ✓ 已保存到 logs_client.txt
) else (
    echo ✗ 转换失败
)

echo.
echo [分析] 搜索关键日志...
echo ----------------------------------------
powershell -NoProfile -Command "$logs = Get-Content logs_host.json | ConvertFrom-Json; Write-Host '主机端日志总数:' $logs.Count -ForegroundColor Cyan; $setId = $logs | Where-Object { $_.content -like '*SetId*' -or $_.content -like '*setId*' }; if ($setId) { Write-Host '找到 SetId 日志:' -ForegroundColor Green; $setId | ForEach-Object { Write-Host $_.content } } else { Write-Host '未找到 SetId 日志' -ForegroundColor Yellow }; $json = $logs | Where-Object { $_.content -like '*[JSON]*' }; if ($json) { Write-Host '`nJSON 消息 (最新3条):' -ForegroundColor Green; $json | Select-Object -First 3 | ForEach-Object { Write-Host $_.content } }"

echo.
powershell -NoProfile -Command "$logs = Get-Content logs_client.json | ConvertFrom-Json; Write-Host '客户端日志总数:' $logs.Count -ForegroundColor Cyan; $setId = $logs | Where-Object { $_.content -like '*SetId*' -or $_.content -like '*setId*' }; if ($setId) { Write-Host '找到 SetId 日志:' -ForegroundColor Green; $setId | ForEach-Object { Write-Host $_.content } } else { Write-Host '未找到 SetId 日志' -ForegroundColor Yellow }; $json = $logs | Where-Object { $_.content -like '*[JSON]*' }; if ($json) { Write-Host '`nJSON 消息 (最新3条):' -ForegroundColor Green; $json | Select-Object -First 3 | ForEach-Object { Write-Host $_.content } }"

:end
echo.
echo ========================================
echo 完成！日志文件已保存：
echo   - logs_host.txt    (主机端 - 文本格式)
echo   - logs_client.txt  (客户端 - 文本格式)
echo   - logs_host.json   (主机端 - JSON格式)
echo   - logs_client.json (客户端 - JSON格式)
echo ========================================
echo.

endlocal
