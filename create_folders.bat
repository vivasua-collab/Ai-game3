@echo off
echo ============================================
echo   Creating Unity project folder structure
echo ============================================
echo.

set PROJECT_ROOT=c:\!Unity\Ai-game3\UnityProject\Assets

echo Checking path: %PROJECT_ROOT%
if not exist "%PROJECT_ROOT%" (
    echo ERROR: Project folder not found!
    echo Check path: %PROJECT_ROOT%
    pause
    exit /b 1
)

echo.
echo Creating folders...
echo.

mkdir "%PROJECT_ROOT%\Data\CultivationLevels"
mkdir "%PROJECT_ROOT%\Data\Elements"
mkdir "%PROJECT_ROOT%\Data\Materials"
mkdir "%PROJECT_ROOT%\Data\Techniques"
mkdir "%PROJECT_ROOT%\Data\Items"
mkdir "%PROJECT_ROOT%\Data\NPCPresets"
mkdir "%PROJECT_ROOT%\Data\Species"
mkdir "%PROJECT_ROOT%\Prefabs"
mkdir "%PROJECT_ROOT%\Scenes"
mkdir "%PROJECT_ROOT%\Resources\UI"
mkdir "%PROJECT_ROOT%\Resources\Icons"
mkdir "%PROJECT_ROOT%\Art\Characters"
mkdir "%PROJECT_ROOT%\Art\Items"
mkdir "%PROJECT_ROOT%\Art\Effects"
mkdir "%PROJECT_ROOT%\Audio\Music"
mkdir "%PROJECT_ROOT%\Audio\SFX"

echo.
echo ============================================
echo   Folder structure created!
echo ============================================
echo.
pause
