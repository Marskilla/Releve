@echo off
setlocal

cd /d "%~dp0..\src\Releve.Game" || (
echo ERREUR : impossible d'acceder au dossier du projet.
pause
exit /b 1
)

set "PUBLISH_DIR=bin\Release\net8.0\win-x64\publish"

echo Suppression de l'ancien dossier publish...

if exist "%PUBLISH_DIR%" (
rmdir /s /q "%PUBLISH_DIR%"
)

if exist "%PUBLISH_DIR%" (
echo ERREUR : impossible de supprimer le dossier publish.
pause
exit /b 1
)

echo Nettoyage de la compilation precedente...
dotnet clean -c Release -r win-x64
if errorlevel 1 (
    echo ERREUR : le nettoyage a echoue.
    pause
    exit /b 1
)

echo Publication et recompilation...
dotnet publish -c Release -r win-x64 --self-contained
if errorlevel 1 (
    echo ERREUR : la publication a echoue.
    pause
    exit /b 1
)

echo.
echo Publication terminee avec succes.
pause