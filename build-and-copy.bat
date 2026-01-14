@echo off
REM Build TypeScript i skopiuj do wwwroot
echo Building TypeScript with Webpack...
call npm run build

if %ERRORLEVEL% EQU 0 (
    echo Build successful, copying to wwwroot...
    xcopy dist\bundle.js* VirtualNanny\wwwroot\dist\ /Y
    echo Done! Files copied to VirtualNanny\wwwroot\dist\
) else (
    echo Build failed!
    exit /b 1
)
