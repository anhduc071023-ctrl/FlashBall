@echo off
setlocal

REM Huong dan su dung:
REM Chay Dedicated Server cho 1 phong (Rank hoac Solo, phuc vu MOI bac Rank):
REM   RunDedicatedServer.bat [SessionName] [PlayerCount] [FallbackDelaySeconds]
REM Vi du:
REM   RunDedicatedServer.bat RankRoom 4 10
REM   RunDedicatedServer.bat SoloRoom 2 10

set SERVER_EXE=F:\Projects\Builds\BLADEFLASH\Blade Flash.exe
if not exist "%SERVER_EXE%" set SERVER_EXE=%~dp0Builds\Server\FlashBall.exe

set SESSION_NAME=%~1
if "%SESSION_NAME%"=="" set SESSION_NAME=RankRoom

set PLAYER_COUNT=%~2
if "%PLAYER_COUNT%"=="" set PLAYER_COUNT=4

set FALLBACK_DELAY=%~3
if "%FALLBACK_DELAY%"=="" set FALLBACK_DELAY=10

if not exist "%SERVER_EXE%" (
    echo [LOI] Khong tim thay file game tai:
    echo   "F:\Projects\Builds\BLADEFLASH\Blade Flash.exe"
    echo   "%~dp0Builds\Server\FlashBall.exe"
    echo Hay kiem tra lai duong dan ban build PC trong file .bat nay.
    pause
    exit /b 1
)

set LOG_FILE=%~dp0dedicated_server_%SESSION_NAME%.log

echo ========================================================
echo  KHOI CHAY DEDICATED SERVER - FLASHBALL (Region: asia)
echo ========================================================
echo  Executable        : %SERVER_EXE%
echo  SessionName       : %SESSION_NAME%
echo  PlayerCount       : %PLAYER_COUNT%
echo  FallbackStartDelay: %FALLBACK_DELAY%s
echo  LogFile           : %LOG_FILE%
echo ========================================================
echo  (Giu cua so nay mo khi dang choi. Nhan Ctrl+C de tat.)
echo.

"%SERVER_EXE%" -batchmode -nographics -logfile "%LOG_FILE%" -sessionName "%SESSION_NAME%" -playerCount %PLAYER_COUNT% -fallbackStartDelay %FALLBACK_DELAY%
