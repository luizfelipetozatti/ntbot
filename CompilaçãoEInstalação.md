# Guia para Compilação e Implantação do NTBot

Este guia descreve os passos necessários para compilar o projeto NTBot e implantá-lo no NinjaTrader.

## Pré-requisitos

1. Visual Studio 2019 ou superior (Community, Professional ou Enterprise)
2. .NET Framework 4.7.2 ou superior instalado
3. NinjaTrader 8 instalado

## Compilando o Projeto

### Opção 1: Usando o Visual Studio

1. Abra o arquivo de solução `NTBotSolution.sln` no Visual Studio
2. Selecione a configuração "Debug" no menu suspenso da barra de ferramentas
3. Clique com o botão direito no projeto "NTBot" no Solution Explorer
4. Selecione "Build" para compilar o projeto

### Opção 2: Usando o Developer Command Prompt

1. Abra o "Developer Command Prompt for VS" no menu Iniciar
2. Navegue até o diretório do projeto:
   ```
   cd /d D:\Felipe\Projetos\ntbot
   ```
3. Execute o comando MSBuild:
   ```
   msbuild NTBotSolution.sln /t:Build /p:Configuration=Debug
   ```

## Configurando o Post-Build Event

Para que os arquivos compilados sejam automaticamente copiados para a pasta do NinjaTrader após cada compilação, siga estes passos:

1. Abra o arquivo de projeto `NTBot.csproj` no Visual Studio ou em um editor de texto
2. Localize a seção `<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />` próxima ao final do arquivo
3. Substitua essa seção pelo código abaixo:

```xml
<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
<!-- Executa após a compilação para copiar os arquivos para a pasta do NinjaTrader -->
<PropertyGroup>
  <NinjaTraderCustomPath>$(USERPROFILE)\Documents\NinjaTrader 8\bin\Custom</NinjaTraderCustomPath>
</PropertyGroup>
<Target Name="AfterBuild">
  <Message Text="Copiando arquivos para o NinjaTrader em: $(NinjaTraderCustomPath)" Importance="high" />
  <Copy SourceFiles="$(TargetDir)$(TargetFileName)" DestinationFolder="$(NinjaTraderCustomPath)" />
  <Copy SourceFiles="$(TargetDir)$(TargetName).pdb" DestinationFolder="$(NinjaTraderCustomPath)" />
</Target>
```

4. Salve o arquivo e recompile o projeto

## Implantação Manual

Se a configuração do post-build não funcionar, você pode copiar manualmente os arquivos:

1. Navegue até a pasta de saída da compilação:
   ```
   D:\Felipe\Projetos\ntbot\NTBot\bin\Debug
   ```
2. Copie os arquivos `NTBot.dll` e `NTBot.pdb`
3. Cole-os na pasta Custom do NinjaTrader:
   ```
   %USERPROFILE%\Documents\NinjaTrader 8\bin\Custom
   ```
   (Onde %USERPROFILE% é geralmente C:\Users\SeuNomeDeUsuário)

## Verificação da Instalação

1. Inicie o NinjaTrader 8
2. No menu "New", verifique se o item "Trading Bot" está presente
3. Clique nesse item para abrir a janela do Trading Bot

## Solução de Problemas

Se você encontrar problemas durante a compilação ou implantação:

1. Verifique se o .NET Framework correto está instalado
2. Verifique se todas as referências do projeto estão corretas
3. Certifique-se de que o NinjaTrader não está em execução durante a cópia dos arquivos
4. Verifique se o caminho da pasta Custom do NinjaTrader está correto para o seu sistema
