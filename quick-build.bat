@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo 逃离鸭科夫联机模组 - 快速构建
echo ========================================

:: 设置环境变量
set "DUCKOV_GAME_DIRECTORY=C:\SteamLibrary\steamapps\common\Escape from Duckov"

:: 快速构建
echo 正在构建...
dotnet build EscapeFromDuckovCoopMod.sln --configuration Release --verbosity quiet --nologo

if %ERRORLEVEL% neq 0 (
    echo 构建失败！
    pause
    exit /b 1
)

:: 显示结果
set "OUTPUT_FILE=EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll"
if exist "%OUTPUT_FILE%" (
    echo 构建成功！
    echo 输出文件: %OUTPUT_FILE%
    
    :: 获取文件大小
    for %%F in ("%OUTPUT_FILE%") do (
        set "filesize=%%~zF"
        set /a "filesizeKB=!filesize!/1024"
        echo 文件大小: !filesizeKB! KB
    )
) else (
    echo 未找到输出文件
)

echo.
echo 完成！
pause