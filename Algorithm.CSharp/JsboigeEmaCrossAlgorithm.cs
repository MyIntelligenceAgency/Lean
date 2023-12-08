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
using System.Drawing;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of the parameter system of QuantConnect. Using parameters you can pass the values required into C# algorithms for optimization.
    /// </summary>
    /// <meta name="tag" content="optimization" />
    /// <meta name="tag" content="using quantconnect" />
    public class JsboigeEmaCrossAlgorithm : QCAlgorithm
    {
        //L'attribut Parameter permet de définir les paramètres dans le fichier de configuration, et d'utiliser une optimisation
        private int FastPeriod = 30;

        private int SlowPeriod = 40;


        private decimal UpCrossMargin = 1.001m;

        private decimal DownCrossMargin = 0.999m;

        private Resolution _resolution = Resolution.Daily;

        public ExponentialMovingAverage Fast;
        public ExponentialMovingAverage Slow;

        private Symbol _btcusd;

        //private string _ChartName = "Trade Plot";
        //private string _PriceSeriesName = "Price";
        //private string _PortfoliovalueSeriesName = "PortFolioValue";


        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfoliovalueSeriesName = "PortFolioValue";
        private string _FastSeriesName = "FastEMA";
        private string _SlowSeriesName = "SlowEMA";



        public override void Initialize()
        {
            this.InitPeriod();

            this.SetWarmUp(TimeSpan.FromDays(365));

            //SetBenchmark(x => 0);

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            SetCash(10000); // capital
            var btcSecurity = AddCrypto("BTCUSD", _resolution);

            _btcusd = btcSecurity.Symbol;

            Fast = this.EMA(_btcusd, FastPeriod, _resolution);
            Slow = EMA(_btcusd, SlowPeriod, _resolution);

            // Dealing with plots
            var stockPlot = new Chart(_ChartName);
            var assetPrice = new Series(_PriceSeriesName, SeriesType.Line, "$", Color.Blue);
            var portFolioValue = new Series(_PortfoliovalueSeriesName, SeriesType.Line, "$", Color.Green);
            var fastSeries = new Series(_FastSeriesName, SeriesType.Line, "$", Color.Red);
            var slowSeries = new Series(_SlowSeriesName, SeriesType.Line, "$", Color.Yellow);

            stockPlot.AddSeries(assetPrice);
            stockPlot.AddSeries(portFolioValue);
            stockPlot.AddSeries(fastSeries);
            stockPlot.AddSeries(slowSeries);
            AddChart(stockPlot);
            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);

        }

        private void DoPlots()
        {
            Plot(_ChartName, _PriceSeriesName, Securities[_btcusd].Price);
            Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
            Plot(_ChartName, _FastSeriesName, Fast);
            Plot(_ChartName, _SlowSeriesName, Slow);
        }


        public override void OnData(Slice data)
        {

            // wait for our indicators to ready
            if (this.IsWarmingUp || !Fast.IsReady || !Slow.IsReady) return;

            if (!Portfolio.Invested && Fast > Slow * UpCrossMargin)
            {
                SetHoldings(_btcusd, 1);
            }
            else if (Portfolio.Invested && Fast < Slow * DownCrossMargin)
            {
                Liquidate(_btcusd);
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
                //We skip small adjusting orders
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

            SetStartDate(2021, 1, 1); // début backtest 29410
            SetEndDate(2023, 10, 20); // fin backtest 29688
        }



    }
}
