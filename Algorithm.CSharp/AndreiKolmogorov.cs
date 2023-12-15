using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Parameters;

namespace QuantConnect.Algorithm.CSharp
{
    public class AndreiKolmogorov : QCAlgorithm
    {


        // Information Bitstamp

        [Parameter("bitstamp-api-key")]
        public string BitstampApiKey;

        [Parameter("bitstamp-api-secret")]
        public string BitstampApiSecret ;


        [Parameter("macd-fast")]
        public int FastPeriodMacd = 12;

        [Parameter("macd-slow")]
        public int SlowPeriodMacd = 26;

        private MovingAverageConvergenceDivergence _macd;
        private Symbol _btcusd;
        private const decimal _tolerance = 0.0025m;
        private bool _invested;

        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfoliovalueSeriesName = "PortFolioValue";

       
        private string _btcusdSymbol = "BTCUSD";
        private const string BitstampApiUrl = "https://www.bitstamp.net/api/v2/ticker/";

        public override void Initialize()
        {
            SetStartDate(2021, 1, 1);
            SetEndDate(DateTime.Now);
            SetCash(10000);
            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;
            _macd = MACD(_btcusd, FastPeriodMacd, SlowPeriodMacd, 9, MovingAverageType.Exponential, Resolution.Daily, Field.Close);

            var stockPlot = new Chart(_ChartName);
            stockPlot.AddSeries(new Series(_PriceSeriesName, SeriesType.Line, "$", Color.Blue));
            stockPlot.AddSeries(new Series(_PortfoliovalueSeriesName, SeriesType.Line, "$", Color.Green));
            AddChart(stockPlot);

            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);
            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromMinutes(1)), FetchBitstampData);
        }

        private void DoPlots()
        {
            Plot(_ChartName, _PriceSeriesName, Securities[_btcusd].Price);
            Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
        }

        public void OnData(BitstampTicker data)
        {
            if (!_macd.IsReady) return;
            Debug($"Bitstamp Data: {data.Open}, {data.High}, {data.Low}, {data.Last}, {data.Volume}");
            var holdings = Portfolio[_btcusd].Quantity;

            if (holdings <= 0 && _macd > _macd.Signal * (1 + _tolerance))
            {
                SetHoldings(_btcusd, 1.0);
                _invested = true;
            }
            else if (_invested && _macd < _macd.Signal)
            {
                Liquidate(_btcusd);
                _invested = false;
            }
        }

        private void FetchBitstampData()
        {
            try
            {
                string url = BitstampApiUrl + _btcusdSymbol.ToLower() + "/";
                string nonce = GenerateNonce();
                string signature = CreateSignature(BitstampApiKey, BitstampApiSecret, nonce);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Auth", "BITSTAMP " + BitstampApiKey);
                    client.DefaultRequestHeaders.Add("X-Auth-Signature", signature);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                    string postParams = "key=" + BitstampApiKey + "&signature=" + signature + "&nonce=" + nonce;
                    var content = new StringContent(postParams, Encoding.UTF8, "application/x-www-form-urlencoded");
                    var response = client.PostAsync(url, content).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        string responseData = response.Content.ReadAsStringAsync().Result;
                        var data = JsonConvert.DeserializeObject<BitstampTicker>(responseData);
                        // Traitez les données ici comme nécessaire
                        OnData(data);
                    }
                    else
                    {
                        Error($"Error fetching data from Bitstamp: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Error($"HTTP error in FetchBitstampData: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Error($"Error deserializing Bitstamp response: {ex.Message}");
            }
            catch (Exception ex)
            {
                Error($"Unexpected error in FetchBitstampData: {ex.Message}");
            }
        }

        private static string GenerateNonce()
        {
            return DateTime.UtcNow.Ticks.ToString();
        }

        private static string CreateSignature(string apiKey, string apiSecret, string nonce)
        {
            string message = nonce + apiKey + apiSecret;

            using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return BitConverter.ToString(hash).Replace("-", "").ToUpper();
            }
        }

        public class BitstampTicker
        {
            [JsonProperty("open")]
            public decimal Open { get; set; }

            [JsonProperty("high")]
            public decimal High { get; set; }

            [JsonProperty("low")]
            public decimal Low { get; set; }

            [JsonProperty("last")]
            public decimal Last { get; set; }

            [JsonProperty("volume")]
            public decimal Volume { get; set; }
        }
    }
}
