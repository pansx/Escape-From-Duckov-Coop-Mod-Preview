@echo off
setlocal EnableDelayedExpansion
chcp 65001 >nul

echo ========================================
echo 逃离鸭科夫联机模组编译脚本
echo Escape From Duckov Coop Mod Build Script
echo ========================================
echo.

REM 设置固定的游戏路径
set "DUCKOV_GAME_DIRECTORY=C:\SteamLibrary\steamapps\common\Escape from Duckov"

REM 验证游戏路径
if not exist "%DUCKOV_GAME_DIRECTORY%" (
    echo [错误] 游戏目录不存在: %DUCKOV_GAME_DIRECTORY%
    echo [Error] Game directory does not exist: %DUCKOV_GAME_DIRECTORY%
    echo.
    echo [提示] 请修改Build.bat中的DUCKOV_GAME_DIRECTORY路径
    echo [Tip] Please modify DUCKOV_GAME_DIRECTORY path in Build.bat
    echo.
    pause
    exit /b 1
)

if not exist "%DUCKOV_GAME_DIRECTORY%\Duckov_Data\Managed" (
    echo [错误] 游戏Managed目录不存在: %DUCKOV_GAME_DIRECTORY%\Duckov_Data\Managed
    echo [Error] Game Managed directory does not exist: %DUCKOV_GAME_DIRECTORY%\Duckov_Data\Managed
    echo.
    pause
    exit /b 1
)

echo [信息] 游戏路径: %DUCKOV_GAME_DIRECTORY%
echo [Info] Game path: %DUCKOV_GAME_DIRECTORY%
echo.

REM 检查依赖文件
echo [检查] 验证依赖文件...
echo [Check] Verifying dependency files...

if not exist "Shared\0Harmony.dll" (
    echo [错误] 缺少依赖文件: Shared\0Harmony.dll
    echo [Error] Missing dependency: Shared\0Harmony.dll
    pause
    exit /b 1
)

if not exist "Shared\LiteNetLib.dll" (
    echo [错误] 缺少依赖文件: Shared\LiteNetLib.dll
    echo [Error] Missing dependency: Shared\LiteNetLib.dll
    pause
    exit /b 1
)

echo [成功] 所有依赖文件存在
echo [Success] All dependency files exist
echo.

REM 清理旧的编译输出
echo [清理] 清理旧的编译输出...
echo [Clean] Cleaning old build output...
if exist "EscapeFromDuckovCoopMod\bin" (
    rmdir /s /q "EscapeFromDuckovCoopMod\bin"
)
if exist "EscapeFromDuckovCoopMod\obj" (
    rmdir /s /q "EscapeFromDuckovCoopMod\obj"
)

REM 开始编译
echo [编译] 开始编译项目...
echo [Build] Starting project compilation...
echo.

dotnet build EscapeFromDuckovCoopMod.sln --configuration Release --verbosity normal

if !errorLevel! equ 0 (
    echo.
    echo ========================================
    echo [成功] 编译完成！
    echo [Success] Build completed!
    echo ========================================
    echo.
    
    REM 显示输出文件信息
    if exist "EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll" (
        echo [输出] 编译输出文件:
        echo [Output] Build output files:
        echo   - EscapeFromDuckovCoopMod.dll
        echo   - EscapeFromDuckovCoopMod.pdb
        echo.
        
        REM 获取文件大小
        for %%F in ("EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll") do (
            set size=%%~zF
            set /a sizeKB=!size!/1024
            echo [信息] DLL文件大小: !sizeKB! KB
            echo [Info] DLL file size: !sizeKB! KB
        )
        echo.
        
        REM 检查是否有模组目录环境变量，如果有则自动复制
        if not "%DUCKOV_MODS_DIRECTORY%"=="" (
            if exist "%DUCKOV_MODS_DIRECTORY%" (
                echo [复制] 自动复制到模组目录...
                echo [Copy] Auto-copying to mods directory...
                
                if not exist "%DUCKOV_MODS_DIRECTORY%\EscapeFromDuckovCoopMod" (
                    mkdir "%DUCKOV_MODS_DIRECTORY%\EscapeFromDuckovCoopMod"
                )
                
                copy "EscapeFromDuckovCoopMod\bin\Release\netstandard2.1\EscapeFromDuckovCoopMod.dll" "%DUCKOV_MODS_DIRECTORY%\EscapeFromDuckovCoopMod\" >nul
                
                if !errorLevel! equ 0 (
                    echo [成功] 已复制到: %DUCKOV_MODS_DIRECTORY%\EscapeFromDuckovCoopMod\
                    echo [Success] Copied to: %DUCKOV_MODS_DIRECTORY%\EscapeFromDuckovCoopMod\
                ) else (
                    echo [警告] 复制到模组目录失败
                    echo [Warning] Failed to copy to mods directory
                )
                echo.
            )
        )
        
        echo [提示] 编译成功！可以启动游戏测试模组了。
        echo [Tip] Build successful! You can now start the game to test the mod.
    ) else (
        echo [警告] 编译成功但未找到输出文件
        echo [Warning] Build succeeded but output file not found
    )
) else (
    echo.
    echo ========================================
    echo [失败] 编译失败！
    echo [Failed] Build failed!
    echo ========================================
    echo.
    echo [提示] 请检查上面的错误信息并修复问题
    echo [Tip] Please check the error messages above and fix the issues
)

echo.
endlocal