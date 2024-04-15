using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Parameters;

namespace QuantConnect.Algorithm.CSharp
{
    public class MACDTrendRSIAlgoWarriorsPTFRebalanceStopLoss : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _previous;
        private Dictionary<string, MovingAverageConvergenceDivergence> _macdIndicators;
        private Dictionary<string, RelativeStrengthIndex> _rsiIndicators;
        private readonly string[] _symbols = { "AAPL", "BAC", "AIG", "IBM" };

        [Parameter(name: "toleranceValue")]
        public decimal Tolerance = 0.0025m;

        public override void Initialize()
        {
            SetStartDate(2001, 01, 01);
            SetEndDate(2022, 01, 01);

            foreach (var symbol in _symbols)
            {
                AddSecurity(SecurityType.Equity, symbol, Resolution.Hour);
            }

            _macdIndicators = new Dictionary<string, MovingAverageConvergenceDivergence>();
            _rsiIndicators = new Dictionary<string, RelativeStrengthIndex>();

            foreach (var symbol in _symbols)
            {
                _macdIndicators[symbol] = MACD(symbol, 12, 26, 9, MovingAverageType.Exponential, Resolution.Hour);
                _rsiIndicators[symbol] = RSI(symbol, 14, MovingAverageType.Simple, Resolution.Hour);
            }
        }

        public void OnData(TradeBars data)
        {
            if (_previous.Date == Time.Date) return;

            foreach (var symbol in _symbols)
            {
                var macd = _macdIndicators[symbol];
                var rsi = _rsiIndicators[symbol];

                if (!macd.IsReady || !rsi.IsReady) continue;

                var holding = Portfolio[symbol];

                var signalDeltaPercent = (macd - macd.Signal) / macd.Fast;

                // Intégration du RSI dans les conditions d'achat et de vente
                if (holding.Quantity <= 0 && signalDeltaPercent > Tolerance && rsi > 50)
                {
                    // Entrer dans la position
                    SetHoldings(symbol, 0.25);
                }
                else if (holding.Quantity > 0 && signalDeltaPercent < -Tolerance && rsi < 50)
                {
                    Liquidate(symbol);
                }

                // Vérifier et liquider la position si le prix est inférieur au stop loss
                CheckStopLoss(symbol, data[symbol].Close, 0.75m);

                Plot(symbol + "_MACD", macd, macd.Signal);
                Plot(symbol + "_Open", data[symbol].Open);
                Plot(symbol + "_Fast", macd.Fast);
                Plot(symbol + "_Slow", macd.Slow);
                Plot(symbol + "_RSI", rsi);
            }

            _previous = Time;
        }

        private void CheckStopLoss(string symbol, decimal currentPrice, decimal stopLossPercentage)
        {
            var holding = Portfolio[symbol];

            if (holding.Quantity > 0)
            {
                var stopLossPrice = holding.AveragePrice * stopLossPercentage;

                if (currentPrice < stopLossPrice)
                {
                    Liquidate(symbol);
                }
            }
        }

        public override void OnEndOfDay()
        {
            if (_previous.Year != Time.Year)
            {
                RebalancePortfolio();
            }
        }

        private void RebalancePortfolio()
        {
            foreach (var symbol in _symbols)
            {
                Liquidate(symbol);
            }

            foreach (var symbol in _symbols)
            {
                SetHoldings(symbol, 0.25);
            }
        }

        public bool CanRunLocally { get; } = true;

        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        public long DataPoints => 22137;

        public int AlgorithmHistoryDataPoints => 0;

        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            // Mettez à jour vos statistiques attendues pour le portefeuille de 4 actions
        };
    }
}
