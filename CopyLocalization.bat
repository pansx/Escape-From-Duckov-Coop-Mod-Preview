@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo 复制本地化文件到构建输出目录
echo ========================================

:: 设置路径
set "SOURCE_DIR=Localization"
set "OUTPUT_DIR=EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\Localization"

:: 检查源目录
if not exist "%SOURCE_DIR%" (
    echo [错误] 源本地化目录不存在: %SOURCE_DIR%
    pause
    exit /b 1
)

:: 创建输出目录
if not exist "%OUTPUT_DIR%" (
    mkdir "%OUTPUT_DIR%"
    echo [信息] 创建输出目录: %OUTPUT_DIR%
)

:: 复制所有JSON文件
echo [信息] 复制本地化文件...
copy "%SOURCE_DIR%\*.json" "%OUTPUT_DIR%\" /Y

if %ERRORLEVEL% equ 0 (
    echo [成功] 本地化文件复制完成
    
    :: 列出复制的文件
    echo [信息] 已复制的文件:
    dir "%OUTPUT_DIR%\*.json" /B
) else (
    echo [错误] 复制失败
    pause
    exit /b 1
)

echo.
echo 完成！
pause