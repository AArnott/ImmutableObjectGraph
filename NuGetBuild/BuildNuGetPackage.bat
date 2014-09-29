@echo off

REM Ensure that we're running in the location of this batch file otherwise the relative paths won't work
PUSHD "%~dp0" 

ECHO.

IF NOT EXIST ..\ImmutableObjectGraph\bin\Release\ImmutableObjectGraph.dll (
	ECHO Could not locate the ImmutableObjectGraph.dll - you must build the solution in Release configuration before trying to generate the NuGet package
	ECHO.
	PAUSE
	GOTO :eof
)

IF NOT EXIST lib MD lib
IF NOT EXIST templates MD templates

COPY ..\ImmutableObjectGraph\bin\Release\ImmutableObjectGraph.dll lib > nul
COPY ..\ImmutableObjectGraph\*.tt templates > nul

..\.nuget\nuget pack ImmutableObjectGraph.1.0.0.nuspec

ECHO.
PAUSE