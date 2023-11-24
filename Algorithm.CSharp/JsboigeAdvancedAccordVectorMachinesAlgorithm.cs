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

using Accord.MachineLearning.VectorMachines.Learning;
using QuantConnect.Brokerages;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Indicators;
using System;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Machine Learning example using Accord VectorMachines Learning
    /// In this example, the algorithm forecasts the direction based on the last 5 days of rate of return
    /// </summary>
    public class JsboigeAdvancedAccordVectorMachinesAlgorithm : QCAlgorithm
    {
        // Define the size of the data used to train the model
        // It will use _lookback sets with _inputSize members
        // Those members are rate of return
        private const int _lookback = 30;
        private const int _inputSize = 30;




        private RollingWindow<double> _window = new RollingWindow<double>(_inputSize * _lookback + 2);

        private Symbol _btcusd;


        public override void Initialize()
        {

            // Passage en debug-mode
            //this.DebugMode = true;

            // Définition des périodes de backtest (3 périodes sont proposées avec retour de la valeur du btc à l'initial)
            InitPeriod();

            //Capital initial
            SetCash(10000);

            //Definition de notre univers

            // even though we're using a framework algorithm, we can still add our securities
            // using the AddEquity/Forex/Crypto/ect methods and then pass them into a manual
            // universe selection model using Securities.Keys
            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);
            var btcSecurity = AddCrypto("BTCUSD", Resolution.Daily);
            _btcusd = btcSecurity.Symbol;

            // define a manual universe of all the securities we manually registered
            SetUniverseSelection(new ManualUniverseSelectionModel());

            ROC(_btcusd, 1, Resolution.Daily).Updated += (s, e) => _window.Add((double)e.Value);

            Schedule.On(DateRules.Every(DayOfWeek.Monday),
                TimeRules.Midnight,
                TrainAndTrade);

            SetWarmUp(_window.Size, Resolution.Daily);
        }

        private void TrainAndTrade()
        {
            if (!_window.IsReady) return;

            // Convert the rolling window of rate of change into the Learn method
            var returns = new double[_inputSize];
            var targets = new double[_lookback];
            var inputs = new double[_lookback][];

            // Use the sign of the returns to predict the direction
            for (var i = 0; i < _lookback; i++)
            {
                for (var j = 0; j < _inputSize; j++)
                {
                    returns[j] = Math.Sign(_window[i + j + 1]);
                }

                targets[i] = Math.Sign(_window[i]);
                inputs[i] = returns;
            }

            // Train SupportVectorMachine using SetHoldings("SPY", percentage);
            var teacher = new LinearCoordinateDescent();
            teacher.Learn(inputs, targets);

            var svm = teacher.Model;

            // Compute the value for the last rate of change
            var last = (double) Math.Sign(_window[0]);
            var value = svm.Compute(new[] {last});
            if (value.IsNaNOrZero()) return;

            SetHoldings(_btcusd,  Math.Max(Math.Sign(value), 0));
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

            SetStartDate(2018, 01, 30); // début backtest 9971
            SetEndDate(2020, 07, 26); // fin backtest 9945


            //SetStartDate(2017, 12, 15); // début backtest 17478
            //SetEndDate(2022, 12, 12); // fin backtest 17209

            //SetStartDate(2017, 11, 25); // début backtest 8718
            //SetEndDate(2020, 05, 1); // fin backtest 8832

            //SetStartDate(2021, 1, 1); // début backtest 29410
            //SetEndDate(2023, 10, 20); // fin backtest 29688
        }


    }
}
