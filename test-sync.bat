@echo off
REM ========================================
REM MoneyMind WiFi Sync - Test Script
REM ========================================

SETLOCAL EnableDelayedExpansion

SET ADB=C:\Users\rober\AppData\Local\Android\Sdk\platform-tools\adb.exe

echo.
echo ========================================
echo   MoneyMind WiFi Sync Test Utility
echo ========================================
echo.

:MENU
echo Scegli un'opzione:
echo.
echo [1] Verifica Dispositivi Connessi
echo [2] Setup Port Forwarding (per Emulatore)
echo [3] Avvia App Mobile
echo [4] Verifica Log App Mobile
echo [5] Test Connessione Server Mobile
echo [6] Screenshot Emulatore
echo [7] Installa APK su Dispositivo
echo [8] Test Completo Desktop -^> Mobile
echo [9] Pulisci Dati App Mobile
echo [0] Esci
echo.

SET /P CHOICE="Scelta: "

IF "%CHOICE%"=="1" GOTO CHECK_DEVICES
IF "%CHOICE%"=="2" GOTO SETUP_PORT_FORWARD
IF "%CHOICE%"=="3" GOTO START_APP
IF "%CHOICE%"=="4" GOTO VIEW_LOGS
IF "%CHOICE%"=="5" GOTO TEST_CONNECTION
IF "%CHOICE%"=="6" GOTO SCREENSHOT
IF "%CHOICE%"=="7" GOTO INSTALL_APK
IF "%CHOICE%"=="8" GOTO FULL_TEST
IF "%CHOICE%"=="9" GOTO CLEAR_DATA
IF "%CHOICE%"=="0" GOTO EXIT

GOTO MENU

REM ========================================
REM [1] Verifica Dispositivi Connessi
REM ========================================
:CHECK_DEVICES
echo.
echo Dispositivi Android connessi:
echo ----------------------------
"%ADB%" devices
echo.
pause
GOTO MENU

REM ========================================
REM [2] Setup Port Forwarding
REM ========================================
:SETUP_PORT_FORWARD
echo.
echo Configurazione Port Forwarding...
echo -----------------------------------
"%ADB%" forward tcp:8765 tcp:8765
IF %ERRORLEVEL% EQU 0 (
    echo.
    echo [OK] Port forwarding configurato: localhost:8765 -^> emulator:8765
    echo.
    echo Ora puoi connettere il Desktop a: localhost:8765
) ELSE (
    echo.
    echo [ERRORE] Impossibile configurare port forwarding
    echo Verifica che l'emulatore sia in esecuzione.
)
echo.
pause
GOTO MENU

REM ========================================
REM [3] Avvia App Mobile
REM ========================================
:START_APP
echo.
echo Avvio MoneyMindApp...
echo ---------------------
"%ADB%" shell am start -n com.moneymind.app/crc64399a401da9d21b0a.MainActivity
IF %ERRORLEVEL% EQU 0 (
    echo.
    echo [OK] App avviata con successo!
    echo.
    echo MANUALE: Vai su Menu -^> Impostazioni -^> Sincronizzazione WiFi
    echo          e premi "Avvia Server"
) ELSE (
    echo.
    echo [ERRORE] Impossibile avviare l'app
    echo Verifica che l'app sia installata (Opzione 7)
)
echo.
pause
GOTO MENU

REM ========================================
REM [4] Verifica Log App Mobile
REM ========================================
:VIEW_LOGS
echo.
echo Log App Mobile (ultimi 50 eventi):
echo ----------------------------------
echo.
"%ADB%" logcat -d -s "MoneyMindApp:V" "MoneyMindApp:D" "MoneyMindApp:I" "MoneyMindApp:W" "MoneyMindApp:E" | findstr /v /c:"---" | more
echo.
echo [Premi un tasto per continuare]
pause >nul
GOTO MENU

REM ========================================
REM [5] Test Connessione Server Mobile
REM ========================================
:TEST_CONNECTION
echo.
echo Test Connessione Server Mobile...
echo ----------------------------------
echo.
echo Verifico se il server WiFi è in ascolto sulla porta 8765...
echo.

REM Usa curl se disponibile
WHERE curl >nul 2>nul
IF %ERRORLEVEL% EQU 0 (
    curl -s --connect-timeout 3 http://localhost:8765/ping
    IF !ERRORLEVEL! EQU 0 (
        echo.
        echo.
        echo [OK] Server mobile raggiungibile!
        echo Puoi procedere con la sincronizzazione.
    ) ELSE (
        echo.
        echo [ERRORE] Server mobile NON raggiungibile
        echo.
        echo Verifica:
        echo 1. App mobile avviata
        echo 2. Server avviato nell'app (Menu -^> Sync WiFi -^> Avvia Server^)
        echo 3. Port forwarding configurato (Opzione 2^)
    )
) ELSE (
    echo [INFO] curl non disponibile, uso netstat...
    echo.
    netstat -an | findstr ":8765"
    IF !ERRORLEVEL! EQU 0 (
        echo.
        echo [OK] Porta 8765 in ascolto
    ) ELSE (
        echo.
        echo [WARN] Porta 8765 non in ascolto
        echo Assicurati che il server mobile sia avviato.
    )
)
echo.
pause
GOTO MENU

