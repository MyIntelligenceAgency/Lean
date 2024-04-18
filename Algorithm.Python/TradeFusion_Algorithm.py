<<<<<<< HEAD
<<<<<<<< HEAD:Algorithm.Python/TradeFusion_Algorithm.py
from AlgorithmImports import *

class TradeFusion_Algorithm(QCAlgorithm):
========

class MeanReversionLunchBreakAlpha(QCAlgorithm):
>>>>>>>> 32581ab51c23b93e7d8c011d76ab82e218d9c237:Algorithm.Python/back_testing.py
=======
from AlgorithmImports import *

class TradeFusion_Algorithm(QCAlgorithm):
>>>>>>> 32581ab51c23b93e7d8c011d76ab82e218d9c237

    def Initialize(self):

        self.SetStartDate(2018, 1, 1)
        self.SetEndDate(2020, 2, 2)  # Specify the end date
<<<<<<< HEAD

        self.SetCash(100000)

        # Set zero transaction fees
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(ConstantFeeModel(0)))

        # Use Monthly Data For Simplicity
        self.UniverseSettings.Resolution = Resolution.Month
        self.SetUniverseSelection(CoarseFundamentalUniverseSelectionModel(self.CoarseSelectionFunction))
=======
        self.SetCash(100000)
        
        #self._symbol = self.AddEquity("SPY").Symbol
        #self._btcEur = self.AddCrypto("BTCEUR").Symbol
        
        self.etfs = ['VNQ', 'REET', 'TAO', 'FREL', 'SRET', 'HIPS']
        # self.symbols = [ Symbol.Create(etf, SecurityType.Equity, Market.USA) for etf in self.etfs ]
        self.symbols = [self.AddEquity(ticker, Resolution.Daily).Symbol for ticker in self.etfs]
        
        # Set zero transaction fees
        self.SetSecurityInitializer(lambda security: security.SetFeeModel(InteractiveBrokersFeeModel(10)))

        # Use Hourly Data For Simplicity
        self.UniverseSettings.Resolution = Resolution.Daily
        # self.SetUniverseSelection(FundamentalUniverseSelectionModel(self.CoarseSelectionFunction))
        self.SetUniverseSelection(ManualUniverseSelectionModel(self.symbols))
>>>>>>> 32581ab51c23b93e7d8c011d76ab82e218d9c237

        # Use MeanReversionLunchBreakAlphaModel to establish insights
        self.SetAlpha(MeanReversionLunchBreakAlphaModel())

        # Equally weigh securities in portfolio, based on insights
        self.SetPortfolioConstruction(EqualWeightingPortfolioConstructionModel())

        # Set Immediate Execution Model
        self.SetExecution(ImmediateExecutionModel())

        # Set Null Risk Management Model
        self.SetRiskManagement(NullRiskManagementModel())

    # Sort the data by daily dollar volume and take the top '20' ETFs
<<<<<<< HEAD
    def CoarseSelectionFunction(self, coarse):
        sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)
        filtered = [ x.Symbol for x in sortedByDollarVolume if not x.HasFundamentalData ]
        return filtered[:20]
=======
    # def CoarseSelectionFunction(self, coarse):
    #     sortedByDollarVolume = sorted(coarse, key=lambda x: x.DollarVolume, reverse=True)
    #     filtered = [ x.Symbol for x in sortedByDollarVolume if not x.HasFundamentalData ]
    #     return filtered[:50]

>>>>>>> 32581ab51c23b93e7d8c011d76ab82e218d9c237

class MeanReversionLunchBreakAlphaModel(AlphaModel):
    '''Uses the price return between the close of previous day to 12:00 the day after to
    predict mean-reversion of stock price during lunch break and creates direction prediction
<<<<<<< HEAD
    for insights accordingly.''' 'we are trying to modify the previous parameters into a monthly basis'

    def __init__(self, *args, **kwargs):
        lookback = kwargs['lookback'] if 'lookback' in kwargs else 1
        self.resolution = Resolution.Month
=======
    for insights accordingly.'''

    def __init__(self, *args, **kwargs):
        lookback = kwargs['lookback'] if 'lookback' in kwargs else 1
        self.resolution = Resolution.Daily
>>>>>>> 32581ab51c23b93e7d8c011d76ab82e218d9c237
        self.predictionInterval = Time.Multiply(Extensions.ToTimeSpan(self.resolution), lookback)
        self.symbolDataBySymbol = dict()

    def Update(self, algorithm, data):

<<<<<<< HEAD
=======

>>>>>>> 32581ab51c23b93e7d8c011d76ab82e218d9c237
        for symbol, symbolData in self.symbolDataBySymbol.items():
            if data.Bars.ContainsKey(symbol):
                bar = data.Bars.GetValue(symbol)
                symbolData.Update(bar.EndTime, bar.Close)

<<<<<<< HEAD
        # Check if it's the last day of the month
        last_day_of_month = (algorithm.Time + timedelta(days=1)).month != algorithm.Time.month
        return [] if not last_day_of_month else \
               [x.Insight for x in self.symbolDataBySymbol.values()]
=======
        return [] if algorithm.Time.hour != 12 else \
               [x.Insight for x in self.symbolDataBySymbol.values() \
                if x.Update(algorithm, data)]
>>>>>>> 32581ab51c23b93e7d8c011d76ab82e218d9c237

    def OnSecuritiesChanged(self, algorithm, changes):
        for security in changes.RemovedSecurities:
            self.symbolDataBySymbol.pop(security.Symbol, None)

        # Retrieve price history for all securities in the security universe
        # and update the indicators in the SymbolData object
        symbols = [x.Symbol for x in changes.AddedSecurities]
        history = algorithm.History(symbols, 1, self.resolution)
        if history.empty:
            algorithm.Debug(f"No data on {algorithm.Time}")
            return
        history = history.close.unstack(level = 0)

        for ticker, values in history.iteritems():
            symbol = next((x for x in symbols if str(x) == ticker ), None)
            if symbol in self.symbolDataBySymbol or symbol is None: continue
            self.symbolDataBySymbol[symbol] = self.SymbolData(symbol, self.predictionInterval)
            self.symbolDataBySymbol[symbol].Update(values.index[0], values[0])


    class SymbolData:
        def __init__(self, symbol, period):
            self.symbol = symbol
            self.period = period
            # Mean value of returns for magnitude prediction
            self.meanOfPriceChange = IndicatorExtensions.SMA(RateOfChangePercent(1),3)
            # Price change from close price the previous day
            self.priceChange = RateOfChangePercent(3)

        def Update(self, time, value):
            return self.meanOfPriceChange.Update(time, value) and \
                   self.priceChange.Update(time, value)

        @property
        def Insight(self):
            direction = InsightDirection.Down if self.priceChange.Current.Value > 0 else InsightDirection.Up
            margnitude = abs(self.meanOfPriceChange.Current.Value)
            return Insight.Price(self.symbol, self.period, direction, margnitude, None)
