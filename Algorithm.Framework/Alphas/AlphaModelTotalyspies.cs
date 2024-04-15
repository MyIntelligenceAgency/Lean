using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Algorithm.Framework.Alphas;
using System;
using System.Collections.Generic;
using QuantConnect.Data.UniverseSelection;
using QuantConnect;

public class MacroeconomicAlphaModel : AlphaModel
{
    private SimpleMovingAverage _sma;
    private decimal _previousGdpValue;
    private const int _rollingWindowPeriod = 30; // Nombre de jours pour la moyenne mobile

    public MacroeconomicAlphaModel()
    {
        _sma = new SimpleMovingAverage(_rollingWindowPeriod);
        _previousGdpValue = 0m; // Valeur initiale pour le PIB simulé
    }

    public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
    {
        // Simuler une donnée macroéconomique (par exemple, variation du PIB)
        decimal currentGdpValue = GetSimulatedGdpValue();

        // Mettre à jour la moyenne mobile avec la valeur actuelle du PIB
        _sma.Update(algorithm.Time, currentGdpValue);

        // Générer un insight en fonction de la tendance du PIB
        if (currentGdpValue > _previousGdpValue && currentGdpValue > _sma)
        {
            _previousGdpValue = currentGdpValue;
            yield return Insight.Price("SPY", TimeSpan.FromDays(1), InsightDirection.Up);
        }
        else if (currentGdpValue < _previousGdpValue && currentGdpValue < _sma)
        {
            _previousGdpValue = currentGdpValue;
            yield return Insight.Price("SPY", TimeSpan.FromDays(1), InsightDirection.Down);
        }
    }

    private decimal GetSimulatedGdpValue()
    {
        // Simuler la valeur du PIB ici
        // Dans une application réelle, vous devriez récupérer la valeur réelle du PIB
        Random random = new Random();
        return (decimal)random.NextDouble();
    }

    public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
    {
        // Gérer les changements dans les titres souscrits si nécessaire
    }

    public class MyCustomData : BaseData
    {
        public decimal MyDataPoint { get; set; }

        public override SubscriptionDataSource GetSource(SubscriptionDataConfig config, DateTime date, bool isLiveMode)
        {
            var file = "C:\\Users\\maxim\\Downloads\\GDP.csv"; 
            return new SubscriptionDataSource(file, SubscriptionTransportMedium.LocalFile);
        }

        public override BaseData Reader(SubscriptionDataConfig config, string line, DateTime date, bool isLiveMode)
        {
            var data = new MyCustomData();
            try
            {
                var parts = line.Split(',');
                data.Time = Convert.ToDateTime(parts[0]);
                data.MyDataPoint = Convert.ToDecimal(parts[1]);
                data.Symbol = config.Symbol;
            }
            catch { /* Gestion des erreurs */ }

            return data;
        }
    }

}
