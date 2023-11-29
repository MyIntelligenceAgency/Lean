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
using System.Collections.Generic;
using MessagePack.Resolvers;
using MyIA.Trading.Backtester;
using MyIA.Trading.Converter;
using QuantConnect.Orders;
using static QuantConnect.Messages;
using QLNet;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Machine Learning example using Accord VectorMachines Learning
    /// In this example, the algorithm forecasts the direction based on the last 5 days of rate of return
    /// </summary>
    public class JsboigeAdvancedAutoMLAlgorithm : QCAlgorithm
    {
        // Define the size of the data used to train the model
        private const int _inputSize = 32;

        TradingTrainingConfig _trainingConfig;


        private Symbol _btcusd;

        // Choix de la résolution des données
        private Resolution _resolution = Resolution.Daily;


        private List<Trade> _HistoricalPrices = new();

        private ITradingModel _Model;

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
            var btcSecurity = AddCrypto("BTCUSD", _resolution);
            _btcusd = btcSecurity.Symbol;

            // define a manual universe of all the securities we manually registered
            SetUniverseSelection(new ManualUniverseSelectionModel());


            var resolutionSpan = _resolution.ToTimeSpan();

            var inputSpan = TimeSpan.FromTicks((_inputSize + 1) * resolutionSpan.Ticks);


            SetWarmUp(inputSpan);


            _trainingConfig = new TradingTrainingConfig()
            {
                DataConfig = new TradingTrainingDataConfig()
                {
                    // Durée de la prédiction
                    OutputPrediction = TimeSpan.FromDays(10),
                    // Pourcentage de variation du prix à partir duquel on considère que le prix a augmenté ou baissé
                    OutputThresold = 20,
                    TrainNb = 10000,
                    TestNb = 500,
                    TrainStartDate = new DateTime(2011, 01, 01),
                    TrainEndDate = new DateTime(2016, 12, 31),
                    TestStartDate = new DateTime(2017, 01, 01),
                    TestEndDate = new DateTime(2017, 12, 31),
                    PredictionMode = PredictionMode.ThresholdPeak,
                    //Taux de prédictions classifiées minimum
                    ClassifiedRate = 0,
                    SampleConfig = new TradingSampleConfig()
                    {
                        Filename = @"A:\TradingTests\bitstampUSD.bin.7z",
                        //Fenêtre d'échantillonnage
                        StartDate = new DateTime(2011, 01, 01),
                        EndDate = new DateTime(2022, 12, 31),
                        // Nombre d'échantillons à générer
                        NbSamples = 400000,
                        // Résolution minimale des inputs
                        MinSlice = resolutionSpan,
                        // largeur de la fenêtre d'inputs
                        LeftWindow = inputSpan,
                        // Les inputs sont échantillonés à des intervalles de temps réguliers
                        SamplingMode = SamplingMode.Constant,
                        ConstantSliceSpan = resolutionSpan,
                        // Les inputs sont échantillonés à des intervalles de temps se rapprochant exponentiellement
                        //SamplingMode = SamplingMode.Exponential,
                        //TimeCoef = 0.7m,
                    }
                },
                ModelsConfig = new TradingModelsConfig()
                {
                    ModelType = TradingModelType.AutoML,
                    AutomMlModelConfig = new TradingAutoMlModelConfig()
                    {
                        TrainingTimeout = TimeSpan.FromSeconds(30)
                    },
                }
            };
        }

        public override void OnData(Slice data)
        {
            var currentBar = data.Bars[_btcusd];
            var currentTrade = new Trade()
            {
                UnixTime = currentBar.Time.ToUnixTime(),
                Price = currentBar.Close,
                Amount = currentBar.Volume
            };
            _HistoricalPrices.Add(currentTrade);

            if (this.IsWarmingUp) return;

            TrainAndTrade();

        }


        private void TrainAndTrade()
        {

            var objSample = _trainingConfig.DataConfig.SampleConfig.CreateInput(_HistoricalPrices, _HistoricalPrices.Count - 1);

            if (objSample != null)
            {
                var objInputs = _trainingConfig.DataConfig.GetTrainingData(objSample);

                var result = GetResult(objInputs, this.Time);
                switch (result)
                {
                    case 1:
                        SetHoldings(_btcusd, 1);
                        break;
                    case 2:
                        SetHoldings(_btcusd, 0);
                        break;
                }

            }
            
        }

        public virtual int GetResult(TradingTrainingSample objInputs, DateTime time)
        {
            var objData = new List<TradingTrainingSample>();
            objData.Add(objInputs);
            if (_Model == null)
            {
                double testError = 0;
                _Model = this._trainingConfig.TrainModel(Log, ref testError);
                if (_Model == null)
                {
                    throw new ApplicationException($"Model could not be trained, check model exception file in {_trainingConfig.ModelsConfig.CurrentModelConfig.GetModelExceptionFileName(_trainingConfig.DataConfig)}");
                }
            }
            var result = this._Model.Predict(objData);
            return (int)result[0].Output;
        }



        public override void OnOrderEvent(Orders.OrderEvent orderEvent)
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


            SetStartDate(2017, 12, 15); // début backtest 17478
            SetEndDate(2022, 12, 12); // fin backtest 17209

            //SetStartDate(2017, 11, 25); // début backtest 8718
            //SetEndDate(2020, 05, 1); // fin backtest 8832

            //SetStartDate(2021, 1, 1); // début backtest 29410
            //SetEndDate(2023, 10, 20); // fin backtest 29688
        }


    }
}
