@echo off

REM Check for unstaged changes and commit them if they exist
git status --porcelain | findstr . >nul
if not errorlevel 1 (
    echo Committing unstaged changes before pulling...
    git add Assets/Scripts/*.cs
    git add -u Assets/Scripts/
    git commit -m "Auto-commit unstaged changes before pull"
)

REM Pull from GitHub to sync with remote
git pull origin main --rebase
if errorlevel 1 (
    echo Pull failed - resolve conflicts manually.
    pause
    exit /b
)

REM Stage new changes (additions, modifications, deletions)
git add Assets/Scripts/*.cs
git add -u Assets/Scripts/

REM Check if there are changes to commit
git status --porcelain | findstr . >nul
if errorlevel 1 (
    echo No new changes to commit.
    pause
    exit /b
)

REM Prompt for commit message
set /p COMMIT_MSG="Enter commit message (or press Enter for default 'Sync scripts on %date% %time%'): "
if "%COMMIT_MSG%"=="" set COMMIT_MSG=Sync scripts on %date% %time%

REM Commit and push
git commit -m "%COMMIT_MSG%"
git push origin main
if errorlevel 1 (
    echo Push failed! Check your connection or credentials.
    pause
    exit /b
) else (
    echo Scripts pushed to GitHub successfully!
    pause
)