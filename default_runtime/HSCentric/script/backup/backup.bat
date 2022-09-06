@echo off
cd /d %~dp0
if "%%1" == "" (
    echo null str
	)else (

	rem 删除备份配置
	rd "..\..\BackupData" /s /q
	
	rem 备份配置
	echo d | xcopy "%~f1\client.config" "..\..\BackupData" /i /y
	echo d | xcopy "%~f1\BepInEx\config" "..\..\BackupData\BepInEx\config" /i /y
	)