REM ========================================
REM [6] Screenshot Emulatore
REM ========================================
:SCREENSHOT
echo.
echo Cattura Screenshot...
echo ---------------------
SET SCREENSHOT_FILE=screenshot_%date:~6,4%%date:~3,2%%date:~0,2%_%time:~0,2%%time:~3,2%%time:~6,2%.png
SET SCREENSHOT_FILE=%SCREENSHOT_FILE: =0%
"%ADB%" shell screencap -p /sdcard/screenshot.png
"%ADB%" pull /sdcard/screenshot.png "%SCREENSHOT_FILE%"
IF %ERRORLEVEL% EQU 0 (
    echo.
    echo [OK] Screenshot salvato: %SCREENSHOT_FILE%
    echo Apro immagine...
    start "" "%SCREENSHOT_FILE%"
) ELSE (
    echo.
    echo [ERRORE] Impossibile catturare screenshot
)
echo.
pause
GOTO MENU

REM ========================================
REM [7] Installa APK su Dispositivo
REM ========================================
:INSTALL_APK
echo.
echo Installazione APK...
echo --------------------
echo.
echo Cerco APK compilato in bin\Release\net8.0-android\...
echo.

SET APK_PATH=bin\Release\net8.0-android\com.moneymind.app-Signed.apk
IF NOT EXIST "%APK_PATH%" (
    echo [WARN] APK non trovato in: %APK_PATH%
    echo.
    echo Provo percorso alternativo...
    SET APK_PATH=bin\Release\net8.0-android\*.apk
)

IF EXIST "%APK_PATH%" (
    echo [OK] Trovato: %APK_PATH%
    echo.
    echo Installazione in corso...
    "%ADB%" install -r "%APK_PATH%"
    IF !ERRORLEVEL! EQU 0 (
        echo.
        echo [OK] App installata con successo!
    ) ELSE (
        echo.
        echo [ERRORE] Installazione fallita
    )
) ELSE (
    echo [ERRORE] APK non trovato!
    echo.
    echo Compila prima l'app:
    echo   cd C:\Users\rober\Documents\MoneyMindApp
    echo   dotnet build -c Release -f net8.0-android
)
echo.
pause
GOTO MENU

REM ========================================
REM [8] Test Completo Desktop -^> Mobile
REM ========================================
:FULL_TEST
echo.
echo ========================================
echo   TEST COMPLETO: Desktop -^> Mobile
echo ========================================
echo.

REM Step 1: Verifica dispositivo
echo [Step 1/5] Verifica dispositivo...
"%ADB%" devices | findstr "device$" >nul
IF %ERRORLEVEL% NEQ 0 (
    echo [ERRORE] Nessun dispositivo connesso!
    pause
    GOTO MENU
)
echo [OK] Dispositivo connesso

REM Step 2: Port forwarding
echo.
echo [Step 2/5] Setup port forwarding...
"%ADB%" forward tcp:8765 tcp:8765 >nul
echo [OK] Port forwarding configurato

REM Step 3: Avvia app
echo.
echo [Step 3/5] Avvio app mobile...
"%ADB%" shell am start -n com.moneymind.app/crc64399a401da9d21b0a.MainActivity >nul 2>&1
timeout /t 3 /nobreak >nul
echo [OK] App avviata

REM Step 4: Prompt manuale
echo.
echo [Step 4/5] AZIONE MANUALE RICHIESTA:
echo -----------------------------------
echo.
echo 1. Sull'EMULATORE:
echo    - Apri Menu -^> Impostazioni -^> Sincronizzazione WiFi
echo    - Premi "Avvia Server"
echo    - Verifica che mostri "Server attivo"
echo.
echo 2. Sul DESKTOP:
echo    - Apri MoneyMind Desktop
echo    - Menu -^> Impostazioni -^> Sincronizzazione WiFi
echo    - IP: localhost  Porta: 8765
echo    - Premi "Test Connessione"
echo    - Se OK, scegli:
echo      * Direzione: Desktop -^> Mobile
echo      * Modalita: SOSTITUISCI (o UNISCI se dati gia presenti^)
echo    - Premi "Sincronizza"
echo.
echo [Premi un tasto quando hai completato la sincronizzazione]
pause >nul

REM Step 5: Verifica log
echo.
echo [Step 5/5] Verifica log sincronizzazione...
echo.
"%ADB%" logcat -d -s "MoneyMindApp:I" | findstr /i "sync" | more
echo.
echo ========================================
echo   Test Completato!
echo ========================================
echo.
echo Verifica visivamente sull'app mobile:
echo - Dashboard: Saldo aggiornato?
echo - Transazioni: Visibili?
echo.
pause
GOTO MENU

REM ========================================
REM [9] Pulisci Dati App Mobile
REM ========================================
:CLEAR_DATA
echo.
echo Pulizia Dati App...
echo -------------------
echo.
echo [ATTENZIONE] Questa operazione cancellera tutti i dati dell'app!
echo.
SET /P CONFIRM="Sei sicuro? (S/N): "
IF /I "%CONFIRM%"=="S" (
    "%ADB%" shell pm clear com.moneymind.app
    IF !ERRORLEVEL! EQU 0 (
        echo.
        echo [OK] Dati app cancellati
        echo L'app e ora come appena installata.
    ) ELSE (
        echo.
        echo [ERRORE] Impossibile cancellare dati
    )
) ELSE (
    echo.
    echo Operazione annullata.
)
echo.
pause
GOTO MENU

REM ========================================
REM EXIT
REM ========================================
:EXIT
echo.
echo Chiusura...
echo.
exit /b 0
