using System;
using System.Collections.Generic;
using System.Linq;
using NTBot.Core;

namespace NTBot.Strategies
{
    /// <summary>
    /// Classe base abstrata para todas as estratégias de trading
    /// </summary>
    public abstract class TradingStrategy : ITradingStrategy
    {
        // Lista para armazenar as barras de preço
        protected List<BarData> bars = new List<BarData>();
        
        // Último preço processado
        protected double lastPrice;
        protected DateTime lastUpdateTime;
        
        /// <summary>
        /// Processa dados de mercado em tempo real
        /// </summary>
        public abstract StrategyOutput ProcessData(StrategyInput input);
        
        /// <summary>
        /// Processa dados de barras (OHLC)
        /// </summary>
        public virtual void ProcessBar(BarData bar)
        {
            // Adiciona a barra à lista
            bars.Add(bar);
            
            // Mantém apenas as últimas 200 barras para economizar memória
            if (bars.Count > 200)
                bars.RemoveAt(0);
            
            // Atualiza o último preço
            lastPrice = bar.Close;
            lastUpdateTime = bar.Time;
        }
        
        /// <summary>
        /// Reseta o estado da estratégia
        /// </summary>
        public virtual void Reset()
        {
            bars.Clear();
            lastPrice = 0;
            lastUpdateTime = DateTime.MinValue;
        }
        
        /// <summary>
        /// Cria uma saída da estratégia com um sinal
        /// </summary>
        protected StrategyOutput CreateOutput(TradeSignal signal, string message)
        {
            return new StrategyOutput
            {
                Signal = signal,
                Message = message,
                EntryPrice = lastPrice,
                StopLossPrice = 0, // Isso será configurado pelo RiskManager
                TakeProfitPrice = 0 // Isso será configurado pelo RiskManager
            };
        }
    }
    
    /// <summary>
    /// Estratégia de Cruzamento de Médias Móveis
    /// </summary>
    public class MovingAverageCrossStrategy : TradingStrategy
    {
        private int fastPeriod;
        private int slowPeriod;
        private bool lastCrossAbove = false;
        private bool lastCrossBelow = false;
        
        /// <summary>
        /// Construtor
        /// </summary>
        public MovingAverageCrossStrategy(int fastPeriod, int slowPeriod)
        {
            this.fastPeriod = fastPeriod;
            this.slowPeriod = slowPeriod;
        }
        
        /// <summary>
        /// Processa dados de mercado em tempo real
        /// </summary>
        public override StrategyOutput ProcessData(StrategyInput input)
        {
            // Atualiza o último preço
            lastPrice = input.LastPrice;
            lastUpdateTime = input.Time;
            
            // Precisamos de pelo menos slowPeriod barras para calcular médias móveis
            if (bars.Count < slowPeriod)
                return CreateOutput(TradeSignal.None, "Aguardando mais dados...");
            
            // Calcula médias móveis
            double fastMA = CalculateMA(fastPeriod);
            double slowMA = CalculateMA(slowPeriod);
            
            // Verifica cruzamentos
            bool crossAbove = fastMA > slowMA && !lastCrossAbove;
            bool crossBelow = fastMA < slowMA && !lastCrossBelow;
            
            // Atualiza o estado dos cruzamentos
            lastCrossAbove = fastMA > slowMA;
            lastCrossBelow = fastMA < slowMA;
            
            // Gera sinais
            if (crossAbove)
            {
                return CreateOutput(TradeSignal.Buy, $"Cruzamento para cima: Fast MA ({fastPeriod}) = {fastMA:F2}, Slow MA ({slowPeriod}) = {slowMA:F2}");
            }
            else if (crossBelow)
            {
                return CreateOutput(TradeSignal.Sell, $"Cruzamento para baixo: Fast MA ({fastPeriod}) = {fastMA:F2}, Slow MA ({slowPeriod}) = {slowMA:F2}");
            }
            
            return CreateOutput(TradeSignal.None, $"Sem cruzamento: Fast MA ({fastPeriod}) = {fastMA:F2}, Slow MA ({slowPeriod}) = {slowMA:F2}");
        }
        
        /// <summary>
        /// Calcula a média móvel simples
        /// </summary>
        private double CalculateMA(int period)
        {
            if (bars.Count < period)
                return 0;
            
            // Pega as últimas 'period' barras e calcula a média dos fechamentos
            double sum = 0;
            for (int i = bars.Count - 1; i >= bars.Count - period; i--)
            {
                sum += bars[i].Close;
            }
            
            return sum / period;
        }
    }
    
    /// <summary>
    /// Estratégia baseada no indicador RSI (Índice de Força Relativa)
    /// </summary>
    public class RSIStrategy : TradingStrategy
    {
        private int period;
        private int overBoughtLevel;
        private int overSoldLevel;
        private bool wasOverBought = false;
        private bool wasOverSold = false;
        
        /// <summary>
        /// Construtor
        /// </summary>
        public RSIStrategy(int period, int overBoughtLevel, int overSoldLevel)
        {
            this.period = period;
            this.overBoughtLevel = overBoughtLevel;
            this.overSoldLevel = overSoldLevel;
        }
        
