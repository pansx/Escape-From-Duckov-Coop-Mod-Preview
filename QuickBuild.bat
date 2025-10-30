@echo off
chcp 65001 >nul

echo 快速编译 - Quick Build
echo.

REM 设置环境变量（如果未设置）
if "%DUCKOV_GAME_DIRECTORY%"=="" (
    set "DUCKOV_GAME_DIRECTORY=C:\SteamLibrary\steamapps\common\Escape from Duckov"
    echo [自动设置] 游戏路径: %DUCKOV_GAME_DIRECTORY%
)

REM 快速编译
dotnet build EscapeFromDuckovCoopMod.sln --configuration Release --verbosity quiet

if %errorLevel% equ 0 (
    echo [成功] 编译完成！
    echo [Success] Build completed!
) else (
    echo [失败] 编译失败！
    echo [Failed] Build failed!
    pause
)