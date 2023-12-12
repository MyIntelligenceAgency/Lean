using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;
using System;
using System.Drawing;
using QuantConnect.Orders;

namespace QuantConnect
{
    public class CMM : QCAlgorithm
    {
        private RelativeStrengthIndex _rsi;
        private SimpleMovingAverage _smaShort;
        private SimpleMovingAverage _smaLong;
        private Symbol _btcusd;
        private decimal _rsiOverboughtThreshold = 70m;
        private decimal _rsiOversoldThreshold = 30m;
        private int _smaShortPeriod = 5;
        private int _smaLongPeriod = 20;

        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        //private string _PortfoliovalueSeriesName = "PortFolioValue";
        //private bool _invested;

        public override void Initialize()
        {
            SetStartDate(2014, 10, 3);
            SetEndDate(2023, 3, 22);
            SetCash(200000);

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;

            _rsi = RSI(_btcusd, 14, MovingAverageType.Simple, Resolution.Daily);
            _smaShort = SMA(_btcusd, _smaShortPeriod, Resolution.Daily);
            _smaLong = SMA(_btcusd, _smaLongPeriod, Resolution.Daily);

            // Configure le graphique
            var stockPlot = new Chart(_ChartName);
            stockPlot.AddSeries(new Series(_PriceSeriesName, SeriesType.Line, "$", Color.Blue));
            stockPlot.AddSeries(new Series("SMA Short", SeriesType.Line, "$", Color.Yellow));
            stockPlot.AddSeries(new Series("SMA Long", SeriesType.Line, "$", Color.Red));
            AddChart(stockPlot);
        }

        public override void OnData(Slice data)
        {
            if (!data.ContainsKey(_btcusd) || !_rsi.IsReady || !_smaShort.IsReady || !_smaLong.IsReady)
                return;

            var closePrice = data[_btcusd].Close;

            // Stratégie d'achat (Long)
            if (_rsi < _rsiOversoldThreshold && _smaShort > _smaLong)
            {
                if (!Portfolio.Invested)
                {
                    SetHoldings(_btcusd, 1.0);
                    Plot("Trade Plot", "Price", closePrice);
                    Debug($"Achat BTC: Prix={closePrice}, RSI={_rsi}");
                }
            }

            // Stratégie de vente (Short)
            if (_rsi > _rsiOverboughtThreshold && _smaShort < _smaLong)
            {
                if (Portfolio.Invested)
                {
                    Liquidate(_btcusd);
                    Plot("Trade Plot", "Price", closePrice);
                    Debug($"Vente BTC: Prix={closePrice}, RSI={_rsi}");
                }
            }

            Plot("Trade Plot", "SMA Short", _smaShort);
            Plot("Trade Plot", "SMA Long", _smaLong);
        }
    }
}
