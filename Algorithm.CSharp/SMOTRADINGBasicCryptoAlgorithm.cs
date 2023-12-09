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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// The demonstration algorithm shows some of the most common order methods when working with Crypto assets.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class SMOTRADINGBasicCryptoAlgorithm : QCAlgorithm
    {
        private ExponentialMovingAverage _fast;
        private ExponentialMovingAverage _slow;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1); // Set Start Date
            SetEndDate(2018, 12, 31); // Set End Date

            // Although typically real brokerages as GDAX only support a single account currency,
            // here we add both USD and EUR to demonstrate how to handle non-USD account currencies.
            // Set Strategy Cash (USD)
            SetCash(10000);

            
            // Add some coins as initial holdings
            // When connected to a real brokerage, the amount specified in SetCash
            // will be replaced with the amount in your actual account.
            SetCash("BTC", 1m);

            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            // You can uncomment the following line when live trading with GDAX,
            // to ensure limit orders will only be posted to the order book and never executed as a taker (incurring fees).
            // Please note this statement has no effect in backtesting or paper trading.
            // DefaultOrderProperties = new GDAXOrderProperties { PostOnly = true };

            // Find more symbols here: http://quantconnect.com/data
           
            var symbol = AddCrypto("BTCUSD").Symbol;

            // create two moving averages
            _fast = EMA(symbol, 30, Resolution.Minute);
            _slow = EMA(symbol, 60, Resolution.Minute);
        }

        /// <summary>
        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// </summary>
        /// <param name="data">Slice object keyed by symbol containing the stock data</param>
        public override void OnData(Slice data)
        {
            if (Portfolio.CashBook["BTC"].ConversionRate == 0)
            {
                Log($"BTC conversion rate: {Portfolio.CashBook["BTC"].ConversionRate}");

                throw new Exception("Conversion rate is 0");
            }
            if (Time.Hour == 2 && Time.Minute == 0)
            {
                // Submit a buy limit order for BTC at 5% below the current price
                var usdTotal = Portfolio.CashBook["USD"].Amount;
                var limitPrice = Math.Round(Securities["BTCUSD"].Price * 0.95m, 2);
                // use only half of our total USD
                var quantity = usdTotal * 0.5m / limitPrice;
                LimitOrder("BTCUSD", quantity, limitPrice);
            }
            else if (Time.Hour == 11 && Time.Minute == 0)
            {
                // Liquidate our BTC holdings (including the initial holding)
                SetHoldings("BTCUSD", 0m);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug(Time + " " + orderEvent);
        }

        public override void OnEndOfAlgorithm()
        {
            Log($"{Time} - TotalPortfolioValue: {Portfolio.TotalPortfolioValue}");
            Log($"{Time} - CashBook: {Portfolio.CashBook}");
        }
        
    }
}
