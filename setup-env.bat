@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo 逃离鸭科夫联机模组 - 环境设置
echo ========================================
echo.

:: 读取.env文件
if exist ".env" (
    echo 正在读取 .env 配置...
    for /f "usebackq tokens=1,2 delims==" %%a in (".env") do (
        set "line=%%a"
        if not "!line:~0,1!"=="#" if not "!line!"=="" (
            echo 设置环境变量: %%a=%%b
            setx "%%a" "%%b" >nul
        )
    )
    echo.
    echo 环境变量设置完成！
    echo 请重启 Visual Studio 或命令行窗口以使环境变量生效。
) else (
    echo 错误：未找到 .env 文件
    echo 请确保 .env 文件存在于当前目录
)

echo.
pause