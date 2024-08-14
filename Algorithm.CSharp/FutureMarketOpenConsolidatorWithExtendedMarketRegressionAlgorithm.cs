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
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Regression algorithm using a consolidator to check GetNextMarketClose() and GetNextMarketOpen()
    /// are returning the correct market close and open times, when extended market hours are used
    /// </summary>
    public class FutureMarketOpenConsolidatorWithExtendedMarketRegressionAlgorithm : FutureMarketOpenConsolidatorRegressionAlgorithm
    {
        protected override bool ExtendedMarketHours => true;
        protected override List<DateTime> ExpectedOpens => new List<DateTime>(){
            new DateTime(2013, 10, 06, 18, 0, 0), // Sunday
            new DateTime(2013, 10, 07, 18, 0, 0),
            new DateTime(2013, 10, 08, 18, 0, 0),
            new DateTime(2013, 10, 09, 18, 0, 0),
            new DateTime(2013, 10, 10, 18, 0, 0),
            new DateTime(2013, 10, 13, 18, 0, 0),
        };
        protected override List<DateTime> ExpectedCloses => new List<DateTime>(){
            new DateTime(2013, 10, 07, 17, 0, 0),
            new DateTime(2013, 10, 08, 17, 0, 0),
            new DateTime(2013, 10, 09, 17, 0, 0),
            new DateTime(2013, 10, 10, 17, 0, 0),
            new DateTime(2013, 10, 11, 17, 0, 0),
            new DateTime(2013, 10, 14, 17, 0, 0),
        };

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public override List<Language> Languages { get; } = new() { Language.CSharp };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public override long DataPoints => 103816;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public override Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Start Equity", "100000"},
            {"End Equity", "100000"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-3.108"},
            {"Tracking Error", "0.163"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}
