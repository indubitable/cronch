@echo off
cd /d %~dp0
set RUN_SYSTEM_TESTS=true
dotnet test %*
