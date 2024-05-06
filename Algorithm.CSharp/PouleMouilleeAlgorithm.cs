using System;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Indicators;
using QLNet;
using QuantConnect.Brokerages;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Scheduling;
using System.Security.Cryptography;
using QuantConnect.Parameters;
using static QuantConnect.Messages;
using QuantConnect.Algorithm.CSharp.Benchmarks;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class PouleMouilleeAlgorithm : QCAlgorithm
    {

        [Parameter("macd-fast")]
        public int MacdFast = 12;

        [Parameter("macd-slow")]
        public int MacdSlow = 28;


        [Parameter("macd-signal")]
        public int MacdSignal = 9;

        [Parameter("rsi-period")]
        public int RsiPeriod = 13;


        private MovingAverageConvergenceDivergence _macd;
        private RelativeStrengthIndex _rsi;
        private Symbol _btcusd;
        private const decimal _tolerance = 0.0025m;
        public override void Initialize()
        {
            // Début de test
            InitPeriod();

            // Capital initial
            SetCash(5000);
            //SetCash("BTC", 0m);

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;
            _macd = MACD(_btcusd, MacdFast, MacdSlow, MacdSignal, MovingAverageType.Exponential, Resolution.Daily, Field.Close);
            _rsi = RSI(_btcusd, RsiPeriod, MovingAverageType.Exponential, Resolution.Daily);
        }

        public override void OnData(Slice data)
        {
            if (data == null || !_macd.IsReady || !_rsi.IsReady) return;

            var holdingsQuantity = Securities[_btcusd].Holdings.Quantity;
            var closingPrice = data.Bars[_btcusd].Close;

            if (holdingsQuantity == 0 && _rsi < 70)
            {
                if (_macd > _macd.Signal * (1 + _tolerance))
                {
                    SetHoldings(_btcusd, 1.0);
                    //Debug($"Purchased BTC @{closingPrice}$/Btc");
                    //Debug($"Portfolio: {Portfolio.Cash}$");
                    //Debug($"Quantity: {Portfolio[_btcusd].Quantity}BTCs");
                    //Debug($"Total Value: {Portfolio.TotalPortfolioValue}$");
                    //Debug($"Total Fees: {Portfolio.TotalFees}");
                }
            }
            else if (_rsi >= 70 && holdingsQuantity > 0)
            {
                if (_macd < _macd.Signal)
                {
                    Liquidate(_btcusd);
                    //Debug($"Sold BTC @{closingPrice}$/Btc");
                    //Debug($"Portfolio: {Portfolio.Cash}$");
                    //Debug($"Quantity: {Portfolio[_btcusd].Quantity}BTCs");
                    //Debug($"Total Value: {Portfolio.TotalPortfolioValue}$");
                    //Debug($"Total Fees: {Portfolio.TotalFees}");
                }
            }
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

            //SetStartDate(2022, 5, 1); // début backtest 29410
            //SetEndDate(2024, 02, 11); // fin backtest 29688

            SetStartDate(2011, 04, 07); // début backtest 164
            SetEndDate(2024, 01, 29);


            //SetStartDate(2021, 02, 01); // début backtest 164
            //SetEndDate(2024, 01, 29);


        }
    }
}

