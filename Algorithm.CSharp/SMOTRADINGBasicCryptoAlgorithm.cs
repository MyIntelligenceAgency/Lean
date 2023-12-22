/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Indicators;
using System;
using System.Drawing;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Brokerages;

using QuantConnect.Parameters;
using System.Linq;

using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Portfolio;

using Accord.Math;

using QuantConnect.Data.Market;
using QuantConnect.Orders;

namespace QuantConnect
{
    public class SMOTRADINGBasicCryptoAlgorithm : QCAlgorithm
    {
        private RollingWindow<decimal> _priceWindow;
        private RollingWindow<decimal> _correlationWindow;
        private const int WindowSize = 14;
        private const decimal CorrelationThreshold = 0.5m;

        [Parameter("macd-fast")]
        public int FastPeriodMacd = 12;

        [Parameter("macd-slow")]
        public int SlowPeriodMacd = 26;

        private MovingAverageConvergenceDivergence _macd;
        private Symbol _btcusd;
        private bool _invested;

        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfoliovalueSeriesName = "PortFolioValue";

        private BollingerBands _bollingerBands;
        private TrailingStopRiskManagementModel _trailingStopRiskManagementModel;

        private decimal _supportLevel = 50000m;
        private decimal _resistanceLevel = 65000m;

        public override void Initialize()
        {
            SetStartDate(2018, 1, 1);
            SetEndDate(2022, 3, 22);
            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);
            SetCash(10000);

            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;
            _macd = MACD(_btcusd, FastPeriodMacd, SlowPeriodMacd, 9, MovingAverageType.Exponential, Resolution.Daily, Field.Close);

            var stockPlot = new Chart(_ChartName);
            var assetPrice = new Series(_PriceSeriesName, SeriesType.Line, "$", Color.Blue);
            var portFolioValue = new Series(_PortfoliovalueSeriesName, SeriesType.Line, "$", Color.Green);
            stockPlot.AddSeries(new Series("MACD", SeriesType.Line, "$", Color.Yellow));
            stockPlot.AddSeries(assetPrice);
            stockPlot.AddSeries(portFolioValue);
            AddChart(stockPlot);
            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);

            _priceWindow = new RollingWindow<decimal>(WindowSize);
            _correlationWindow = new RollingWindow<decimal>(WindowSize);

            _bollingerBands = BB(_btcusd, 20, 2, MovingAverageType.Simple, Resolution.Daily);
            stockPlot.AddSeries(new Series("bollingerUpperBand", SeriesType.Line, "$", Color.Red));
            stockPlot.AddSeries(new Series("bollingerlowerBand", SeriesType.Line, "$", Color.Red));
            AddChart(stockPlot);

            _trailingStopRiskManagementModel = new TrailingStopRiskManagementModel(0.05m);
            SetRiskManagement(_trailingStopRiskManagementModel);
        }

        private void DoPlots()
        {
            Plot(_ChartName, _PriceSeriesName, Securities[_btcusd].Price);
            Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
            Plot(_ChartName, "bollingerUpperBand", _bollingerBands.UpperBand);
            Plot(_ChartName, "bollingerlowerBand", _bollingerBands.LowerBand);
            Plot(_ChartName, "macdSignal ", _macd.Signal);
        }

        public override void OnData(Slice data)
        {
            if (!_macd.IsReady) return;

            var closePrice = Securities[_btcusd].Close;
            var holdings = Portfolio[_btcusd].Quantity;

            if (holdings <= 0 && _macd > _macd.Signal && _priceWindow.IsReady && closePrice <= _supportLevel)
            {
                decimal _correlation = Correlation(_priceWindow, WindowSize);
                if (_correlation > CorrelationThreshold)
                {
                    SetHoldings(_btcusd, 1.0);
                    _invested = true;
                }
            }
            else if (_invested && _macd < _macd.Signal && closePrice >= _resistanceLevel)
            {
                Liquidate(_btcusd);
                _invested = false;
            }

            if (!data.ContainsKey(_btcusd)) return;

            _priceWindow.Add(data[_btcusd].Close);
            decimal correlation = Correlation(_priceWindow, WindowSize);
            _correlationWindow.Add(correlation);

            var upperBand = _bollingerBands.UpperBand.Current.Value;
            var lowerBand = _bollingerBands.LowerBand.Current.Value;

            if (data.Bars[_btcusd].Close > upperBand && !_invested)
            {
                Liquidate(_btcusd);
                _invested = false;
            }
            else if (data.Bars[_btcusd].Close < lowerBand && !_invested)
            {
                SetHoldings(_btcusd, 1.0);
                _invested = true;
            }

            var riskAdjustedTargets = _trailingStopRiskManagementModel.ManageRisk(this, new[] { new PortfolioTarget(_btcusd, holdings) });
            foreach (var target in riskAdjustedTargets)
            {
                SetHoldings(target.Symbol, 5);
            }
        }

        private decimal Correlation(RollingWindow<decimal> x, int period)
        {
            if (x.IsReady)
            {
                var meanX = x.Average();
                var meanY = x.Skip(1).Take(period).Average();
                var cov = x.Zip(x.Skip(1).Take(period), (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
                var stdDevX = (decimal)Math.Sqrt(x.Sum(xi => (double)((xi - meanX) * (xi - meanX))));
                var stdDevY = (decimal)Math.Sqrt(x.Skip(1).Take(period).Sum(yi => (double)((yi - meanY) * (yi - meanY))));

                if (stdDevX > 0 && stdDevY > 0)
                {
                    return cov / (stdDevX * stdDevY);
                }
            }

            return 0m;
        }
    }
}
