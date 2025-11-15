@echo off
chcp 65001 >nul
echo ========================================
echo   HTTP 服务器测试脚本
echo ========================================
echo.

set PORT=9050
set BASE_URL=http://localhost:%PORT%

echo [1/4] 测试根路径 /
echo URL: %BASE_URL%/
echo.
curl -s %BASE_URL%/ -o test_root.html
if %errorlevel% equ 0 (
    echo ✓ 成功 - 已保存到 test_root.html
) else (
    echo ✗ 失败 - 无法连接到服务器
)
echo.
echo ----------------------------------------
echo.

echo [2/4] 测试状态端点 /api/status
echo URL: %BASE_URL%/api/status
echo.
curl -s %BASE_URL%/api/status
echo.
echo.
echo ----------------------------------------
echo.

echo [3/4] 测试玩家列表 /api/players
echo URL: %BASE_URL%/api/players
echo.
curl -s %BASE_URL%/api/players
echo.
echo.
echo ----------------------------------------
echo.

echo [4/4] 测试 404 错误
echo URL: %BASE_URL%/api/notfound
echo.
curl -s %BASE_URL%/api/notfound
echo.
echo.
echo ----------------------------------------
echo.

echo 测试完成！
echo.
echo 提示：
echo - 如果看到 "无法连接" 错误，请确保游戏已启动并开启了联机服务
echo - 根路径的 HTML 内容已保存到 test_root.html，可以用浏览器打开查看
echo - 也可以直接在浏览器中访问: %BASE_URL%/
echo.
pause
