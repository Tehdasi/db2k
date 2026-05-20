..\bin\Debug\net10.0\db2k.exe -t 01_spinning_cube\test.glsl
if errorlevel 1 echo passed
if errorlevel 0 echo failed


..\bin\Debug\net10.0\db2k.exe -t 12_failing\test.glsl
if errorlevel 1 echo passed
if errorlevel 0 echo failed

