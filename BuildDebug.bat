@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul

echo ========================================
echo 调试编译脚本 - Debug Build Script
echo ========================================
echo.

REM 设置环境变量
if "%DUCKOV_GAME_DIRECTORY%"=="" (
    set "DUCKOV_GAME_DIRECTORY=C:\SteamLibrary\steamapps\common\Escape from Duckov"
    echo [自动设置] 游戏路径: %DUCKOV_GAME_DIRECTORY%
    echo [Auto-set] Game path: %DUCKOV_GAME_DIRECTORY%
    echo.
)

REM 显示编译信息
echo [配置] 编译配置: Debug
echo [Config] Build configuration: Debug
echo [路径] 输出路径: EscapeFromDuckovCoopMod\bin\Debug\netstandard2.1\
echo [Path] Output path: EscapeFromDuckovCoopMod\bin\Debug\netstandard2.1\
echo.

REM 编译Debug版本
echo [编译] 开始Debug编译...
echo [Build] Starting Debug build...
echo.

dotnet build EscapeFromDuckovCoopMod.sln --configuration Debug --verbosity detailed

if !errorLevel! equ 0 (
    echo.
    echo ========================================
    echo [成功] Debug编译完成！
    echo [Success] Debug build completed!
    echo ========================================
    echo.
    
    if exist "EscapeFromDuckovCoopMod\bin\Debug\netstandard2.1\EscapeFromDuckovCoopMod.dll" (
        echo [输出] Debug文件已生成:
        echo [Output] Debug files generated:
        echo   - EscapeFromDuckovCoopMod.dll
        echo   - EscapeFromDuckovCoopMod.pdb (包含调试符号)
        echo   - EscapeFromDuckovCoopMod.pdb (contains debug symbols)
        echo.
        
        for %%F in ("EscapeFromDuckovCoopMod\bin\Debug\netstandard2.1\EscapeFromDuckovCoopMod.dll") do (
            set size=%%~zF
            set /a sizeKB=!size!/1024
            echo [信息] Debug DLL大小: !sizeKB! KB
            echo [Info] Debug DLL size: !sizeKB! KB
        )
    )
) else (
    echo.
    echo ========================================
    echo [失败] Debug编译失败！
    echo [Failed] Debug build failed!
    echo ========================================
)

echo.
pause
endlocal