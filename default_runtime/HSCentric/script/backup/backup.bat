@echo off
cd /d %~dp0

rem 检查是否传入了参数
if "%~1" == "" (
    echo null str
) else (
    rem 删除旧的备份数据
    if exist "..\..\BackupData" (
        rd /s /q "..\..\BackupData"
    )

    rem 备份配置
    echo 备份配置文件到 BackupData

    rem 备份 client.config 文件
    if exist "%~f1\client.config" (
        xcopy "%~f1\client.config" "..\..\BackupData" /i /y >nul
    ) else (
        echo client.config 文件不存在！
    )

    rem 备份 BepInEx 配置文件夹
    if exist "%~f1\BepInEx\config" (
        xcopy "%~f1\BepInEx\config" "..\..\BackupData\BepInEx\config" /i /y >nul
    ) else (
        echo BepInEx\config 文件夹不存在！
    )
)
