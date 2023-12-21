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
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Brokerages;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using System.Drawing;
using static Python.Runtime.TypeSpec;

namespace QuantConnect.Algorithm.CSharp
{

    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class TotallyspiesAlgo : QCAlgorithm
    {
        private IchimokuKinkoHyo ichimoku;

        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfoliovalueSeriesName = "PortFolioValue";



        public override void Initialize()
        {
            SetStartDate(2016, 4, 4);
            SetEndDate(2018, 4, 4);
            SetCash("USD", 10000);
            SetCash("BTC", 50m);
            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);
            AddCrypto("BTCUSD");
            var symbol = AddCrypto("BTCUSD").Symbol;
            ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);

            //this.ICHIMOKU(symbol)
            RegisterIndicator(symbol, ichimoku, Resolution.Daily);

            var stockPlot = new Chart(_ChartName);
            var assetPrice = new Series(_PriceSeriesName, SeriesType.Line, "$", Color.Blue);
            var portFolioValue = new Series(_PortfoliovalueSeriesName, SeriesType.Line, "$", Color.Green);


            stockPlot.AddSeries(assetPrice);
            stockPlot.AddSeries(portFolioValue);
            AddChart(stockPlot);
            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);
        }


        private void DoPlots()
        {
            Plot(_ChartName, _PriceSeriesName, Securities["BTCUSD"].Price);
            Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);

        }

        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey("BTCUSD"))
            {
                return;
            }

            if (!ichimoku.IsReady)
            {
                return;
            }

            var btcPrice = Securities["BTCUSD"].Price;
            var btcHoldings = Portfolio["BTCUSD"].Quantity;

            if (Time.Hour == 1 && Time.Minute == 0)
            {
                //Tenkan, Kijun, et Chikou Span
                if (ichimoku.Tenkan > ichimoku.Kijun && ichimoku.Chikou > btcPrice && btcHoldings <= 0)
                {
                    //  Tenkan est au-dessus de Kijun et Chikou Span est au-dessus du prix actuel
                    Buy("BTCUSD", 0.1);
                }
                else if (ichimoku.Tenkan < ichimoku.Kijun && ichimoku.Chikou < btcPrice && btcHoldings > 0)
                {
                    //si Tenkan est en dessous de Kijun et Chikou Span est en dessous du prix actuel
                    Liquidate("BTCUSD");
                }

                if (ichimoku.Tenkan > ichimoku.Kijun && btcHoldings <= 0)
                {
                    //Tenkan-Sen croise au-dessus de Kijun-Sen
                    SetHoldings("BTCUSD", 0.1);
                }
                else if (ichimoku.Tenkan < ichimoku.Kijun && btcHoldings > 0)
                {
                    //Tenkan-Sen croise en dessous de Kijun-Sen
                    Liquidate("BTCUSD");
                }

                // la position de la Lagging Span par rapport aux nuages (Senkou Span A et B)
                if (ichimoku.Chikou > ichimoku.SenkouA && ichimoku.Chikou > ichimoku.SenkouB && btcHoldings <= 0)
                {
                    Buy("BTCUSD", 0.1);
                }
                else if (ichimoku.Chikou < ichimoku.SenkouA && ichimoku.Chikou < ichimoku.SenkouB && btcHoldings > 0)
                {
                    // si Chikou Span est en dessous des deux lignes du nuage
                    Liquidate("BTCUSD");
                }
                Debug($"BTC Price: {btcPrice}, Tenkan: {ichimoku.Tenkan}, Kijun: {ichimoku.Kijun}, Chikou Span: {ichimoku.Chikou}");

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
                    $"{orderEvent.UtcTime.ToShortDateString()}, Price:  @{this.CurrentSlice.Bars["BTCUSD"].Close:N3}$/Btc; Portfolio: {Portfolio.CashBook[Portfolio.CashBook.AccountCurrency].Amount:N3}$, {Portfolio["BTCUSD"].Quantity}BTCs, Total Value: {Portfolio.TotalPortfolioValue:N3}$, Total Fees: {Portfolio.TotalFees:N3}$";
                //We skip small adjusting orders
                if (orderEvent.AbsoluteFillQuantity * orderEvent.FillPrice > 100)
                {
                    Log($"{message} {endMessage}");
                }


            }

        }

        public override void OnEndOfAlgorithm()
        {
            Log($"{Time} - TotalPortfolioValue: {Portfolio.TotalPortfolioValue}");
            Log($"{Time} - CashBook: {Portfolio.CashBook}");
        }

        public bool CanRunLocally { get; } = true;

        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        public long DataPoints => 12965;

        public int AlgorithmHistoryDataPoints => 240;

    }
}
