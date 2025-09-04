using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NTBot.Strategies;
using NTBot.Core;

namespace NTBot
{
    /// <summary>
    /// Lógica de interação para NTBotPage.xaml
    /// </summary>
    public partial class NTBotPage : NTTabPage, NinjaTrader.Gui.Tools.IInstrumentProvider, NinjaTrader.Gui.Tools.IIntervalProvider
    {
        #region Variáveis
        private NinjaTrader.Cbi.Instrument instrument;
        private bool isRunning = false;
        private BarsRequest barsRequest;
        private MarketData marketData;
        private TradingStrategy currentStrategy;
        private RiskManager riskManager;
        private DispatcherTimer updateTimer;
    #endregion

        public NTBotPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("NTBotPage: Construtor iniciado");
                // Força o carregamento do XAML, mas qualquer exceção será capturada
                InitializeComponent();
                System.Diagnostics.Debug.WriteLine("NTBotPage: InitializeComponent concluído");

                // Define o nome da aba como o instrumento selecionado
                TabName = "@INSTRUMENT_FULL";

                // Inicializa o gerenciador de risco
                riskManager = new RiskManager();
                System.Diagnostics.Debug.WriteLine("NTBotPage: RiskManager inicializado");

                // Configura o timer para atualizações periódicas da UI
                updateTimer = new DispatcherTimer();
                updateTimer.Interval = TimeSpan.FromSeconds(1);
                updateTimer.Tick += OnUpdateTimerTick;
                System.Diagnostics.Debug.WriteLine("NTBotPage: Timer configurado");

                // Associa handlers de eventos (verifica nulos para evitar NRE caso XAML não carregue algum controle)
                try {
                    System.Diagnostics.Debug.WriteLine("NTBotPage: Tentando registrar eventos");
                    System.Diagnostics.Debug.WriteLine("NTBotPage: instrumentSelector=" + (instrumentSelector != null ? "encontrado" : "NULL"));
                    System.Diagnostics.Debug.WriteLine("NTBotPage: accountSelector=" + (accountSelector != null ? "encontrado" : "NULL"));
                    System.Diagnostics.Debug.WriteLine("NTBotPage: strategyTypeComboBox=" + (strategyTypeComboBox != null ? "encontrado" : "NULL"));
                    System.Diagnostics.Debug.WriteLine("NTBotPage: logTextBox=" + (logTextBox != null ? "encontrado" : "NULL"));
                    System.Diagnostics.Debug.WriteLine("NTBotPage: statusTextBlock=" + (statusTextBlock != null ? "encontrado" : "NULL"));
                    System.Diagnostics.Debug.WriteLine("NTBotPage: currentPositionTextBlock=" + (currentPositionTextBlock != null ? "encontrado" : "NULL"));
                    System.Diagnostics.Debug.WriteLine("NTBotPage: currentPLTextBlock=" + (currentPLTextBlock != null ? "encontrado" : "NULL"));
                    
                    if (instrumentSelector != null) {
                        instrumentSelector.InstrumentChanged += OnInstrumentChanged;
                        System.Diagnostics.Debug.WriteLine("NTBotPage: InstrumentChanged registrado");
                    }

                    // Inscreve-se em atualizações de status de conta (evento estático)
                    Account.AccountStatusUpdate += OnAccountStatusUpdate;
                    System.Diagnostics.Debug.WriteLine("NTBotPage: AccountStatusUpdate registrado");

                    // Configura a seleção padrão de estratégia (verifica existência do combo)
                    if (strategyTypeComboBox != null) {
                        System.Diagnostics.Debug.WriteLine("NTBotPage: Iniciando OnStrategyTypeChanged inicial");
                        OnStrategyTypeChanged(null, null);
                        System.Diagnostics.Debug.WriteLine("NTBotPage: OnStrategyTypeChanged inicial concluído");
                    }
                } 
                catch (Exception e) {
                    System.Diagnostics.Debug.WriteLine("NTBotPage: Erro ao registrar eventos: " + e.Message + "\n" + e.StackTrace);
                }

