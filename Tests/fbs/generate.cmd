@ECHO OFF

SET packageDir=%USERPROFILE%\.nuget\packages
SET flatc=%packageDir%\kdsoft.flatbuffers\1.12.0\tools\flatc.exe

ECHO Generating FlatBuffer code ...
%flatc% --csharp Test.LineItemKey.fbs Test.LineItem.fbs 