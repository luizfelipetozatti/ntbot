using System;
using System.Linq;
using NinjaTrader.Cbi;

namespace NTBot.Core
{
    /// <summary>
    /// Enumeração para sinais de trading
    /// </summary>
    public enum TradeSignal
    {
        None,   // Sem sinal
        Buy,    // Comprar
        Sell,   // Vender
        Exit    // Sair da posição
    }

    /// <summary>
    /// Classe que representa uma entrada de dados para a estratégia
    /// </summary>
    public class StrategyInput
    {
        public double LastPrice { get; set; }
        public DateTime Time { get; set; }
        public long Volume { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
    }

    /// <summary>
    /// Classe que representa uma saída de dados da estratégia
    /// </summary>
    public class StrategyOutput
    {
        public TradeSignal Signal { get; set; }
        public string Message { get; set; }
        public double EntryPrice { get; set; }
        public double StopLossPrice { get; set; }
        public double TakeProfitPrice { get; set; }
    }

    /// <summary>
    /// Classe que representa dados de uma barra
    /// </summary>
    public class BarData
    {
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public DateTime Time { get; set; }
        public long Volume { get; set; }
    }

    /// <summary>
    /// Interface base para todas as estratégias de trading
    /// </summary>
    public interface ITradingStrategy
    {
        /// <summary>
        /// Processa dados de mercado em tempo real
        /// </summary>
        StrategyOutput ProcessData(StrategyInput input);
        
        /// <summary>
        /// Processa dados de barras (OHLC)
        /// </summary>
        void ProcessBar(BarData bar);
        
        /// <summary>
        /// Reseta o estado da estratégia
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Classe de gerenciamento de risco
    /// </summary>
    public class RiskManager
    {
        // Propriedades para configuração de gerenciamento de risco
        public int PositionSize { get; private set; }
        public int StopLossTicks { get; private set; }
        public int TakeProfitTicks { get; private set; }
        public double MaxDailyRisk { get; private set; }
        
        // Controle de estado
        private double dailyPnL = 0;
        
        /// <summary>
        /// Configura os parâmetros de gerenciamento de risco
        /// </summary>
        public void Configure(int positionSize, int stopLossTicks, int takeProfitTicks, double maxDailyRisk)
        {
            PositionSize = positionSize;
            StopLossTicks = stopLossTicks;
            TakeProfitTicks = takeProfitTicks;
            MaxDailyRisk = maxDailyRisk;
        }
        
        /// <summary>
        /// Verifica se um trade é permitido com base nas regras de risco
        /// </summary>
        public bool IsTradeAllowed(TradeSignal signal, Instrument instrument, Account account)
        {
            // Se for um sinal de saída, sempre permitir
            if (signal == TradeSignal.Exit)
                return true;
            
            // Verifica se já atingimos o risco diário máximo
            if (dailyPnL <= -MaxDailyRisk)
                return false;
            
            // Verifica se já existe uma posição para o instrumento
            Position position = account.Positions.FirstOrDefault(p => p.Instrument == instrument);
            
            // Se já existe uma posição na mesma direção, não permitir
            if (position != null)
            {
                if ((signal == TradeSignal.Buy && position.Quantity > 0) ||
                    (signal == TradeSignal.Sell && position.Quantity < 0))
                {
                    return false;
                }
            }
            
            // Calcula o valor monetário em risco
            double tickValue = instrument.MasterInstrument.PointValue * instrument.MasterInstrument.TickSize;
            double riskPerTrade = StopLossTicks * tickValue * PositionSize;
            
            // Verifica se o risco por trade é aceitável
            if (riskPerTrade > MaxDailyRisk * 0.25) // Máximo 25% do risco diário por trade
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Atualiza o P&L diário
        /// </summary>
        public void UpdateDailyPnL(double pnl)
        {
            dailyPnL += pnl;
        }
        
        /// <summary>
        /// Reseta o P&L diário (a ser chamado no início do dia)
        /// </summary>
        public void ResetDailyPnL()
        {
            dailyPnL = 0;
        }
    }
}