                // Registra o log inicial
                LogMessage("NT Bot inicializado. Selecione um instrumento e configure sua estratégia.");
                System.Diagnostics.Debug.WriteLine("NTBotPage: Construtor concluído com sucesso");
            }
            catch (Exception ex)
            {
                // Protege e informa erro de inicialização de forma amigável
                var msg = "Erro ao inicializar a página do NT Bot: " + ex.Message;
                System.Diagnostics.Debug.WriteLine(msg + "\n" + ex.StackTrace);
                try
                {
                    MessageBox.Show(msg, "NTBot - Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
                // Re-throw para que o host (NinjaTrader) também veja a exceção, caso deseje interromper a abertura da aba
                throw;
            }
        }

        #region Event Handlers
        
        // Manipulador para mudanças no tipo de estratégia selecionada
        public void OnStrategyTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("OnStrategyTypeChanged: Iniciando alternância de painéis");
                
                // Esconde todos os painéis de estratégia (com verificação null)
                if (movingAveragePanel != null)
                    movingAveragePanel.Visibility = Visibility.Collapsed;
                if (rsiPanel != null)
                    rsiPanel.Visibility = Visibility.Collapsed;
                if (bollingerPanel != null)
                    bollingerPanel.Visibility = Visibility.Collapsed;
                
                // Verifique o combobox e se está selecionado
                if (strategyTypeComboBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("OnStrategyTypeChanged: ERRO - strategyTypeComboBox é null");
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("OnStrategyTypeChanged: ComboBox seleção=" + strategyTypeComboBox.SelectedIndex);
                
                // Mostra o painel correspondente à estratégia selecionada
                if (strategyTypeComboBox.SelectedIndex == 0)
                {
                    if (movingAveragePanel != null)
                        movingAveragePanel.Visibility = Visibility.Visible;
                }
                else if (strategyTypeComboBox.SelectedIndex == 1)
                {
                    if (rsiPanel != null)
                        rsiPanel.Visibility = Visibility.Visible;
                }
                else if (strategyTypeComboBox.SelectedIndex == 2)
                {
                    if (bollingerPanel != null)
                        bollingerPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERRO em OnStrategyTypeChanged: " + ex.Message + "\n" + ex.StackTrace);
            }
        }
        
        // Manipulador para o botão de iniciar o bot
        private void OnStartButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("OnStartButtonClick: Tentando iniciar o bot");
                
                if (isRunning)
                {
                    System.Diagnostics.Debug.WriteLine("OnStartButtonClick: Bot já está rodando, ignorando");
                    return;
                }
                
                // Verifica se os controles essenciais estão disponíveis
                if (accountSelector == null)
                {
                    System.Diagnostics.Debug.WriteLine("OnStartButtonClick: ERRO - accountSelector é null");
                    MessageBox.Show("Erro de interface: Seletor de conta não disponível.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (instrumentSelector == null)
                {
                    System.Diagnostics.Debug.WriteLine("OnStartButtonClick: ERRO - instrumentSelector é null");
                    MessageBox.Show("Erro de interface: Seletor de instrumento não disponível.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (accountSelector.SelectedAccount == null)
                {
                    System.Diagnostics.Debug.WriteLine("OnStartButtonClick: Nenhuma conta selecionada");
                    MessageBox.Show("Por favor, selecione uma conta antes de iniciar o bot.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (instrumentSelector.Instrument == null)
                {
                    System.Diagnostics.Debug.WriteLine("OnStartButtonClick: Nenhum instrumento selecionado");
                    MessageBox.Show("Por favor, selecione um instrumento antes de iniciar o bot.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("OnStartButtonClick: Iniciando o bot com conta e instrumento válidos");
                
                // Inicia o bot
                isRunning = true;
                
                // Atualiza a UI (com verificação null)
                if (startButton != null)
                    startButton.IsEnabled = false;
                if (stopButton != null)
                    stopButton.IsEnabled = true;
                if (strategyTypeComboBox != null)
                    strategyTypeComboBox.IsEnabled = false;
                
                // Cria a estratégia selecionada
                CreateStrategy();
                
                // Configura o gerenciador de risco
                ConfigureRiskManager();
                
                // Inicia a assinatura de dados
                SubscribeToMarketData();
                
                // Inicia o timer de atualização
                if (updateTimer != null)
                {
                    updateTimer.Start();
                    System.Diagnostics.Debug.WriteLine("OnStartButtonClick: Timer iniciado");
                }
                
                // Atualiza o status
                if (statusTextBlock != null)
                    statusTextBlock.Text = "Em execução";
                
                LogMessage("Bot iniciado com estratégia: " + (strategyTypeComboBox != null ? strategyTypeComboBox.Text : "desconhecida"));
                System.Diagnostics.Debug.WriteLine("OnStartButtonClick: Bot iniciado com sucesso");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERRO em OnStartButtonClick: " + ex.Message + "\n" + ex.StackTrace);
                LogMessage("Erro ao iniciar bot: " + ex.Message);
                
                // Restaura estado anterior em caso de erro
                isRunning = false;
                if (startButton != null)
                    startButton.IsEnabled = true;
                if (stopButton != null)
                    stopButton.IsEnabled = false;
                if (strategyTypeComboBox != null)
                    strategyTypeComboBox.IsEnabled = true;
                
                MessageBox.Show("Erro ao iniciar o bot: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            // Inicia o timer de atualização
            updateTimer.Start();
            
            // Atualiza o status
            statusTextBlock.Text = "Em execução";
            
            LogMessage("Bot iniciado com estratégia: " + strategyTypeComboBox.Text);
        }
        
        // Manipulador para o botão de parar o bot
        private void OnStopButtonClick(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
                return;
            
            // Para o bot
            isRunning = false;
            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
            strategyTypeComboBox.IsEnabled = true;
            
            // Cancela assinaturas de dados
            UnsubscribeFromMarketData();
            
            // Para o timer de atualização
            updateTimer.Stop();
            
            // Atualiza o status
            statusTextBlock.Text = "Parado";
            
            LogMessage("Bot parado.");
        }
        
        // Manipulador para o botão de limpar o log
        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            logTextBox.Clear();
            LogMessage("Log limpo.");
        }
        
        // Manipulador para mudanças no instrumento selecionado
        private void OnInstrumentChanged(object sender, EventArgs e)
        {
            instrument = instrumentSelector.Instrument;
            
            if (instrument != null)
            {
                LogMessage("Instrumento alterado para: " + instrument.FullName);
            }
        }
        
        // Manipulador para atualizações de status de conta
        private void OnAccountStatusUpdate(object sender, AccountStatusEventArgs e)
        {
            if (e.Account == accountSelector.SelectedAccount)
            {
                LogMessage("Status da conta atualizado: " + e.Status);
            }
        }
        
        // Manipulador para o timer de atualização
        private void OnUpdateTimerTick(object sender, EventArgs e)
        {
            if (!isRunning || accountSelector.SelectedAccount == null)
                return;
            
            // Atualiza as informações da posição atual
            Account account = accountSelector.SelectedAccount;
            Position position = account.Positions.FirstOrDefault(p => p.Instrument == instrument);
            
            if (position != null)
            {
                currentPositionTextBlock.Text = position.Quantity.ToString();
                // use API template: GetUnrealizedProfitLoss
                currentPLTextBlock.Text = position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, double.MinValue).ToString("C");
            }
            else
            {
                currentPositionTextBlock.Text = "0";
                currentPLTextBlock.Text = "$0.00";
            }
        }
        
        // Manipulador para receber dados de mercado
        private void OnMarketData(object sender, MarketDataEventArgs e)
        {
            if (!isRunning || currentStrategy == null)
                return;
            
            // Envia os dados para a estratégia
            StrategyInput input = new StrategyInput
            {
                LastPrice = e.Last,
                Time = e.Time,
                Volume = e.Volume,
                Bid = e.Bid,
                Ask = e.Ask
            };
            
            StrategyOutput output = currentStrategy.ProcessData(input);
            
            // Processa sinais de trading
            if (output.Signal != TradeSignal.None && autoTradeCheckBox.IsChecked == true)
            {
                // Verifica se o risco está dentro dos limites
                if (riskManager.IsTradeAllowed(output.Signal, instrument, accountSelector.SelectedAccount))
                {
                    ExecuteTrade(output.Signal);
                }
                else
                {
                    LogMessage("Sinal de trade rejeitado pelo gerenciador de risco.");
                }
            }
        }
        
        // Manipulador para receber barras de dados
        private void OnBarsUpdate(object sender, BarsUpdateEventArgs e)
        {
            if (!isRunning || currentStrategy == null)
                return;
            
            // Alimenta barras completas para a estratégia
            if (e.BarsSeries != null &&
                e.BarsSeries.BarsPeriod.BarsPeriodType == intervalSelector.Interval.BarsPeriodType &&
                e.BarsSeries.BarsPeriod.Value == intervalSelector.Interval.Value)
            {
                // Processa barras atualizadas (iterando do MinIndex ao MaxIndex)
                for (int i = e.MinIndex; i <= e.MaxIndex; i++)
                {
                    BarData barData = new BarData
                    {
                        Open = e.BarsSeries.GetOpen(i),
                        High = e.BarsSeries.GetHigh(i),
                        Low = e.BarsSeries.GetLow(i),
                        Close = e.BarsSeries.GetClose(i),
                        Time = e.BarsSeries.GetTime(i),
                        Volume = e.BarsSeries.GetVolume(i)
                    };
                    
                    // Envia a barra para a estratégia
                    currentStrategy.ProcessBar(barData);
                }
            }
        }
        
        #endregion
        
        #region Métodos Privados
        
        // Cria a estratégia baseada na seleção do usuário
        private void CreateStrategy()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("CreateStrategy: Iniciando criação da estratégia");
                
                if (strategyTypeComboBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("CreateStrategy: ERRO - strategyTypeComboBox é null");
                    // Cria uma estratégia padrão segura
                    currentStrategy = new MovingAverageCrossStrategy(9, 21);
                    LogMessage("Estratégia padrão criada devido a erro de interface");
                    return;
                }
                
                int strategyType = strategyTypeComboBox.SelectedIndex;
                System.Diagnostics.Debug.WriteLine("CreateStrategy: Tipo selecionado: " + strategyType);
                
                switch (strategyType)
                {
                    case 0: // Média Móvel Cruzamento
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("CreateStrategy: Criando MovingAverageCrossStrategy");
                            if (fastMAPeriodTextBox == null || slowMAPeriodTextBox == null)
                            {
                                System.Diagnostics.Debug.WriteLine("CreateStrategy: TextBox de MA são null, usando valores padrão");
                                currentStrategy = new MovingAverageCrossStrategy(9, 21);
                            }
                            else
                            {
                                int fastPeriod = int.Parse(fastMAPeriodTextBox.Text);
                                int slowPeriod = int.Parse(slowMAPeriodTextBox.Text);
                                currentStrategy = new MovingAverageCrossStrategy(fastPeriod, slowPeriod);
                                System.Diagnostics.Debug.WriteLine($"CreateStrategy: MovingAverageCrossStrategy criada com fast={fastPeriod}, slow={slowPeriod}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("CreateStrategy: Erro ao criar MA strategy: " + ex.Message);
                            currentStrategy = new MovingAverageCrossStrategy(9, 21);
                        }
                        break;
                        
                    case 1: // RSI
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("CreateStrategy: Criando RSIStrategy");
                            if (rsiPeriodTextBox == null || overBoughtTextBox == null || overSoldTextBox == null)
                            {
                                System.Diagnostics.Debug.WriteLine("CreateStrategy: TextBox de RSI são null, usando valores padrão");
                                currentStrategy = new RSIStrategy(14, 70, 30);
                            }
                            else
                            {
                                int rsiPeriod = int.Parse(rsiPeriodTextBox.Text);
                                int overBought = int.Parse(overBoughtTextBox.Text);
                                int overSold = int.Parse(overSoldTextBox.Text);
                                currentStrategy = new RSIStrategy(rsiPeriod, overBought, overSold);
                                System.Diagnostics.Debug.WriteLine($"CreateStrategy: RSIStrategy criada com period={rsiPeriod}, overbought={overBought}, oversold={overSold}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("CreateStrategy: Erro ao criar RSI strategy: " + ex.Message);
                            currentStrategy = new RSIStrategy(14, 70, 30);
                        }
                        break;
                        
                    case 2: // Bollinger Bands
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("CreateStrategy: Criando BollingerBandsStrategy");
                            if (bbPeriodTextBox == null || stdDevTextBox == null)
                            {
                                System.Diagnostics.Debug.WriteLine("CreateStrategy: TextBox de BB são null, usando valores padrão");
                                currentStrategy = new BollingerBandsStrategy(20, 2.0);
                            }
                            else
                            {
                                int bbPeriod = int.Parse(bbPeriodTextBox.Text);
                                double stdDev = double.Parse(stdDevTextBox.Text);
                                currentStrategy = new BollingerBandsStrategy(bbPeriod, stdDev);
                                System.Diagnostics.Debug.WriteLine($"CreateStrategy: BollingerBandsStrategy criada com period={bbPeriod}, stdDev={stdDev}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("CreateStrategy: Erro ao criar BB strategy: " + ex.Message);
                            currentStrategy = new BollingerBandsStrategy(20, 2.0);
                        }
                        break;
                        
                    default:
                        System.Diagnostics.Debug.WriteLine("CreateStrategy: Usando estratégia padrão");
                        currentStrategy = new MovingAverageCrossStrategy(9, 21);
                        break;
                }
                
                if (currentStrategy != null)
                {
                    LogMessage("Estratégia criada: " + currentStrategy.GetType().Name);
                    System.Diagnostics.Debug.WriteLine("CreateStrategy: Estratégia criada com sucesso: " + currentStrategy.GetType().Name);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CreateStrategy: ALERTA - currentStrategy é null após criação");
                    currentStrategy = new MovingAverageCrossStrategy(9, 21);
                    LogMessage("Erro ao criar estratégia, usando estratégia padrão");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERRO em CreateStrategy: " + ex.Message + "\n" + ex.StackTrace);
                // Em caso de erro, cria uma estratégia padrão para evitar NRE
                currentStrategy = new MovingAverageCrossStrategy(9, 21);
                LogMessage("Erro ao criar estratégia: " + ex.Message);
            }
        }
        
        // Configura o gerenciador de risco
        private void ConfigureRiskManager()
        {
            int positionSize = int.Parse(positionSizeTextBox.Text);
            int stopLossTicks = int.Parse(stopLossTextBox.Text);
            int takeProfitTicks = int.Parse(takeProfitTextBox.Text);
            double maxDailyRisk = double.Parse(maxDailyRiskTextBox.Text);
            
            riskManager.Configure(positionSize, stopLossTicks, takeProfitTicks, maxDailyRisk);
            
            LogMessage("Gerenciador de risco configurado: Tamanho=" + positionSize + 
                      ", SL=" + stopLossTicks + ", TP=" + takeProfitTicks + 
                      ", Risco Máx=$" + maxDailyRisk);
        }
        
        // Assina dados de mercado
        private void SubscribeToMarketData()
        {
            if (instrument == null)
                return;
            
            // Assinatura de dados em tempo real
            marketData = new MarketData(instrument);
            marketData.Update += OnMarketData;
            
            // Assinatura de barras (constrói um BarsRequest e depois define BarsPeriod)
            barsRequest = new BarsRequest(instrument, DateTime.Now.AddDays(-1), DateTime.Now);
            barsRequest.BarsPeriod = intervalSelector.Interval;
            barsRequest.Update += OnBarsUpdate;
            
            LogMessage("Assinatura de dados iniciada para " + instrument.FullName);
        }
        
        // Cancela assinaturas de dados
        private void UnsubscribeFromMarketData()
        {
            if (marketData != null)
            {
                marketData.Update -= OnMarketData;
                marketData = null;
            }
            
            if (barsRequest != null)
            {
                barsRequest.Update -= OnBarsUpdate;
                barsRequest = null;
            }
            
            LogMessage("Assinatura de dados cancelada");
        }
        
        // Executa uma operação de compra ou venda
        private void ExecuteTrade(TradeSignal signal)
        {
            if (accountSelector.SelectedAccount == null || instrument == null)
                return;
            
            Account account = accountSelector.SelectedAccount;
            int quantity = riskManager.PositionSize;
            
            try
            {
                if (signal == TradeSignal.Buy)
                {
                    // Cria uma ordem de compra a mercado
#pragma warning disable 0612, 0618
                    Order order = account.CreateOrder(instrument, OrderAction.Buy, OrderType.Market, TimeInForce.Day, quantity, 0, 0, string.Empty, "NTBot", null);
                    
                    // Adiciona stop loss e take profit
                    double lastPrice = marketData != null && marketData.Last != null ? marketData.Last.Price : 0.0;
                    double stopLossPrice = lastPrice - (instrument.MasterInstrument.TickSize * riskManager.StopLossTicks);
                    double takeProfitPrice = lastPrice + (instrument.MasterInstrument.TickSize * riskManager.TakeProfitTicks);

                    // Stop Loss
                    Order stopOrder = account.CreateOrder(instrument, OrderAction.Sell, OrderType.StopMarket, TimeInForce.Day, quantity, 0, stopLossPrice, string.Empty, "NTBot-StopLoss", null);
                    stopOrder.OrderState = OrderState.Working;

                    // Take Profit
                    Order limitOrder = account.CreateOrder(instrument, OrderAction.Sell, OrderType.Limit, TimeInForce.Day, quantity, takeProfitPrice, 0, string.Empty, "NTBot-TakeProfit", null);
#pragma warning restore 0612, 0618
                    limitOrder.OrderState = OrderState.Working;
                    
                    // Submete a ordem principal
                    account.Submit(new[] { order });
                    
                    LogMessage("Ordem de COMPRA enviada: " + quantity + " @ Mercado");
                }
                else if (signal == TradeSignal.Sell)
                {
                    // Cria uma ordem de venda a mercado
#pragma warning disable 0612, 0618
                    Order order = account.CreateOrder(instrument, OrderAction.Sell, OrderType.Market, TimeInForce.Day, quantity, 0, 0, string.Empty, "NTBot", null);
                    
                    // Adiciona stop loss e take profit
                    double lastPrice = marketData != null && marketData.Last != null ? marketData.Last.Price : 0.0;
                    double stopLossPrice = lastPrice + (instrument.MasterInstrument.TickSize * riskManager.StopLossTicks);
                    double takeProfitPrice = lastPrice - (instrument.MasterInstrument.TickSize * riskManager.TakeProfitTicks);

                    // Stop Loss
                    Order stopOrder = account.CreateOrder(instrument, OrderAction.Buy, OrderType.StopMarket, TimeInForce.Day, quantity, 0, stopLossPrice, string.Empty, "NTBot-StopLoss", null);
                    stopOrder.OrderState = OrderState.Working;

                    // Take Profit
                    Order limitOrder = account.CreateOrder(instrument, OrderAction.Buy, OrderType.Limit, TimeInForce.Day, quantity, takeProfitPrice, 0, string.Empty, "NTBot-TakeProfit", null);
#pragma warning restore 0612, 0618
                    limitOrder.OrderState = OrderState.Working;
                    
                    // Submete a ordem principal
                    account.Submit(new[] { order });
                    
                    LogMessage("Ordem de VENDA enviada: " + quantity + " @ Mercado");
                }
                else if (signal == TradeSignal.Exit)
                {
                    // Verifica se existe uma posição aberta
                    Position position = account.Positions.FirstOrDefault(p => p.Instrument == instrument);
                    
                    if (position != null && position.Quantity != 0)
                    {
                        // Cria ordem para fechar a posição
                        OrderAction action = position.Quantity > 0 ? OrderAction.Sell : OrderAction.Buy;
                        int exitQuantity = Math.Abs(position.Quantity);
                        
#pragma warning disable 0612, 0618
                        Order order = account.CreateOrder(instrument, action, OrderType.Market, TimeInForce.Day, exitQuantity, 0, 0, string.Empty, "NTBot-Exit", null);
#pragma warning restore 0612, 0618
                        account.Submit(new[] { order });
                        
                        LogMessage("Ordem de SAÍDA enviada: " + exitQuantity + " @ Mercado");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage("Erro ao enviar ordem: " + ex.Message);
            }
        }
        
        // Adiciona uma mensagem ao log
        private void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            try
            {
                if (logTextBox != null)
                {
                    logTextBox.AppendText("[" + timestamp + "] " + message + Environment.NewLine);
                    logTextBox.ScrollToEnd();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[" + timestamp + "] " + message);
                }
            }
            catch (Exception ex)
            {
                // Ensure logging never throws back to UI flow
                System.Diagnostics.Debug.WriteLine("Erro no LogMessage: " + ex.Message + " - Original: " + message);
            }
        }
        
        #endregion
        
        #region IInstrumentProvider / IIntervalProvider + NTTabPage overrides

        // Implementação da interface IInstrumentProvider
        public NinjaTrader.Cbi.Instrument Instrument
        {
            get { return instrument; }
            set
            {
                // Unsubscribe from previous instrument data
                if (instrument != null)
                {
                    if (marketData != null)
                        marketData.Update -= OnMarketData;
                    if (barsRequest != null)
                        barsRequest.Update -= OnBarsUpdate;
                }

                // Subscribe to new instrument
                if (value != null)
                {
                    marketData = new MarketData(value);
                    marketData.Update += OnMarketData;

                    barsRequest = new BarsRequest(value, DateTime.Now.AddDays(-1), DateTime.Now);
                    barsRequest.BarsPeriod = intervalSelector.Interval;
                    barsRequest.Update += OnBarsUpdate;
                }

                instrument = value;
                if (instrumentSelector != null)
                    instrumentSelector.Instrument = value;

                // Atualiza o cabeçalho
                RefreshHeader();
            }
        }

        // Implementação da interface IIntervalProvider
        public BarsPeriod BarsPeriod
        {
            get { return intervalSelector.Interval; }
            set { intervalSelector.Interval = value; }
        }

        // NTTabPage member. Required to determine the text for the tab header name
        protected override string GetHeaderPart(string variable)
        {
            switch (variable)
            {
                case "@INSTRUMENT":
                    return Instrument == null ? "New Tab" : Instrument.MasterInstrument.Name;
                case "@INSTRUMENT_FULL":
                    return Instrument == null ? "New Tab" : Instrument.FullName;
            }
            return variable;
        }

        // Called by TabControl when tab is being removed or window is closed
        public override void Cleanup()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("NTBotPage: Cleanup iniciado");
                
                // Desinscreve eventos
                try
                {
                    Account.AccountStatusUpdate -= OnAccountStatusUpdate;
                    System.Diagnostics.Debug.WriteLine("NTBotPage: AccountStatusUpdate desregistrado");
                    
                    if (updateTimer != null)
                    {
                        updateTimer.Stop();
                        updateTimer.Tick -= OnUpdateTimerTick;
                        System.Diagnostics.Debug.WriteLine("NTBotPage: Timer parado e desregistrado");
                    }
                    
                    if (instrumentSelector != null)
                    {
                        instrumentSelector.InstrumentChanged -= OnInstrumentChanged;
                        System.Diagnostics.Debug.WriteLine("NTBotPage: instrumentSelector.InstrumentChanged desregistrado");
                    }
                    
                    if (marketData != null)
                    {
                        marketData.Update -= OnMarketData;
                        System.Diagnostics.Debug.WriteLine("NTBotPage: marketData.Update desregistrado");
                    }
                    
                    if (barsRequest != null)
                    {
                        barsRequest.Update -= OnBarsUpdate;
                        System.Diagnostics.Debug.WriteLine("NTBotPage: barsRequest.Update desregistrado");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("NTBotPage: Erro ao desregistrar eventos: " + ex.Message);
                }
                
                System.Diagnostics.Debug.WriteLine("NTBotPage: Chamando Cleanup da classe base");
                base.Cleanup();
                System.Diagnostics.Debug.WriteLine("NTBotPage: Cleanup concluído");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("NTBotPage: Erro em Cleanup: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        // NTTabPage member. Required for restoring elements from workspace
        protected override void Restore(System.Xml.Linq.XElement element)
        {
            if (element == null)
                return;

            var accountElement = element.Element("Account");
            if (accountElement != null && accountSelector != null)
                accountSelector.DesiredAccount = accountElement.Value;

            var instrumentElement = element.Element("Instrument");
            if (instrumentElement != null && !string.IsNullOrEmpty(instrumentElement.Value))
                Instrument = NinjaTrader.Cbi.Instrument.GetInstrument(instrumentElement.Value);
        }

        // NTTabPage member. Required for storing elements to workspace
        protected override void Save(System.Xml.Linq.XElement element)
        {
            if (element == null)
                return;

            if (accountSelector != null && !string.IsNullOrEmpty(accountSelector.DesiredAccount))
                element.Add(new System.Xml.Linq.XElement("Account") { Value = accountSelector.DesiredAccount });

            if (Instrument != null)
                element.Add(new System.Xml.Linq.XElement("Instrument") { Value = Instrument.FullName });
        }

        #endregion
    }
}
