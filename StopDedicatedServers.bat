@echo off
echo Dang tat tat ca tien trinh Dedicated Server dang chay nen...
powershell -NoProfile -Command "Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like '*-batchmode*' -and ($_.Name -eq 'Blade Flash.exe' -or $_.Name -eq 'FlashBall.exe') } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force; Write-Host ('Da tat PID ' + $_.ProcessId) }"
echo Hoan tat!
pause
