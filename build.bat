@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo ========================================
echo 逃离鸭科夫联机模组 - 构建脚本
echo Escape From Duckov Coop Mod - Build Script
echo ========================================
echo.

:: 设置颜色
for /F %%a in ('echo prompt $E ^| cmd') do set "ESC=%%a"
set "GREEN=%ESC%[32m"
set "RED=%ESC%[31m"
set "YELLOW=%ESC%[33m"
set "BLUE=%ESC%[34m"
set "RESET=%ESC%[0m"

:: 读取.env文件中的配置
if exist ".env" (
    echo %BLUE%[信息]%RESET% 正在读取 .env 配置文件...
    for /f "usebackq tokens=1,2 delims==" %%a in (".env") do (
        set "line=%%a"
        if not "!line:~0,1!"=="#" if not "!line!"=="" (
            set "%%a=%%b"
        )
    )
) else (
    echo %YELLOW%[警告]%RESET% 未找到 .env 文件，使用默认配置
    set "DUCKOV_GAME_DIRECTORY=C:\SteamLibrary\steamapps\common\Escape from Duckov"
    set "BUILD_CONFIGURATION=Release"
)

:: 设置环境变量
set "DUCKOV_GAME_DIRECTORY=%DUCKOV_GAME_DIRECTORY%"
echo %BLUE%[信息]%RESET% 游戏目录: %DUCKOV_GAME_DIRECTORY%

:: 验证游戏目录
if not exist "%DUCKOV_GAME_DIRECTORY%" (
    echo %RED%[错误]%RESET% 游戏目录不存在: %DUCKOV_GAME_DIRECTORY%
    echo 请检查 .env 文件中的 DUCKOV_GAME_DIRECTORY 配置
    exit /b 1
)

:: 验证游戏Managed目录
set "DUCKOV_GAME_MANAGED=%DUCKOV_GAME_DIRECTORY%\Duckov_Data\Managed"
if not exist "%DUCKOV_GAME_MANAGED%" (
    echo %RED%[错误]%RESET% 游戏Managed目录不存在: %DUCKOV_GAME_MANAGED%
    echo 请确保游戏已正确安装
    exit /b 1
)

:: 验证依赖文件
echo %BLUE%[信息]%RESET% 检查依赖文件...
if not exist "Shared\0Harmony.dll" (
    echo %RED%[错误]%RESET% 缺少依赖文件: Shared\0Harmony.dll
    exit /b 1
)
if not exist "Shared\LiteNetLib.dll" (
    echo %RED%[错误]%RESET% 缺少依赖文件: Shared\LiteNetLib.dll
    exit /b 1
)

:: 清理旧的构建输出
echo %BLUE%[信息]%RESET% 清理旧的构建输出...
if exist "EscapeFromDuckovCoopMod\bin" rmdir /s /q "EscapeFromDuckovCoopMod\bin"
if exist "EscapeFromDuckovCoopMod\obj" rmdir /s /q "EscapeFromDuckovCoopMod\obj"

:: 开始构建
echo %BLUE%[信息]%RESET% 开始构建项目...
echo 配置: %BUILD_CONFIGURATION%
echo.

dotnet build EscapeFromDuckovCoopMod.sln --configuration %BUILD_CONFIGURATION% --verbosity minimal

if %ERRORLEVEL% neq 0 (
    echo.
    echo %RED%[错误]%RESET% 构建失败！
    echo 请检查上面的错误信息
    exit /b 1
)

:: 构建成功
echo.
echo %GREEN%[成功]%RESET% 构建完成！

:: 复制本地化文件
echo %BLUE%[信息]%RESET% 复制本地化文件...
set "OUTPUT_DIR=EscapeFromDuckovCoopMod\bin\%BUILD_CONFIGURATION%\netstandard2.1"
set "LOCALIZATION_OUTPUT_DIR=%OUTPUT_DIR%\Localization"

if exist "Localization" (
    if not exist "%LOCALIZATION_OUTPUT_DIR%" mkdir "%LOCALIZATION_OUTPUT_DIR%"
    copy "Localization\*.json" "%LOCALIZATION_OUTPUT_DIR%\" /Y >nul
    if !ERRORLEVEL! equ 0 (
        echo %GREEN%[成功]%RESET% 本地化文件复制完成
        
        :: 列出复制的文件
        for %%f in ("%LOCALIZATION_OUTPUT_DIR%\*.json") do (
            echo %BLUE%[信息]%RESET% 复制了本地化文件: %%~nxf
        )
    ) else (
        echo %YELLOW%[警告]%RESET% 本地化文件复制失败
    )
) else (
    echo %YELLOW%[警告]%RESET% 未找到本地化目录: Localization
)

:: 显示输出文件信息
if exist "%OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll" (
    echo %GREEN%[成功]%RESET% 输出文件: %OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll
    
    :: 获取文件大小
    for %%F in ("%OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll") do (
        set "filesize=%%~zF"
        set /a "filesizeKB=!filesize!/1024"
        echo %BLUE%[信息]%RESET% 文件大小: !filesizeKB! KB
    )
    
    :: 获取文件修改时间
    for %%F in ("%OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll") do (
        echo %BLUE%[信息]%RESET% 构建时间: %%~tF
    )
) else (
    echo %RED%[错误]%RESET% 未找到输出文件: %OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll
)

:: 可选：复制到模组目录
if defined DUCKOV_MODS_DIRECTORY (
    if exist "%DUCKOV_MODS_DIRECTORY%" (
        echo %BLUE%[信息]%RESET% 复制到模组目录: %DUCKOV_MODS_DIRECTORY%
        
        :: 创建模组子目录
        set "MOD_SUBFOLDER=%DUCKOV_MODS_DIRECTORY%\EscapeFromDuckovCoopMod"
        if not exist "!MOD_SUBFOLDER!" mkdir "!MOD_SUBFOLDER!"
        
        :: 复制 DLL
        copy "%OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll" "!MOD_SUBFOLDER%\" >nul
        if !ERRORLEVEL! equ 0 (
            echo %GREEN%[成功]%RESET% 已复制 DLL 到模组目录
        ) else (
            echo %YELLOW%[警告]%RESET% 复制 DLL 到模组目录失败
        )
        
        :: 复制本地化文件
        if exist "%OUTPUT_DIR%\Localization" (
            if not exist "!MOD_SUBFOLDER!\Localization" mkdir "!MOD_SUBFOLDER!\Localization"
            xcopy "%OUTPUT_DIR%\Localization\*.*" "!MOD_SUBFOLDER!\Localization\" /Y /Q >nul
            if !ERRORLEVEL! equ 0 (
                echo %GREEN%[成功]%RESET% 已复制本地化文件到模组目录
            ) else (
                echo %YELLOW%[警告]%RESET% 复制本地化文件失败
            )
        ) else (
            echo %YELLOW%[警告]%RESET% 未找到本地化文件目录: %OUTPUT_DIR%\Localization
        )
    )
) else (
    echo %BLUE%[信息]%RESET% 手动复制指南:
    echo   1. 复制 %OUTPUT_DIR%\EscapeFromDuckovCoopMod.dll
    echo   2. 复制 %OUTPUT_DIR%\Localization\ 文件夹
    echo   到游戏的模组目录中
)

echo.
echo %GREEN%构建完成！%RESET%