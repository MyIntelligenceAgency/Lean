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
    public class TFL : QCAlgorithm
    {
        private RelativeStrengthIndex _rsi;
        private SimpleMovingAverage _smaShort;
        private SimpleMovingAverage _smaLong;
        private BollingerBands _bollingerBands;
        private MovingAverageConvergenceDivergence macd;
        private Symbol _btcusd;

        // Ajout de la gestion des risques
        private decimal _stopLossPercentage = 0.05m; // 10% de stop loss
        private decimal _takeProfitPercentage = 0.30m; // 30% de take profit
        private decimal _entryPrice;

        // Paramètres des bandes de Bollinger, MACD et RSI
        private int _bollingerPeriod = 20;
        private decimal _bollingerK = 2;
        private int macdFastPeriod = 12;
        private int macdSlowPeriod = 26;
        private int macdSignalPeriod = 9;
        private decimal _rsiOverboughtThreshold = 70m;
        private decimal _rsiOversoldThreshold = 30m;
        private int _smaShortPeriod = 5;
        private int _smaLongPeriod = 20;

        // Variables pour le suivi du portefeuille
        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfoliovalueSeriesName = "PortFolioValue";
        private bool _invested;

        public override void Initialize()
        {
            SetStartDate(2014, 10, 3);
            SetEndDate(2023, 3, 22);

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);
            SetCash(200000);

            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;

            _bollingerBands = BB(_btcusd, _bollingerPeriod, _bollingerK, MovingAverageType.Simple, Resolution.Daily);
            macd = MACD(_btcusd, macdFastPeriod, macdSlowPeriod, macdSignalPeriod, MovingAverageType.Simple, Resolution.Daily);
            _rsi = RSI(_btcusd, 14, MovingAverageType.Simple, Resolution.Daily);
            _smaShort = SMA(_btcusd, _smaShortPeriod, Resolution.Daily);
            _smaLong = SMA(_btcusd, _smaLongPeriod, Resolution.Daily);

            // Configuration des graphiques
            var stockPlot = new Chart(_ChartName);
            stockPlot.AddSeries(new Series(_PriceSeriesName, SeriesType.Line, "$", Color.Blue));
            stockPlot.AddSeries(new Series(_PortfoliovalueSeriesName, SeriesType.Line, "$", Color.Green));
            stockPlot.AddSeries(new Series("SMA Short", SeriesType.Line, "$", Color.Yellow));
            stockPlot.AddSeries(new Series("SMA Long", SeriesType.Line, "$", Color.Red));
            AddChart(stockPlot);
        }

        public override void OnData(Slice data)
        {
            if (!_bollingerBands.IsReady || !macd.IsReady || !data.ContainsKey(_btcusd) || !_rsi.IsReady || !_smaShort.IsReady || !_smaLong.IsReady) return;

            var endPrice = data[_btcusd].Close;
            var holdings = Portfolio[_btcusd].Quantity;
            var closePrice = data.Bars[_btcusd].Close;

            bool macdCrossoverBuy = macd > macd.Signal && macd.IsReady;
            bool macdCrossoverSell = macd < macd.Signal && macd.IsReady;

            // Gestion du stop loss et du take profit
            if (_invested)
            {
                if (closePrice <= _entryPrice * (1 - _stopLossPercentage) ||
                    closePrice >= _entryPrice * (1 + _takeProfitPercentage))
                {
                    Liquidate(_btcusd);
                    Debug($"Liquidation BTC: Prix={closePrice}, MACD={macd}");
                    _invested = false;
                }
            }

            // Stratégie de trading
            if (holdings <= 0 && closePrice < _bollingerBands.LowerBand.Current.Value && macdCrossoverBuy)
            {
                SetHoldings(_btcusd, 1.0);
                _entryPrice = closePrice;
                Debug($"Achat BTC: Prix={closePrice}, MACD={macd}");
                _invested = true;
            }
            else if (_invested && closePrice > _bollingerBands.MiddleBand.Current.Value && macdCrossoverSell)
            {
                Liquidate(_btcusd);
                Debug($"Vente BTC: Prix={closePrice}, MACD={macd}");
                _invested = false;
            }

            // Stratégie de trading2
            
            if (_rsi < _rsiOversoldThreshold && _smaShort > _smaLong)
            {
                if (!Portfolio.Invested)
                {
                    SetHoldings(_btcusd, 1.0);
                    Plot("Trade Plot", "Price", endPrice);
                    Debug($"Achat BTC: Prix={endPrice}, RSI={_rsi}");
                }
            }

            // Stratégie de vente (Short)
            if (_rsi > _rsiOverboughtThreshold && _smaShort < _smaLong)
            {
                if (Portfolio.Invested)
                {
                    Liquidate(_btcusd);
                    Plot("Trade Plot", "Price", endPrice);
                    Debug($"Vente BTC: Prix={endPrice}, RSI={_rsi}");
                }
            }

            DoPlots();
        }

        private void DoPlots()
        {
            Plot(_ChartName, _PriceSeriesName, Securities[_btcusd].Price);
            Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
            Plot(_ChartName, "SMA Short", _smaShort);
            Plot(_ChartName, "SMA Long", _smaLong);
        }
    }
}
