@echo off

echo "Current path:"
cd

if NOT EXIST %~dp0..\buildtools\reportgenerator (
    echo "Install reportgenerator"
    dotnet tool install dotnet-reportgenerator-globaltool --tool-path %~dp0..\buildtools
)

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