using System;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data;
using System.Xml.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Parameters;

namespace QuantConnect.Algorithm.CSharp
{
    public class RsiAlphaModel : AlphaModel
    {
        private DateTime _previous;
        private readonly int _period;
        private readonly Resolution _resolution;
        private readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();
        private readonly TimeSpan _insightPeriod;

        public RsiAlphaModel(int period = 14, Resolution resolution = Resolution.Daily)
        {
            _period = period;
            _resolution = resolution;
            _insightPeriod = Time.Multiply(resolution.ToTimeSpan(), period);

            Name = $"{nameof(RsiAlphaModel)}({period},{resolution})";
        }

        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var insights = new List<Insight>();

            foreach (var symbolData in _symbolDataBySymbol.Values)
            {
                var rsi = symbolData.RSI;
                var previousState = symbolData.State;
                var state = GetState(rsi, previousState);

                if (state != previousState && rsi.IsReady)
                {
                    if (state == State.TrippedLow)
                    {
                        insights.Add(Insight.Price(symbolData.Symbol, _insightPeriod, InsightDirection.Up));
                    }

                    if (state == State.TrippedHigh)
                    {
                        insights.Add(Insight.Price(symbolData.Symbol, _insightPeriod, InsightDirection.Down));
                    }
                }

                symbolData.State = state;
            }

            return insights;
        }

        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (var removed in changes.RemovedSecurities)
            {
                var symbol = removed.Symbol;
                if (_symbolDataBySymbol.TryGetValue(symbol, out var symbolData))
                {
                    symbolData.Dispose();
                    _symbolDataBySymbol.Remove(symbol);
                }
            }

            foreach (var added in changes.AddedSecurities)
            {
                var symbol = added.Symbol;
                if (!_symbolDataBySymbol.ContainsKey(symbol))
                {
                    var symbolData = new SymbolData(algorithm, symbol, _period, _resolution);
                    _symbolDataBySymbol[symbol] = symbolData;
                }
            }
        }

        private State GetState(RelativeStrengthIndex rsi, State previous)
        {
            if (rsi.Current.Value > 70)
            {
                return State.TrippedHigh;
            }

            if (rsi.Current.Value < 30)
            {
                return State.TrippedLow;
            }

            if (previous == State.TrippedLow && rsi.Current.Value > 35)
            {
                return State.Middle;
            }

            if (previous == State.TrippedHigh && rsi.Current.Value < 65)
            {
                return State.Middle;
            }

            return previous;
        }

        public bool CanRunLocally { get; } = true;

        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        public long DataPoints => 0;

        public int AlgorithmHistoryDataPoints => 0;

        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>();
    }

    public class SymbolData
    {
        public Symbol Symbol { get; }
        public State State { get; set; }

        public RelativeStrengthIndex RSI { get; }
        private readonly IDataConsolidator _consolidator;

        public SymbolData(QCAlgorithm algorithm, Symbol symbol, int period, Resolution resolution)
        {
            Symbol = symbol;
            State = State.Middle;
            RSI = new RelativeStrengthIndex(period, MovingAverageType.Wilders);
            _consolidator = algorithm.ResolveConsolidator(symbol, resolution);
            algorithm.RegisterIndicator(symbol, RSI, _consolidator);
        }

        public void Update(TradeBar bar)
        {
            _consolidator.Update(bar);
        }

        public void Dispose()
        {
            _consolidator.Dispose();
        }
    }

    public enum State
    {
        TrippedLow = 0,
        Middle = 1,
        TrippedHigh = 2
    }
}
