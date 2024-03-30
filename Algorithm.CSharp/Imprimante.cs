using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Orders;
using QuantConnect.Algorithm.Framework.Alphas;
using System.Drawing;

namespace QuantConnect.Algorithm.CSharp
{
    public class Imprimante : QCAlgorithm
    {
        public int KPeriod = 3;
        public int DPeriod = 3;
        public decimal UpCrossMargin = 1.001m;
        public decimal DownCrossMargin = 0.999m;

        private Symbol _btcusd;

        private string _chartName = "Trade Plot";
        private string _priceSeriesName = "Price";
        private string _portfolioValueSeriesName = "PortFolioValue";
        private string _MACD = "MACD";
        private string _bollingerUpperSeriesName = "UpperBollinger";
        private string _bollingerLowerSeriesName = "LowerBollinger";
        private string _ichimokuTenkanSeriesName = "IchimokuTenkan";
        private string _ichimokuKijunSeriesName = "IchimokuKijun";
        private string _ichimokuSenkouASeriesName = "IchimokuSenkouA";
        private string _ichimokuSenkouBSeriesName = "IchimokuSenkouB";

        public BollingerBands _bollingerBands;
        public MovingAverageConvergenceDivergence macd1;
        public IchimokuKinkoHyo _ichimoku;

        public override void Initialize()
        {
            InitPeriod();

            SetWarmUp(TimeSpan.FromDays(365));

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            SetCash(10000); // capital
            var btcSecurity = AddCrypto("BTCUSD", Resolution.Daily);
            _btcusd = btcSecurity.Symbol;

            _bollingerBands = BB(_btcusd, 20, 2, MovingAverageType.Simple, Resolution.Daily);
            macd1 = MACD(_btcusd, 12, 26, 9, MovingAverageType.Exponential, Resolution.Daily, Field.Close);
            _ichimoku = ICHIMOKU(_btcusd, 9, 26, 26, 52, 26, 26);

            var stockPlot = new Chart(_chartName);
            var assetPrice = new Series(_priceSeriesName, SeriesType.Line, "$", Color.Blue);
            var portFolioValue = new Series(_portfolioValueSeriesName, SeriesType.Line, "$", Color.Green);
            var upperBollingerSeries = new Series(_bollingerUpperSeriesName, SeriesType.Line, "$", Color.Gray);
            var lowerBollingerSeries = new Series(_bollingerLowerSeriesName, SeriesType.Line, "$", Color.Gray);
            var MACDPlot = new Series(_MACD, SeriesType.Line, "$", Color.Purple);
            var ichimokuTenkanSeries = new Series(_ichimokuTenkanSeriesName, SeriesType.Line, "$", Color.Orange);
            var ichimokuKijunSeries = new Series(_ichimokuKijunSeriesName, SeriesType.Line, "$", Color.Red);
            var ichimokuSenkouASeries = new Series(_ichimokuSenkouASeriesName, SeriesType.Line, "$", Color.Cyan);
            var ichimokuSenkouBSeries = new Series(_ichimokuSenkouBSeriesName, SeriesType.Line, "$", Color.Magenta);

            stockPlot.AddSeries(assetPrice);
            stockPlot.AddSeries(portFolioValue);
            stockPlot.AddSeries(upperBollingerSeries);
            stockPlot.AddSeries(lowerBollingerSeries);
            stockPlot.AddSeries(MACDPlot);
            stockPlot.AddSeries(ichimokuTenkanSeries);
            stockPlot.AddSeries(ichimokuKijunSeries);
            stockPlot.AddSeries(ichimokuSenkouASeries);
            stockPlot.AddSeries(ichimokuSenkouBSeries);
            AddChart(stockPlot);

            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);
        }

        private void DoPlots()
        {
            Plot(_chartName, _priceSeriesName, Securities[_btcusd].Price);
            Plot(_chartName, _portfolioValueSeriesName, Portfolio.TotalPortfolioValue);
            Plot(_chartName, _bollingerUpperSeriesName, _bollingerBands.UpperBand);
            Plot(_chartName, _MACD, macd1);
            Plot(_chartName, _bollingerLowerSeriesName, _bollingerBands.LowerBand);
            Plot(_chartName, _ichimokuTenkanSeriesName, _ichimoku.Tenkan);
            Plot(_chartName, _ichimokuKijunSeriesName, _ichimoku.Kijun);
            Plot(_chartName, _ichimokuSenkouASeriesName, _ichimoku.SenkouA);
            Plot(_chartName, _ichimokuSenkouBSeriesName, _ichimoku.SenkouB);
        }

        public override void OnData(Slice data)
        {
            if (this.IsWarmingUp || !_bollingerBands.IsReady || !macd1.IsReady || !_ichimoku.IsReady)
                return;

            var holdings = Portfolio[_btcusd].Quantity;
            var currentPrice = data[_btcusd].Close;

            var tenkan = _ichimoku.Tenkan.Current.Value;
            var kijun = _ichimoku.Kijun.Current.Value;
            var senkouA = _ichimoku.SenkouA.Current.Value;
            var senkouB = _ichimoku.SenkouB.Current.Value;


            if (macd1 > 0 && currentPrice > _bollingerBands.UpperBand && tenkan > kijun)

            {
                if (!Portfolio.Invested)
                {
                    SetHoldings(_btcusd, 1);
                }
            }
            else if (macd1 < 0 && currentPrice < _bollingerBands.LowerBand && tenkan < kijun)
            {
                if (Portfolio.Invested)
                {
                    Liquidate(_btcusd);
                }
            }
        }

        private void InitPeriod()
        {
            SetStartDate(2021, 1, 1); // Start Date
            SetEndDate(2023, 10, 20); // End date
        }
    }
}