        /// <summary>
        /// Processa dados de mercado em tempo real
        /// </summary>
        public override StrategyOutput ProcessData(StrategyInput input)
        {
            // Atualiza o último preço
            lastPrice = input.LastPrice;
            lastUpdateTime = input.Time;
            
            // Precisamos de pelo menos period+1 barras para calcular o RSI
            if (bars.Count <= period)
                return CreateOutput(TradeSignal.None, "Aguardando mais dados...");
            
            // Calcula o RSI
            double rsi = CalculateRSI();
            
            // Verifica condições de sobrecompra/sobrevenda
            bool isOverBought = rsi >= overBoughtLevel;
            bool isOverSold = rsi <= overSoldLevel;
            
            // Gera sinais
            if (isOverBought && !wasOverBought)
            {
                wasOverBought = true;
                wasOverSold = false;
                return CreateOutput(TradeSignal.Sell, $"RSI em sobrecompra: {rsi:F2}");
            }
            else if (isOverSold && !wasOverSold)
            {
                wasOverBought = false;
                wasOverSold = true;
                return CreateOutput(TradeSignal.Buy, $"RSI em sobrevenda: {rsi:F2}");
            }
            else if (rsi < overBoughtLevel - 5)
            {
                wasOverBought = false;
            }
            else if (rsi > overSoldLevel + 5)
            {
                wasOverSold = false;
            }
            
            return CreateOutput(TradeSignal.None, $"RSI atual: {rsi:F2}");
        }
        
        /// <summary>
        /// Calcula o indicador RSI
        /// </summary>
        private double CalculateRSI()
        {
            if (bars.Count <= period)
                return 50; // Valor neutro
            
            double sumGain = 0;
            double sumLoss = 0;
            
            // Calcula a soma dos ganhos e perdas no período
            for (int i = bars.Count - period; i < bars.Count; i++)
            {
                double change = bars[i].Close - bars[i - 1].Close;
                
                if (change >= 0)
                    sumGain += change;
                else
                    sumLoss -= change; // Converte para valor positivo
            }
            
            // Evita divisão por zero
            if (sumLoss == 0)
                return 100;
            
            // Calcula o RSI
            double avgGain = sumGain / period;
            double avgLoss = sumLoss / period;
            double rs = avgGain / avgLoss;
            
            return 100 - (100 / (1 + rs));
        }
    }
    
    /// <summary>
    /// Estratégia baseada em Bandas de Bollinger
    /// </summary>
    public class BollingerBandsStrategy : TradingStrategy
    {
        private int period;
        private double standardDeviations;
        private bool wasBelowLower = false;
        private bool wasAboveUpper = false;
        
        /// <summary>
        /// Construtor
        /// </summary>
        public BollingerBandsStrategy(int period, double standardDeviations)
        {
            this.period = period;
            this.standardDeviations = standardDeviations;
        }
        
        /// <summary>
        /// Processa dados de mercado em tempo real
        /// </summary>
        public override StrategyOutput ProcessData(StrategyInput input)
        {
            // Atualiza o último preço
            lastPrice = input.LastPrice;
            lastUpdateTime = input.Time;
            
            // Precisamos de pelo menos period barras para calcular Bandas de Bollinger
            if (bars.Count < period)
                return CreateOutput(TradeSignal.None, "Aguardando mais dados...");
            
            // Calcula as Bandas de Bollinger
            double middleBand = CalculateMA();
            double stdDev = CalculateStdDev(middleBand);
            double upperBand = middleBand + (standardDeviations * stdDev);
            double lowerBand = middleBand - (standardDeviations * stdDev);
            
            // Verifica condições de compra/venda
            bool isBelowLower = lastPrice <= lowerBand;
            bool isAboveUpper = lastPrice >= upperBand;
            
            // Gera sinais
            if (isBelowLower && !wasBelowLower)
            {
                wasBelowLower = true;
                wasAboveUpper = false;
                return CreateOutput(TradeSignal.Buy, $"Preço na banda inferior: {lastPrice:F2} <= {lowerBand:F2}");
            }
            else if (isAboveUpper && !wasAboveUpper)
            {
                wasBelowLower = false;
                wasAboveUpper = true;
                return CreateOutput(TradeSignal.Sell, $"Preço na banda superior: {lastPrice:F2} >= {upperBand:F2}");
            }
            else if (lastPrice > lowerBand + stdDev * 0.5)
            {
                wasBelowLower = false;
            }
            else if (lastPrice < upperBand - stdDev * 0.5)
            {
                wasAboveUpper = false;
            }
            
            return CreateOutput(TradeSignal.None, $"Bandas de Bollinger: Superior={upperBand:F2}, Média={middleBand:F2}, Inferior={lowerBand:F2}");
        }
        
        /// <summary>
        /// Calcula a média móvel simples
        /// </summary>
        private double CalculateMA()
        {
            if (bars.Count < period)
                return 0;
            
            double sum = 0;
            for (int i = bars.Count - 1; i >= bars.Count - period; i--)
            {
                sum += bars[i].Close;
            }
            
            return sum / period;
        }
        
        /// <summary>
        /// Calcula o desvio padrão
        /// </summary>
        private double CalculateStdDev(double mean)
        {
            if (bars.Count < period)
                return 0;
            
            double sum = 0;
            for (int i = bars.Count - 1; i >= bars.Count - period; i--)
            {
                double diff = bars[i].Close - mean;
                sum += diff * diff;
            }
            
            return Math.Sqrt(sum / period);
        }
    }
}
