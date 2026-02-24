echo off
set current_dir=%cd%
echo %current_dir%
pushd
echo ### Cleaning RStein.TOML (release) ###
dotnet clean --configuration release
echo ### Building RStein.TOML (release) ###
dotnet build --configuration release
IF NOT %errorlevel%==0 GOTO error
echo ### Running internal tests ###
dotnet test --configuration release --no-build
echo:
popd
set current_dir=%cd%
echo %current_dir%

echo ### Running external encoder/decoder tests (.NET 9, TOML 1.1)###
toml-test-v2.1.0-windows-amd64.exe  test -decoder "RStein.Toml.ExternalTest.Decoder\bin\Release\net9.0\RStein.Toml.ExternalTest.Decoder.exe Toml11" -encoder "RStein.Toml.ExternalTest.Encoder\bin\Release\net9.0\RStein.Toml.ExternalTest.Encoder.exe" -toml 1.1 -skip valid/key/quoted-unicode,valid/string/quoted-unicode,valid/multibyte,valid/string/multibyte,valid/string/multibyte-escape,
IF NOT %errorlevel%==0 GOTO error

echo:
echo ### Running external encoder/decoder tests (.NET 10, TOML 1.1)###
toml-test-v2.1.0-windows-amd64.exe  test -decoder "RStein.Toml.ExternalTest.Decoder\bin\Release\net9.0\RStein.Toml.ExternalTest.Decoder.exe Toml11" -encoder "RStein.Toml.ExternalTest.Encoder\bin\Release\net10.0\RStein.Toml.ExternalTest.Encoder.exe" -toml 1.1 -skip valid/key/quoted-unicode,valid/string/quoted-unicode,valid/multibyte,valid/string/multibyte,valid/string/multibyte-escape,
IF NOT %errorlevel%==0 GOTO error

echo ### Running external encoder/decoder tests (.NET 9, TOML 1.0)###
toml-test-v2.1.0-windows-amd64.exe  test -decoder "RStein.Toml.ExternalTest.Decoder\bin\Release\net9.0\RStein.Toml.ExternalTest.Decoder.exe Toml10" -encoder "RStein.Toml.ExternalTest.Encoder\bin\Release\net9.0\RStein.Toml.ExternalTest.Encoder.exe" -toml 1.0 -skip valid/key/quoted-unicode,valid/string/quoted-unicode,valid/multibyte,valid/string/multibyte,valid/string/multibyte-escape,
IF NOT %errorlevel%==0 GOTO error

echo:
echo ### Running external encoder/decoder tests (.NET 10, TOML 1.0)###
toml-test-v2.1.0-windows-amd64.exe  test -decoder "RStein.Toml.ExternalTest.Decoder\bin\Release\net9.0\RStein.Toml.ExternalTest.Decoder.exe Toml10" -encoder "RStein.Toml.ExternalTest.Encoder\bin\Release\net10.0\RStein.Toml.ExternalTest.Encoder.exe" -toml 1.0 -skip valid/key/quoted-unicode,valid/string/quoted-unicode,valid/multibyte,valid/string/multibyte,valid/string/multibyte-escape,
IF NOT %errorlevel%==0 GOTO error
exit /B 0

:error
echo:
echo !!!!!! Build failed: ErrorLevel: %errorlevel%!!!!!!.
exit /B %errorlevel%