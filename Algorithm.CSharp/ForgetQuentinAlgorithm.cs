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

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Demonstration of the parameter system of QuantConnect. Using parameters you can pass the values required into C# algorithms for optimization.
    /// </summary>
    /// <meta name="tag" content="optimization" />
    /// <meta name="tag" content="using quantconnect" />
    public class ForgetQuentinAlgorithm : QCAlgorithm
    {
        //L'attribut Parameter permet de définir les paramètres dans le fichier de configuration, et d'utiliser une optimisation
        public int FastPeriod = 50;

        public int SlowPeriod = 100;


        public decimal UpCrossMargin = 1.001m;

        public decimal DownCrossMargin = 0.999m;



        public ExponentialMovingAverage Fast;
        public ExponentialMovingAverage Slow;
        public RelativeStrengthIndex _rsi;

        private Symbol _btcusd;

        //private string _ChartName = "Trade Plot";
        //private string _PriceSeriesName = "Price";
        //private string _PortfoliovalueSeriesName = "PortFolioValue";


        public override void Initialize()
        {
            //this.InitPeriod();

            this.SetWarmUp(TimeSpan.FromDays(365));

            //SetBenchmark(x => 0);

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            SetCash(10000); // capital
            var btcSecurity = AddCrypto("BTCUSD", Resolution.Daily);

            _btcusd = btcSecurity.Symbol;

            Fast = EMA(_btcusd, FastPeriod, Resolution.Daily);
            Slow = EMA(_btcusd, SlowPeriod, Resolution.Daily);


            _rsi = RSI(_btcusd, 14, MovingAverageType.Simple, Resolution.Daily);


        }


        public override void OnData(Slice data)
        {

            // wait for our indicators to ready
            if (this.IsWarmingUp || !Fast.IsReady || !Slow.IsReady) return;

            var holdings = Portfolio[_btcusd].Quantity;
            var currentPrice = data[_btcusd].Close;

            // Si Fast est supérieur à Slow et que le prix est supérieur à Slow et RSI supérieur à 50, on achète
            if (Fast > Slow && currentPrice > Fast && _rsi > 50)
            {
                if (!Portfolio.Invested)
                {
                    SetHoldings(_btcusd, 1);

                }
            }
            // Si Fast est supérieur à Slow et que le prix est inférieur à Slow et RSI inférieur à 50, on vend
            else if (Fast > Slow && currentPrice < Fast && _rsi < 50)
            {
                if (Portfolio.Invested)
                {
                    Liquidate(_btcusd);

                }
            }
            // Si Fast est inférieur à Slow et que le prix est supérieur à Fast et RSI supérieur à 50, on achète
            else if (Fast < Slow && currentPrice > Fast && _rsi > 50)
            {
                if (!Portfolio.Invested)
                {
                    SetHoldings(_btcusd, 1);

                }
            }
            // Si Fast est inférieur à Slow et que le prix est inférieur à Slow et RSI inférieur à 50, on vend
            else if (Fast < Slow && currentPrice < Slow && _rsi < 50)
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
                //We skip small adjusting orders
                if (orderEvent.AbsoluteFillQuantity * orderEvent.FillPrice > 100)
                {
                    Log($"{message} {endMessage}");
                }


            }

        }


        /*private void InitPeriod()
        {
            SetStartDate(2013, 04, 07); // début backtest 164
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
            SetEndDate(2023, 10, 20); // fin backtest 29688
        }*/



    }
}
