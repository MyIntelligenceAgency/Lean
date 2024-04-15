from AlgorithmImports import *

class Mloukhiya(QCAlgorithm):

    def Initialize(self):
        # Initialisation de la période de backtesting, du capital et de la paire de trading
        self.SetStartDate(2018, 10, 1)
        self.SetEndDate(2018, 12, 31)
        self.SetCash(10000)

        # Set Strategy Cash (USD)
        self.SetCash(10000)

        # Set Strategy Cash (EUR)
        # EUR/USD conversion rate will be updated dynamically
        self.SetCash("EUR", 10000)

        # Add some coins as initial holdings
        # When connected to a real brokerage, the amount specified in SetCash
        # will be replaced with the amount in your actual account.
        self.SetCash("BTC", 1)
        self.SetCash("ETH", 5)

        # Set brokerage model for GDAX
        self.SetBrokerageModel(BrokerageName.GDAX, AccountType.Cash)

        # Find more symbols here: http://quantconnect.com/data
        self.AddCrypto("BTCUSD", Resolution.Minute)
        self.AddCrypto("ETHUSD", Resolution.Minute)
        self.AddCrypto("BTCEUR", Resolution.Minute)
        symbol = self.AddCrypto("LTCUSD", Resolution.Minute).Symbol

        # create two moving averages
        ema_fast_period = 20
        ema_slow_period = 50
        self.fast = self.EMA(symbol, ema_fast_period, Resolution.Minute)
        self.slow = self.EMA(symbol, ema_slow_period, Resolution.Minute)

        # Ajouter des niveaux de retracement de Fibonacci
        self.fibonacci_levels = [0, 23.6, 38.2, 50, 61.8, 100]

    def OnData(self, data):
        current_price = self.Securities["BTCUSD"].Price
        fibonacci_retracements = [(level / 100) * current_price for level in self.fibonacci_levels]

        # Calculer la quantité à utiliser (50% du capital)
        capital_to_use = 0.5 * self.Portfolio.CashBook["USD"].Amount

        # Conditions d'achat pour BTCUSD
        if not self.Portfolio.Invested:
            # Condition d'achat basée sur EMA
            ema_condition = self.fast.Current.Value > self.slow.Current.Value

            # Condition d'achat basée sur Retracements de Fibonacci
            fibonacci_condition = any(current_price < retracement for retracement in fibonacci_retracements)

            # Condition d'achat basée sur la stop-loss (5% du prix actuel)
            stop_loss_percentage = 5
            stop_loss_price = current_price * (1 - stop_loss_percentage / 100)
            stop_loss_condition = current_price < stop_loss_price

            # Vérifier au moins une des conditions pour acheter
            if ema_condition or fibonacci_condition or stop_loss_condition:
                # Acheter avec une stop-loss
                quantity_to_buy = capital_to_use / current_price
                self.SetHoldings("BTCUSD", quantity_to_buy, stop_loss_price)
                self.Debug(f"Achat de BTCUSD au prix de {current_price} avec un stop-loss à {stop_loss_price}")

        # Conditions de vente pour BTCUSD
        if self.Portfolio.Invested and self.fast.Current.Value < self.slow.Current.Value:
            # Vous pouvez ajouter des conditions supplémentaires ici pour ajuster les stratégies de sortie
            self.Liquidate("BTCUSD")
            self.Debug(f"Vente de BTCUSD au prix de {current_price} en raison du croisement des moyennes mobiles")

    def OnOrderEvent(self, orderEvent):
        self.Debug("{} {}".format(self.Time, orderEvent.ToString()))

    def OnEndOfAlgorithm(self):
        self.Log("{} - TotalPortfolioValue: {}".format(self.Time, self.Portfolio.TotalPortfolioValue))
        self.Log("{} - CashBook: {}".format(self.Time, self.Portfolio.CashBook))
