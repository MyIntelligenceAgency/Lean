using System;
using MyIA.Trading.Backtester;
using QuantConnect.Algorithm.CSharp.Alphas;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Brokerages;
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


    private TimeSpan _trainingTimout = TimeSpan.FromSeconds(30);

    private KnownKernel _kernel = KnownKernel.NormalizedPolynomial3;

    private double _complexity = 0.023;


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
            InputSize, _resolution, _insightPeriod, _trainingTimout, _kernel, _complexity));

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
