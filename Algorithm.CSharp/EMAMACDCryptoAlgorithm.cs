using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;
using System;
using System.Drawing;
using QuantConnect.Parameters;
using System.Security.Cryptography;

namespace QuantConnect
{
    public class EMAMACDCryptoAlgorithm : QCAlgorithm
    {
        [Parameter("fast-period")]
        public int FastPeriod = 12;

        [Parameter("slow-period")]
        public int SlowPeriod = 26;

        [Parameter("ema-period")]
        public int EmaPeriod = 9;

        private Symbol _btcusd;
        private MovingAverageConvergenceDivergence _macd;
        private ExponentialMovingAverage _ema;
        private bool _invested;
        private String ChartName = "Trade Plot";
        private String Price = "Price";
        private String PortfoliovalueSeriesName = "PorteFoliovalue";
        private String FastSeriesName = " Fast Ema";
        private String SlowSeriesName = " Slow Ema";


        public override void Initialize()
        {
            //SetStartDate(2011, 1, 1);
            //SetEndDate(2021, 1, 1);
            SetStartDate(2020, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);
            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;
            _macd = MACD(_btcusd, FastPeriod, SlowPeriod, 9, MovingAverageType.Exponential, Resolution.Daily, Field.Close);
            _ema = EMA(_btcusd, EmaPeriod, Resolution.Daily);
            var tradePlot = new Chart(ChartName);

            tradePlot.AddSeries(new Series(Price, SeriesType.Line, "$", Color.Blue));
            tradePlot.AddSeries(new Series(PortfoliovalueSeriesName, SeriesType.Line, "$", Color.Green));
            tradePlot.AddSeries(new Series(FastSeriesName, SeriesType.Line, "Unit", Color.Red));
            tradePlot.AddSeries(new Series(SlowSeriesName, SeriesType.Line, "Unit", Color.Yellow));

            AddChart(tradePlot);


        }


        public override void OnData(Slice data)
        {
            if (!_macd.IsReady || !_ema.IsReady) return;

            var close = Securities[_btcusd].Close;


            Plot(ChartName, Price, close);
            Plot(ChartName, PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
            Plot(ChartName, FastSeriesName, _macd.Fast);
            Plot(ChartName, SlowSeriesName, _macd.Slow);



            if (!_invested && close > _ema && _macd > _macd.Signal)
            {
                SetHoldings(_btcusd, 1.0);
                _invested = true;
            }
            else if (_invested && (close < _ema || _macd < _macd.Signal))
            {
                Liquidate(_btcusd);
                _invested = false;
            }
        }
    }
}
