# 🤖 NTBot - NT Bot para NinjaTrader 8

[![.NET Framework](https://img.shields.io/badge/.NET-Framework%204.8.1-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net481)
[![NinjaTrader](https://img.shields.io/badge/NinjaTrader-8-orange.svg)](https://ninjatrader.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

## 📝 Descrição

**NTBot** é um Add-On avançado para NinjaTrader 8 que permite a execução automatizada de estratégias de trading através de uma interface gráfica intuitiva. O bot oferece uma arquitetura modular e extensível para implementação de estratégias personalizadas com suporte a análise técnica em tempo real.

## ✨ Funcionalidades Principais

- 🎯 **Execução Automatizada**: Sistema de trading automatizado com controle de posições
- 📊 **Análise Técnica**: Suporte a múltiplos indicadores técnicos (SMA, EMA, RSI, MACD)
- 🔄 **Estratégias Modulares**: Arquitetura extensível para estratégias personalizadas
- 💻 **Interface Gráfica**: GUI integrada ao NinjaTrader com controles intuitivos
- ⚡ **Processamento em Tempo Real**: Análise de dados de mercado com baixa latência
- 🛡️ **Gerenciamento de Risco**: Stop loss e take profit automatizados
- 📈 **Multi-timeframe**: Suporte a múltiplos timeframes simultaneamente

## 🏗️ Arquitetura do Projeto

```text
NTBot/
├── 📁 Core/                     # Núcleo do sistema
│   └── TradingCore.cs          # Classes base e interfaces
├── 📁 Strategies/              # Estratégias de trading
│   └── TradingStrategies.cs    # Implementações de estratégias
├── 📄 NTBot.cs            # Add-On principal do NinjaTrader
├── 📄 NTBotPage.xaml      # Interface gráfica (XAML)
└── 📄 NTBotPage.xaml.cs   # Code-behind da interface
```

## 🔧 Pré-requisitos

### Software Obrigatório

- 🛠️ **Windows 10/11** (64-bit)
- 🛠️ **NinjaTrader 8** (versão mais recente)
- 🛠️ **.NET Framework 4.8.1** ou superior
- 🛠️ **Visual Studio 2019/2022** ou **MSBuild Tools**

### Dependências do NinjaTrader

O projeto utiliza as seguintes bibliotecas do NinjaTrader:

- `NinjaTrader.Core.dll`
- `NinjaTrader.Gui.dll`
- `NinjaTrader.Cbi.dll`
- `NinjaTrader.Data.dll`

### Configuração do Ambiente

1. **Instalar NinjaTrader 8**

   ```text
   Download: https://ninjatrader.com/GetStarted
   ```

2. **Verificar .NET Framework**

   ```powershell
   # PowerShell: Verificar versão instalada
   Get-ItemProperty "HKLM:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\" -Name Release
   ```

3. **Visual Studio Build Tools** (se não tiver VS completo)

   ```text
   Download: https://visualstudio.microsoft.com/pt-br/downloads/#build-tools-for-visual-studio-2022
   ```

## 🚀 Instalação

### Build Manual

1. **Compile o projeto**

   ```powershell
   # Usando MSBuild
   msbuild "NTBot\NTBot.csproj" /t:Rebuild /p:Configuration=Release
   
   # Ou usando dotnet (se disponível)
   dotnet build NTBot\NTBot.csproj --configuration Release
   ```

2. **Copie os arquivos para NinjaTrader**

   ```powershell
   # Localizar pasta Custom do NinjaTrader
   $ntPath = Join-Path $env:USERPROFILE "Documents\NinjaTrader 8\bin\Custom"
   
   # Copiar DLL e PDB
   Copy-Item "NTBot\bin\Release\NTBot.dll" $ntPath
   Copy-Item "NTBot\bin\Release\NTBot.pdb" $ntPath
   ```

3. **Reinicie o NinjaTrader**

## 💡 Como Usar

### 1. Ativação do Add-On

1. Abra o **NinjaTrader 8**
2. Vá em **New → NT Bot** no Control Center
3. A interface do bot será aberta em nova janela

### 2. Configuração Básica

```csharp
// Exemplo de configuração de estratégia
var strategy = new MovingAverageStrategy();
strategy.FastPeriod = 10;     // Média móvel rápida
strategy.SlowPeriod = 20;     // Média móvel lenta
strategy.StopLoss = 10;       // Stop loss em ticks
strategy.TakeProfit = 20;     // Take profit em ticks
```

### 3. Estratégias Disponíveis

- 📈 **Moving Average Crossover**: Cruzamento de médias móveis
- 📊 **RSI Divergence**: Estratégia baseada em divergência do RSI
- 🎯 **Bollinger Bands**: Operações com bandas de Bollinger
- ⚡ **Scalping Strategy**: Estratégia de scalping intraday

## 🔧 Desenvolvimento

### Estrutura de Classes Principais

```csharp
// Interface base para estratégias
public interface ITradingStrategy
{
    StrategyOutput ProcessData(StrategyInput input);
    void ProcessBar(BarData bar);
    void Reset();
}

// Entrada de dados
public class StrategyInput
{
    public double LastPrice { get; set; }
    public DateTime Time { get; set; }
    public long Volume { get; set; }
    public double Bid { get; set; }
    public double Ask { get; set; }
}

// Sinais de trading
public enum TradeSignal
{
    None, Buy, Sell, Exit
}
```

### Criando Nova Estratégia

```csharp
public class MinhaEstrategia : TradingStrategy
{
    public override StrategyOutput ProcessData(StrategyInput input)
    {
        // Implementar lógica da estratégia
        return new StrategyOutput
        {
            Signal = TradeSignal.Buy,
            EntryPrice = input.LastPrice,
            StopLossPrice = input.LastPrice - 10,
            TakeProfitPrice = input.LastPrice + 20
        };
    }
}
```

## 🐛 Troubleshooting

### Problemas Comuns

| Problema | Solução |
|----------|---------|
| 🚫 **DLL não carrega** | Verificar se .NET Framework 4.8.1 está instalado |
| 🔄 **AddOn não aparece** | Reiniciar NinjaTrader após instalação |
| ⚠️ **Erro de compilação** | Verificar referências do NinjaTrader no projeto |
| 📁 **Pasta Custom não encontrada** | Verificar instalação do NinjaTrader 8 |

### Debug

```powershell
# Verificar logs do NinjaTrader
Get-Content "$env:USERPROFILE\Documents\NinjaTrader 8\trace\*.log" | Select-String "NTBot"
```

## 📁 Estrutura de Arquivos

```text
ntbot/
├── 📁 .vscode/                 # Configurações VS Code
│   ├── build_and_copy.ps1     # Script de build automatizado
│   └── tasks.json             # Tasks do VS Code
├── 📁 NTBot/                  # Projeto principal
│   ├── 📁 Core/               # Classes do núcleo
│   ├── 📁 Strategies/         # Estratégias de trading
│   ├── 📁 Properties/         # Propriedades do assembly
│   └── 📄 *.cs, *.xaml       # Código fonte
├── 📁 NinjaTraderAddOnProject/ # Projeto exemplo
└── 📄 *.sln, *.csproj         # Arquivos de solução
```

## 🤝 Contribuição

1. Faça um fork do projeto
2. Crie uma branch para sua feature (`git checkout -b feature/nova-estrategia`)
3. Commit suas mudanças (`git commit -am 'Adiciona nova estratégia'`)
4. Push para a branch (`git push origin feature/nova-estrategia`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

## 🔗 Links Úteis

- 📚 [Documentação NinjaTrader 8](https://ninjatrader.com/support/helpGuides/nt8/)
- 🛠️ [NinjaScript Reference](https://ninjatrader.com/support/helpGuides/nt8/en-us/ninjascript.html)
- 💬 [Fórum NinjaTrader](https://ninjatrader.com/support/forum/)
- 📊 [Estratégias de Trading](https://ninjatrader.com/support/helpGuides/nt8/en-us/strategies.html)

---

**⚠️ Aviso Legal**: Este software é fornecido apenas para fins educacionais. Trading automático envolve riscos financeiros significativos. Use por sua conta e risco.
