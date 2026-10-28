@echo off
echo ========================================================
echo Deploying M.J. Billones Web Application to GitHub Pages
echo ========================================================
echo.

cd /d "%~dp0"
git branch -M gh-pages
echo Pushing branch gh-pages to origin...
git push -u origin gh-pages --force

echo.
if %ERRORLEVEL% EQU 0 (
    echo ========================================================
    echo Deployment pushed successfully!
    echo.
    echo Your live website link:
    echo https://hazellealagasca.github.io/TransportDeliveryManagementSystem-or-MJBillones-TDMS/
    echo ========================================================
) else (
    echo.
    echo If GitHub prompted for login or personal access token,
    echo please authenticate and retry.
)
pause
