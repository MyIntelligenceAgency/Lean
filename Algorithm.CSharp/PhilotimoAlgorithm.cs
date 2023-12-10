using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;
using System;
using QuantConnect.Parameters;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class PhilotimoAlgorithm : QCAlgorithm
    {
        [Parameter("fast-period")]
        public int FastPeriod = 12;

        [Parameter("slow-period")]
        public int SlowPeriod = 26;

        private Symbol _btcusd;
        private MovingAverageConvergenceDivergence _macd;
        private bool _invested;

        private FibonacciRetracement _fibonacci;
        private decimal _lastHigh;
        private decimal _lastLow;

        public override void Initialize()
        {
            InitPeriod();
            SetCash(10000);
            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;
            _macd = MACD(_btcusd, FastPeriod, SlowPeriod, 9, MovingAverageType.Exponential, Resolution.Daily, Field.Close);

            _fibonacci = new FibonacciRetracement();
            _lastHigh = 0m;
            _lastLow = decimal.MaxValue;
        }

        public override void OnData(Slice data)
        {
            if (!_macd.IsReady) return;

            var close = Securities[_btcusd].Close;
            UpdateFibonacciLevels(close);

            if (!_invested && close > _fibonacci.SupportLevel1 && _macd > _macd.Signal)
            {
                SetHoldings(_btcusd, 1.0);
                _invested = true;
            }
            else if (_invested && close < _fibonacci.ResistanceLevel1 && _macd < _macd.Signal)
            {
                Liquidate(_btcusd);
                _invested = false;
            }
        }

        private void UpdateFibonacciLevels(decimal close)
        {
            _lastHigh = Math.Max(_lastHigh, close);
            _lastLow = Math.Min(_lastLow, close);

            _fibonacci.Calculate(_lastHigh, _lastLow);
        }

        public class FibonacciRetracement
        {
            public decimal SupportLevel1 { get; private set; }
            public decimal ResistanceLevel1 { get; private set; }

            public void Calculate(decimal high, decimal low)
            {
                var range = high - low;
                SupportLevel1 = low - 0.382m * range; // Adjusted for support
                ResistanceLevel1 = high + 0.382m * range; // Adjusted for resistance
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


    }
}
