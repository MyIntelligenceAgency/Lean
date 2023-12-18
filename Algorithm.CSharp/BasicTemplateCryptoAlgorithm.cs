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
    public class BasicTemplateCryptoAlgorithm : QCAlgorithm
    {
        private IchimokuKinkoHyo ichimoku;


        public override void Initialize()
        {
            SetStartDate(2018, 4, 4); 
            SetEndDate(2018, 4, 4); 

            SetCash(10000);

            SetCash("EUR", 10000);

            SetCash("ETH", 5m);

            SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash);

            AddCrypto("BTCUSD");
            AddCrypto("ETHUSD");
            AddCrypto("BTCEUR");
            var symbol = AddCrypto("LTCUSD").Symbol;
            ichimoku = new IchimokuKinkoHyo("Ichimoku", 9, 26, 26, 52, 26, 26);
            RegisterIndicator(symbol, ichimoku, Resolution.Daily);

        }

        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey("ETHUSD"))
            {
                return; 
            }

            if (!ichimoku.IsReady)

            {
                return;
            }

            var ethPrice = Securities["ETHUSD"].Price;
            var ethHoldings = Portfolio["ETHUSD"].Quantity;
            var chikouSpan = ichimoku.Chikou;

            if (ichimoku.Chikou > ichimoku.Kijun &&
                ethPrice > ichimoku.SenkouA &&
                ethPrice > ichimoku.SenkouB &&
                chikouSpan > Securities["ETHUSD"].Price && 
                ethHoldings <= 0) 
            {
                SetHoldings("ETHUSD", 1.0); 
            }

            if (ichimoku.Tenkan < ichimoku.Kijun &&
                ethPrice < ichimoku.SenkouA &&
                ethPrice < ichimoku.SenkouB &&
                chikouSpan < Securities["ETHUSD"].Price && 
                ethHoldings > 0) 
            {
                Liquidate("ETHUSD"); 
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

        public bool CanRunLocally { get; } = true;

        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        public long DataPoints => 12965;

        public int AlgorithmHistoryDataPoints => 240;

    }
}
