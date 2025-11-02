@echo off
chcp 65001 >nul
echo ========================================
echo 逃离鸭科夫联机模组 - 调试脚本
echo Escape From Duckov Coop Mod - Debug Script
echo ========================================
echo.

echo [信息] 正在终止游戏进程...
taskkill /f /im "Duckov.exe" 2>nul
if %errorlevel% equ 0 (
    echo [成功] 游戏进程已终止
) else (
    echo [信息] 游戏进程未运行或已终止
)

echo.
echo [信息] 等待进程完全退出...
timeout /t 2 /nobreak >nul

echo [信息] 开始构建模组...
call build.bat

REM 检查构建输出文件是否存在来判断构建是否成功
if exist "EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll" (
    echo [成功] 模组构建完成！
) else (
    echo [错误] 构建失败！找不到输出文件
    exit /b 1
)

echo.
echo [信息] 构建完成，正在启动游戏...


start "" steam://rungameid/3167020

echo.
echo [成功] 调试流程完成！
echo [提示] 游戏正在启动，请等待加载完成
echo ========================================