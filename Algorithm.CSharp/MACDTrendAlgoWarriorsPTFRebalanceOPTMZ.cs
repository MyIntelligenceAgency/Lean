using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Parameters;

namespace QuantConnect.Algorithm.CSharp
{
    public class MACDTrendAlgoWarriorsPTFRebalanceOPTMZ : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private DateTime _previous;
        private Dictionary<string, MovingAverageConvergenceDivergence> _macdIndicators;
        private readonly string[] _symbols = { "AAPL", "BAC", "AIG", "IBM" };

        // Declare tolerance as a public variable
        [Parameter (name:"toleranceValue")]
        public decimal Tolerance = 0.0025m;

        public override void Initialize()
        {
            SetStartDate(2004, 01, 01);
            SetEndDate(2015, 01, 01);

            foreach (var symbol in _symbols)
            {
                AddSecurity(SecurityType.Equity, symbol, Resolution.Hour);
            }

            _macdIndicators = new Dictionary<string, MovingAverageConvergenceDivergence>();

            foreach (var symbol in _symbols)
            {
                // define MACD(12,26) with a 9-day signal for each symbol
                _macdIndicators[symbol] = MACD(symbol, 12, 26, 9, MovingAverageType.Exponential, Resolution.Hour);
            }
        }

        public void OnData(TradeBars data)
        {
            if (_previous.Date == Time.Date) return;

            foreach (var symbol in _symbols)
            {
                var macd = _macdIndicators[symbol];

                if (!macd.IsReady) continue;

                var holding = Portfolio[symbol];

                var signalDeltaPercent = (macd - macd.Signal) / macd.Fast;

                // Use the public Tolerance variable here
                if (holding.Quantity <= 0 && signalDeltaPercent > Tolerance)
                {
                    SetHoldings(symbol, 0.25); // Invest 25% of portfolio in each stock
                }
                else if (holding.Quantity >= 0 && signalDeltaPercent < -Tolerance)
                {
                    Liquidate(symbol);
                }

                Plot(symbol + "_MACD", macd, macd.Signal);
                Plot(symbol + "_Open", data[symbol].Open);
                Plot(symbol + "_Fast", macd.Fast);
                Plot(symbol + "_Slow", macd.Slow);
            }

            _previous = Time;
        }

        public override void OnEndOfDay()
        {
            // Rebalance at the end of each year
            if (_previous.Year != Time.Year)
            {
                RebalancePortfolio();
            }
        }

        private void RebalancePortfolio()
        {
            foreach (var symbol in _symbols)
            {
                Liquidate(symbol); // Liquidate all positions
            }

            foreach (var symbol in _symbols)
            {
                SetHoldings(symbol, 0.25); // Reinvest 25% of the portfolio in each stock
            }
        }

        public bool CanRunLocally { get; } = true;

        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        public long DataPoints => 22137;

        public int AlgorithmHistoryDataPoints => 0;

        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            // Update your expected statistics for the portfolio of 4 equities
        };
    }
}
