@echo off
:: Fallout build bootstrapper (Windows). Runs the build project directly.
::   build.cmd Generate
dotnet run --project "%~dp0build\_build.csproj" -- %*
