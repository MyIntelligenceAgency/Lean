using System;
using System.Collections.Generic;
using MyIA.Trading.Backtester;
using MyIA.Trading.Converter;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Parameters;

namespace QuantConnect.Algorithm.CSharp;

/// <summary>
/// Show cases how to use the <see cref="EmaCrossAlphaModel"/> 
/// </summary>
public class JsboigeAccordVectorMachinesFrameworkAlgorithm : QCAlgorithm
{

    private Symbol _btcusd;

    // Choix de la résolution des données
    private Resolution _resolution = Resolution.Daily;

    //L'attribut Parameter permet de définir les paramètres dans le fichier de configuration, et d'utiliser une optimisation
    [Parameter("accord-loopBack")]
    public int LoopBack = 30;

    [Parameter("accord-inputSize")]
    public int InputSize = 30;


    private TimeSpan _insightPeriod = TimeSpan.FromDays(365);

    public override void Initialize()
    {

        //Passage en debug-mode
        //this.DebugMode = true;

        // Définition des périodes de backtest (3 périodes sont proposées avec retour de la valeur du btc à l'initial)
        InitPeriod();


        // Periode de warmup pour les indicateurs
        this.SetWarmUp(TimeSpan.FromDays(365));


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

        // define alpha model as a composite of the rsi and ema cross models
        SetAlpha(new JsboigeAccordBtcUsdSVMAlphaModel(this, _btcusd, LoopBack, 
            InputSize, _resolution, _insightPeriod));

        // default models for the rest
        SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModelWithoutExpiry(Resolution.Daily, PortfolioBias.Long));
        SetExecution(new ImmediateExecutionModel());
        SetRiskManagement(new NullRiskManagementModel());
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

}

public class JsboigeAccordBtcUsdSVMAlphaModel : AlphaModel
{

    public int _lookback;

    public int _inputSize;

    TradingTrainingConfig _trainingConfig;



    private Symbol _btcusd;

    // Choix de la résolution des données
    private Resolution _resolution;


    private TimeSpan _insightPeriod;


    private List<Trade> _HistoricalPrices = new();

    private ITradingModel _Model;


    public JsboigeAccordBtcUsdSVMAlphaModel(QCAlgorithm algorithm, Symbol btcusd, int lookback,
        int inputSize, Resolution resolution, TimeSpan insightPeriod) : base()
    {
        _btcusd = btcusd;
       _lookback = lookback;
       _inputSize = inputSize;
       _resolution = resolution;
       _insightPeriod = insightPeriod;



        var resolutionSpan = _resolution.ToTimeSpan();

        var inputSpan = TimeSpan.FromTicks((_inputSize + 1) * resolutionSpan.Ticks);


        algorithm.SetWarmUp(inputSpan);


        _trainingConfig = new TradingTrainingConfig()
        {
            DataConfig = new TradingTrainingDataConfig()
            {
                // Durée de la prédiction
                OutputPrediction = TimeSpan.FromHours(48),
                // Pourcentage de variation du prix à partir duquel on considère que le prix a augmenté ou baissé
                OutputThresold = 10,
                TrainNb = 2000,
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
                SvmModelConfig = new TradingSvmModelConfig()
                {
                    Kernel = KnownKernel.NormalizedPolynomial3,
                    Complexity = 0.023,
                    TrainingTimeout = TimeSpan.FromSeconds(30),
                }
            }
        };


    }



    public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
    {
        var insights = new List<Insight>();
        var currentBar = data.Bars[_btcusd];
        var currentTrade = new Trade()
        {
            UnixTime = currentBar.Time.ToUnixTime(),
            Price = currentBar.Close,
            Amount = currentBar.Volume
        };
        _HistoricalPrices.Add(currentTrade);

        if (!algorithm.IsWarmingUp)
        {
            var result = TrainAndTrade(algorithm);

            switch (result)
            {
                case 1:
                    insights.Add(Insight.Price(_btcusd, _insightPeriod, InsightDirection.Up));
                    break;
                case 2:
                    insights.Add(Insight.Price(_btcusd, _insightPeriod, InsightDirection.Down));
                    break;
            }
        }

        return insights;
    }


    private int TrainAndTrade(QCAlgorithm algorithm)
    {

        var objSample = _trainingConfig.DataConfig.SampleConfig.CreateInput(_HistoricalPrices, _HistoricalPrices.Count - 1);

        if (objSample != null)
        {
            var objInputs = _trainingConfig.DataConfig.GetTrainingData(objSample);

            var objData = new List<TradingTrainingSample>();
            objData.Add(objInputs);
            if (_Model == null)
            {
                double testError = 0;
                _Model = this._trainingConfig.TrainModel(algorithm.Log, ref testError);
            }
            var result = this._Model.Predict(objData);
            return (int)result[0].Output;

        }

        return 0;
    }

    

}
