@echo off
setlocal

set SERVER_EXE=F:\Projects\Builds\BLADEFLASH\Blade Flash.exe
if not exist "%SERVER_EXE%" set SERVER_EXE=%~dp0Builds\Server\FlashBall.exe

if not exist "%SERVER_EXE%" (
    echo [LOI] Khong tim thay file build tai: "F:\Projects\Builds\BLADEFLASH\Blade Flash.exe"
    pause
    exit /b 1
)

echo Dang khoi dong 2 Dedicated Server (Rank + Solo, phuc vu TAT CA bac Rank, Region: asia)...
echo.

start "FlashBall Server - RANK (RankRoom)" cmd /k ""%~dp0RunDedicatedServer.bat" RankRoom 4 10"
timeout /t 2 /nobreak >nul
start "FlashBall Server - SOLO (SoloRoom)" cmd /k ""%~dp0RunDedicatedServer.bat" SoloRoom 2 10"

echo Da mo 2 cua so Dedicated Server:
echo  1. RankRoom (4 nguoi, doi nguoi choi 10s truoc khi dien bot)
echo  2. SoloRoom (2 nguoi, doi nguoi choi 10s truoc khi dien bot)
echo.
echo Bay gio ban va ban be co the mo "Blade Flash.exe" de ghep tran Rank & Solo!
echo (Luu y: Can Build lai "Blade Flash.exe" 1 lan sau khi cap nhat code moi.)
echo Muon dung tat ca server, dong 2 cua so cmd vua mo hoac chay StopDedicatedServers.bat.
pause
