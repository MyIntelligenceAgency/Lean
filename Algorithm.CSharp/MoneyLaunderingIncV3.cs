using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Parameters;
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Orders;
using QuantConnect.Algorithm.Framework.Alphas;
using System.Drawing;
using System.Security.Cryptography;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of the parameter system of QuantConnect. Using parameters you can pass the values required into C# algorithms for optimization.
    /// </summary>
    /// <meta name="tag" content="optimization" />
    /// <meta name="tag" content="using quantconnect" />
    public class MoneyLaunderingIncV3: QCAlgorithm
    {
        //L'attribut Parameter permet de définir les paramètres dans le fichier de configuration, et d'utiliser une optimisation
        public int FastPeriod = 50;

        public int SlowPeriod = 150;

        public decimal UpCrossMargin = 1.001m;

        public decimal DownCrossMargin = 0.999m;

        public ExponentialMovingAverage Fast;
        public ExponentialMovingAverage Slow;
        public RelativeStrengthIndex _rsi;
        public MovingAverageConvergenceDivergence macd1;
        public AverageDirectionalIndex adx1;

        private Symbol _btcusd;
  
        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfoliovalueSeriesName = "PortFolioValue";
        private string _FastSeriesName = "FastEMA";
        private string _SlowSeriesName = "SlowEMA";
        private string _MACD = "MACD";
        private string _ADX = "ADX";
        

        public override void Initialize()
        {

            this.InitPeriod();

            this.SetWarmUp(TimeSpan.FromDays(365));

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            SetCash(10000); // capital
            var btcSecurity = AddCrypto("BTCUSD", Resolution.Daily);

            _btcusd = btcSecurity.Symbol;

            Fast = EMA(_btcusd, FastPeriod, Resolution.Daily);
            Slow = EMA(_btcusd, SlowPeriod, Resolution.Daily);
            _rsi = RSI(_btcusd, 21, MovingAverageType.Simple, Resolution.Daily);
            macd1 = MACD(_btcusd, 12 , 26, 9, MovingAverageType.Exponential, Resolution.Daily, Field.Close);
            adx1 = ADX(_btcusd, 25, Resolution.Daily);


            var stockPlot = new Chart(_ChartName);
            var assetPrice = new Series(_PriceSeriesName, SeriesType.Line, "$", Color.Blue);
            var portFolioValue = new Series(_PortfoliovalueSeriesName, SeriesType.Line, "$", Color.Green);
            var fastSeries = new Series(_FastSeriesName, SeriesType.Line, "$", Color.Red);
            var slowSeries = new Series(_SlowSeriesName, SeriesType.Line, "$", Color.Yellow);
            var MACDPlot = new Series(_MACD, SeriesType.Line, "$", Color.Purple);
            var ADXPlot = new Series(_ADX, SeriesType.Line, "$", Color.Pink);


            stockPlot.AddSeries(assetPrice);
            stockPlot.AddSeries(portFolioValue);
            stockPlot.AddSeries(fastSeries);
            stockPlot.AddSeries(slowSeries);
            stockPlot.AddSeries(MACDPlot);
            stockPlot.AddSeries(ADXPlot);
            AddChart(stockPlot);
            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);
            

        }
        
        private void DoPlots()
        {
            Plot(_ChartName, _PriceSeriesName, Securities[_btcusd].Price);
            Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
            Plot(_ChartName, _FastSeriesName, Fast);
            Plot(_ChartName, _SlowSeriesName, Slow);
            Plot(_ChartName, _MACD, macd1);
            Plot(_ChartName, _ADX, adx1);
        }
        

        public override void OnData(Slice data)
        {

            if (this.IsWarmingUp || !Fast.IsReady || !Slow.IsReady || !macd1.IsReady) return;

            var holdings = Portfolio[_btcusd].Quantity;
            var currentPrice = data[_btcusd].Close;
            var macdHistogram = macd1 - macd1.Signal;
            var isMacdBullish = macdHistogram > 0;
            var isMacdBearish = macdHistogram < 0;

            if (adx1 >= 20 && isMacdBullish)
            {
                if (!Portfolio.Invested)
                {
                    SetHoldings(_btcusd, 1);

                }
            }
            else if (adx1 < 15 && isMacdBearish)
            {
                if (Portfolio.Invested)
                {
                    Liquidate(_btcusd);
                }
            }
        }


        public override void OnOrderEvent(OrderEvent orderEvent)
        {

            if (orderEvent.Status == OrderStatus.Filled)
            {

                string message = "";
                if (orderEvent.Quantity < 0)
                {
                    message = "Sold";
                }
                else
                {
                    message = "Purchased";
                }

                var endMessage =
                    $"{orderEvent.UtcTime.ToShortDateString()}, Price:  @{this.CurrentSlice.Bars[_btcusd].Close:N3}$/Btc; Portfolio: {Portfolio.CashBook[Portfolio.CashBook.AccountCurrency].Amount:N3}$, {Portfolio[_btcusd].Quantity}BTCs, Total Value: {Portfolio.TotalPortfolioValue:N3}$, Total Fees: {Portfolio.TotalFees:N3}$";
                if (orderEvent.AbsoluteFillQuantity * orderEvent.FillPrice > 100)
                {
                    Log($"{message} {endMessage}");
                }


            }

        }


        private void InitPeriod()
        {
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

            //SetStartDate(2021, 1, 1); // début backtest 29410
            //SetEndDate(2023, 10, 20); // fin backtest 29688

            SetStartDate(2020, 01, 01);
            SetEndDate(2022, 12, 31); 
        }



    }
}
