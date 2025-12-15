@echo off
pushd "%~dp0\.."
rd /s /q "DS4Windows\bin"
popd

TIMEOUT /T 2
