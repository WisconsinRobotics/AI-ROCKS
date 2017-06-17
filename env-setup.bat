:: Windows dependency script for AI-ROCKS
@ECHO OFF

:: Find MsBuild
for /f "usebackq tokens=*" %%i in (`.\tools\vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
  set msbuild=%%i
)

if exist "%msbuild%\MSBuild\15.0\Bin\MSBuild.exe" (
  set msbuild="%msbuild%\MSBuild\15.0\Bin"
) else (
  echo Failed to find MSBuild!
  goto :eof
)

:: /f or clean as cmd args?
IF /I [%1]==[] (
    goto MAYBE_CLONE_DEP
) ELSE ( 
    goto ARG_PARSE
)

:ARG_PARSE
    IF /i "%1"=="clean" goto CLEAN
    IF /i "%1"=="/f" goto CLONE_DEP
    goto :eof

:CLEAN
    IF EXIST ".\ObstacleLibrary" ( rd /s /q ObstacleLibrary)
    del .\*.dll .\*.pdb
    goto :eof

:: If no switches specified, go down this path
:MAYBE_CLONE_DEP
    IF EXIST ".\ObstacleLibrary" (
      echo ObstacleLibrary already exists, will not override. Use /f switch to override.
      cd ObstacleLibrary 
      git pull
      cd ..
    ) ELSE (
      git clone https://github.com/WisconsinRobotics/ObstacleLibrary
    )

    goto BUILD_DEP

:: Force clean issued
:CLONE_DEP
    IF EXIST ".\ObstacleLibrary" ( rd /s /q ObstacleLibrary )
    git clone https://github.com/WisconsinRobotics/ObstacleLibrary
    goto BUILD_DEP

:: Actually build the dependencies
:BUILD_DEP
    :: Ensure repo is up to date
    git pull
    echo ==== Building Dependencies ====
    del .\*.dll .\*.pdb
    pushd .
    
    :: ObstacleLibrary C# 
    pushd .
    cd ObstacleLibrary\csharp
    call %msbuild%\msbuild.exe ObstacleLibrary.sln /t:Rebuild /p:Configuration=Debug /p:Platform=x64 /m
    cp ObstacleLibrarySharp\ObstacleLibrarySharp\bin\Debug\ObstacleLibrarySharp.dll ..\.. 
    cp ObstacleLibrarySharp\ObstacleLibrarySharp\bin\Debug\ObstacleLibrarySharp.pdb ..\..
    popd
    
    echo ==== Complete! ====
    popd
