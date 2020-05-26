@echo off

echo "Current path:"
cd

echo "Starting Windows pakcage restore"

dotnet restore %~dp0..\src\Liftr.Common.sln -v minimal || goto :error

REM https://github.com/domaindrivendev/Swashbuckle.AspNetCore#swashbuckleaspnetcorecli
cd %~dp0..\src\Samples\Liftr.Sample.Web
dotnet tool restore

echo "Finished Windows pakcage restore successfully"
goto :EOF

:error
echo Failed with error #%errorlevel%
exit /b %errorlevel%