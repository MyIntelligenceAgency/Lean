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
using System.Drawing;

namespace QuantConnect.Algorithm.CSharp
{

    // Intègre 3 Moyennes mobiles, un RSI, un Trailing stop et une allocation alternée entre 0.5 et 0.75 du portefeuille.
    public class AlphaStratEmmaCross : QCAlgorithm
    {
        private int FastPeriod = 50;
        private int SlowPeriod = 200;
        private int ThirdPeriod = 100;
        private int RsiPeriod = 14; // Add RSI period

        private decimal UpCrossMargin = 1.001m;
        private decimal DownCrossMargin = 0.999m;
        private int OverboughtThreshold = 70; // RSI overbought threshold
        private int OversoldThreshold = 30; // RSI oversold threshold
        private decimal TrailingStopPercentage = 0.02m; // Trailing stop as a percentage

        private Resolution _resolution = Resolution.Daily;

        public ExponentialMovingAverage Fast;
        public ExponentialMovingAverage Slow;
        public ExponentialMovingAverage Third;
        public RelativeStrengthIndex Rsi; // Declare the RSI

        private Symbol _btcusd;

        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfoliovalueSeriesName = "PortFolioValue";
        private string _FastSeriesName = "FastEMA";
        private string _SlowSeriesName = "SlowEMA";
        private string _ThirdSeriesName = "ThirdEMA";
        private string _RsiSeriesName = "RSI"; // Add RSI series name

        private bool useHalfAllocation = true; // Variable to toggle allocation

        public override void Initialize()
        {
            this.InitPeriod();

            this.SetWarmUp(TimeSpan.FromDays(365));

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            SetCash(10000);
            var btcSecurity = AddCrypto("BTCUSD", _resolution);

            _btcusd = btcSecurity.Symbol;

            Fast = this.EMA(_btcusd, FastPeriod, _resolution);
            Slow = EMA(_btcusd, SlowPeriod, _resolution);
            Third = EMA(_btcusd, ThirdPeriod, _resolution);
            Rsi = this.RSI(_btcusd, RsiPeriod); // Initialize the RSI

            var stockPlot = new Chart(_ChartName);
            var assetPrice = new Series(_PriceSeriesName, SeriesType.Line, "$", Color.Blue);
            var portFolioValue = new Series(_PortfoliovalueSeriesName, SeriesType.Line, "$", Color.Green);
            var fastSeries = new Series(_FastSeriesName, SeriesType.Line, "$", Color.Red);
            var slowSeries = new Series(_SlowSeriesName, SeriesType.Line, "$", Color.Yellow);
            var thirdSeries = new Series(_ThirdSeriesName, SeriesType.Line, "$", Color.Orange);
            var rsiSeries = new Series(_RsiSeriesName, SeriesType.Line, "%", Color.Purple); // Add RSI series

            stockPlot.AddSeries(assetPrice);
            stockPlot.AddSeries(portFolioValue);
            stockPlot.AddSeries(fastSeries);
            stockPlot.AddSeries(slowSeries);
            stockPlot.AddSeries(thirdSeries);
            stockPlot.AddSeries(rsiSeries); // Add RSI series
            AddChart(stockPlot);

            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);
        }

        private void DoPlots()
        {
            Plot(_ChartName, _PriceSeriesName, Securities[_btcusd].Price);
            Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
            Plot(_ChartName, _FastSeriesName, Fast);
            Plot(_ChartName, _SlowSeriesName, Slow);
            Plot(_ChartName, _ThirdSeriesName, Third);
            Plot(_ChartName, _RsiSeriesName, Rsi); // Plot the RSI
        }

        public override void OnData(Slice data)
        {
            if (this.IsWarmingUp || !Fast.IsReady || !Slow.IsReady || !Third.IsReady || !Rsi.IsReady) return;

            // Toggle between 0.5 and 0.75 allocation based on useHalfAllocation flag
            decimal allocation = useHalfAllocation ? 0.5m : 0.75m;

            // Trading logic using EMA crossovers
            if (!Portfolio.Invested && Fast > Slow * UpCrossMargin && Fast > Third && Rsi < OversoldThreshold)
            {
                SetHoldings(_btcusd, allocation);
            }
            else if (Portfolio.Invested && (Fast < Slow * DownCrossMargin || Fast < Third || Rsi > OverboughtThreshold))
            {
                Liquidate(_btcusd);
            }

            // Trailing stop logic
            if (Portfolio.Invested)
            {
                decimal highestPortfolioValue = Portfolio.TotalPortfolioValue;
                decimal trailingStopPrice = highestPortfolioValue * (1 - TrailingStopPercentage);

                if (Portfolio.TotalPortfolioValue < trailingStopPrice)
                {
                    Liquidate(_btcusd);
                    Log($"Trailing stop triggered. Liquidating position at {Securities[_btcusd].Price}");
                }
                Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
            }

            // Toggle between 0.5 and 0.75 allocation for the next iteration
            useHalfAllocation = !useHalfAllocation;
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

                if (orderEvent.AbsoluteFillQuantity * orderEvent.FillPrice > 100)
                {
                    Log($"{message} {endMessage}");
                }
            }
        }

        private void InitPeriod()
        {
            SetStartDate(2010, 1, 1);
            SetEndDate(2019, 1, 1);
        }
    }
}
