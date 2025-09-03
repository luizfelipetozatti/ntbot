@echo off
echo Copiando arquivos NTBot para o NinjaTrader...

set SOURCE_DIR=D:\Felipe\Projetos\ntbot\NTBot\bin\Debug
set TARGET_DIR=%USERPROFILE%\Documents\NinjaTrader 8\bin\Custom

if not exist "%SOURCE_DIR%\NTBot.dll" (
    echo Erro: Arquivo NTBot.dll não encontrado em %SOURCE_DIR%
    goto :error
)

if not exist "%TARGET_DIR%" (
    echo Criando diretório %TARGET_DIR%...
    mkdir "%TARGET_DIR%"
)

echo Copiando NTBot.dll para %TARGET_DIR%...
copy /Y "%SOURCE_DIR%\NTBot.dll" "%TARGET_DIR%"

if exist "%SOURCE_DIR%\NTBot.pdb" (
    echo Copiando NTBot.pdb para %TARGET_DIR%...
    copy /Y "%SOURCE_DIR%\NTBot.pdb" "%TARGET_DIR%"
)

echo.
echo Cópia concluída com sucesso!
goto :end

:error
echo.
echo Falha na cópia dos arquivos.
exit /b 1

:end
echo.
echo Pressione qualquer tecla para sair...
pause > nul
