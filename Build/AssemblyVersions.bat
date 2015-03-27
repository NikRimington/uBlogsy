@echo off
REM Ensure version number is given
IF .%1 == . (
	@echo off
	set /p VERSION="Enter version (eg. 1.0.2): " %=%
	@echo on
)
IF NOT .%1 == . (
	set VERSION=%1
)

REM ***********************************
REM Set version number for assemblies.
REM ***********************************
..\Tools\AssemblyInfoUtil\AssemblyInfoUtil.exe -set:%VERSION%.* "..\Source\uBlogsy.BusinessLogic\Properties\AssemblyInfo.cs
..\Tools\AssemblyInfoUtil\AssemblyInfoUtil.exe -set:%VERSION%.* "..\Source\uBlogsy.Common\Properties\AssemblyInfo.cs
..\Tools\AssemblyInfoUtil\AssemblyInfoUtil.exe -set:%VERSION%.* "..\Source\uBlogsy.Mvc.Parts\Properties\AssemblyInfo.cs
..\Tools\AssemblyInfoUtil\AssemblyInfoUtil.exe -set:%VERSION%.* "..\Source\uBlogsy.Web\Properties\AssemblyInfo.cs


exit /b
