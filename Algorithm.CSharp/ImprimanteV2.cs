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
    public class CryptoSmartTrade : QCAlgorithm
    {

        public int StochPeriod = 14;
        public int KPeriod = 3;
        public int DPeriod = 3;


        private Symbol _btcusd;


        private string _chartName = "Trade Plot";
        private string _priceSeriesName = "Price";
        private string _portfolioValueSeriesName = "PortFolioValue";
        private string _ADX = "ADX";
        private string _bollingerUpperSeriesName = "UpperBollinger";
        private string _bollingerLowerSeriesName = "LowerBollinger";





        public AverageDirectionalIndex adx;
        private BollingerBands _bollingerBands;
        private Stochastic _stoch;
        public override void Initialize()
        {
            this.InitPeriod();


            this.SetWarmUp(TimeSpan.FromDays(365));


            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);


            SetCash(10000); // capital
            var btcSecurity = AddCrypto("BTCUSD", Resolution.Daily);
            _btcusd = btcSecurity.Symbol;


            _bollingerBands = BB(_btcusd, 20, 2, MovingAverageType.Simple, Resolution.Daily);
            adx = ADX(_btcusd, 25, Resolution.Daily);
            _stoch = STO(_btcusd, StochPeriod, KPeriod, DPeriod, Resolution.Daily);


            var stockPlot = new Chart(_chartName);
            var assetPrice = new Series(_priceSeriesName, SeriesType.Line, "$", Color.Blue);
            var portFolioValue = new Series(_portfolioValueSeriesName, SeriesType.Line, "$", Color.Green);
            var upperBollingerSeries = new Series(_bollingerUpperSeriesName, SeriesType.Line, "$", Color.Gray);
            var lowerBollingerSeries = new Series(_bollingerLowerSeriesName, SeriesType.Line, "$", Color.Gray);
            var ADXPlot = new Series(_ADX, SeriesType.Line, "$", Color.Pink);





            stockPlot.AddSeries(assetPrice);
            stockPlot.AddSeries(portFolioValue);
            stockPlot.AddSeries(upperBollingerSeries);
            stockPlot.AddSeries(lowerBollingerSeries);
            stockPlot.AddSeries(ADXPlot);
            AddChart(stockPlot);


            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);
        }


        private void DoPlots()
        {
            Plot(_chartName, _priceSeriesName, Securities[_btcusd].Price);
            Plot(_chartName, _portfolioValueSeriesName, Portfolio.TotalPortfolioValue);
            Plot(_chartName, _bollingerUpperSeriesName, _bollingerBands.UpperBand);
            Plot(_chartName, _ADX, adx);
            Plot(_chartName, _bollingerLowerSeriesName, _bollingerBands.LowerBand);

        }


        public override void OnData(Slice data)
        {
            if (this.IsWarmingUp || !_bollingerBands.IsReady || !_stoch.IsReady)
                return;


            var holdings = Portfolio[_btcusd].Quantity;
            var currentPrice = data[_btcusd].Close;


            if (adx >= 20 && currentPrice > _bollingerBands.UpperBand && _stoch.StochK > 80)
            {
                if (!Portfolio.Invested)
                {
                    SetHoldings(_btcusd, 1);
                }
            }
            else if (adx < 15 && currentPrice < _bollingerBands.LowerBand && _stoch.StochK < 20)
            {
                if (Portfolio.Invested)
                {
                    Liquidate(_btcusd);
                }
            }
        }


        private void InitPeriod()
        {
            SetStartDate(2021, 1, 1); // Start date
            SetEndDate(2023, 10, 20); // End date


            //SetStartDate(2013, 04, 07); // début backtest 164
            //SetEndDate(2015, 01, 14); // fin backtest 172




            //SetStartDate(2014, 02, 08); // début backtest 680
            //SetEndDate(2016, 11, 07); // fin backtest 703




            //SetStartDate(2017, 08, 08); // début backtest 3412
            //SetEndDate(2019, 02, 05); // fin backtest 3432


            //SetStartDate(2018, 01, 30); // début backtest 9971
            //SetEndDate(2020, 07, 26); // fin backtest 9945




            //SetStartDate(2017, 12, 15); // début backtest 17478
            //SetEndDate(2022, 12, 12); // fin backtest 17209


            //SetStartDate(2017, 11, 25); // début backtest 8718
            //SetEndDate(2020, 05, 1); // fin backtest 8832
        }
    }
}
