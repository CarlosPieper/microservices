using CloudWeather.Report.Config;
using CloudWeather.Report.DataAccess;
using CloudWeather.Report.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CloudWeather.Report.BusinessLogic
{
    public interface IWeatherReportAggregator
    {
        public Task<WeatherReport> BuildWeeklyReport(string zip, int days);
    }

    public class WeatherReportAggregator : IWeatherReportAggregator
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<WeatherReportAggregator> _logger;
        private readonly WeatherDataConfig _weatherDataConfig;
        private readonly WeatherReportDbContext _db;

        public WeatherReportAggregator(IHttpClientFactory http, ILogger<WeatherReportAggregator> logger, IOptions<WeatherDataConfig> weatherDataConfig, WeatherReportDbContext db)
        {
            _http = http;
            _logger = logger;
            _weatherDataConfig = weatherDataConfig.Value;
            _db = db;
        }

        public async Task<WeatherReport> BuildWeeklyReport(string zip, int days)
        {
            var httpClient = _http.CreateClient();

            var precipData = await FechPrecipitationData(httpClient, zip, days);
            var totalSnow = GetTotalSnow(precipData);
            var totalRain = GetTotalRain(precipData);
            _logger.LogInformation($"zip: {zip} over last {days} days: total snow: {totalSnow}, rain {totalRain}");

            var tempData = await FechTemperatureData(httpClient, zip, days);
            var averageHighTemp = tempData.Average(x => x.TempHighF);
            var averageLowTemp = tempData.Average(x => x.TempLowF);
            _logger.LogInformation($"zip: {zip} over last {days} days: low temp: {averageLowTemp}, high temp {averageLowTemp}");

            var report = new WeatherReport
            {
                AverageHighF = Math.Round(averageHighTemp, 1),
                AverageLowF = Math.Round(averageLowTemp, 1),
                RainFallTotalInches = totalRain,
                SnowTotalInches = totalSnow,
                ZipCode = zip,
                CreatedOn = DateTime.UtcNow
            };

            _db.Add(report);
            await _db.SaveChangesAsync();

            return report;
        }

        private static decimal GetTotalSnow(IEnumerable<PrecipitationModel> precipData)
        {
            var totalSnow = precipData.Where(x => x.WeatherType == "snow")
                .Sum(x => x.AmountInches);
            return Math.Round(totalSnow, 1);
        }

        private static decimal GetTotalRain(IEnumerable<PrecipitationModel> precipData)
        {
            var totalSnow = precipData.Where(x => x.WeatherType == "rain")
                .Sum(x => x.AmountInches);
            return Math.Round(totalSnow, 1);
        }

        private async Task<List<TemperatureModel>> FechTemperatureData(HttpClient httpClient, string zip, int days)
        {
            var endpoint = BuildTemperatureServiceEndpoint(zip, days);
            var records = await httpClient.GetAsync(endpoint);
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var data = await records.Content.ReadFromJsonAsync<List<TemperatureModel>>(jsonSerializerOptions);
            return data ?? new List<TemperatureModel>();
        }

        private async Task<List<PrecipitationModel>> FechPrecipitationData(HttpClient httpClient, string zip, int days)
        {
            var endpoint = BuildPrecipitationServiceEndpoint(zip, days);
            var records = await httpClient.GetAsync(endpoint);
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var data = await records.Content.ReadFromJsonAsync<List<PrecipitationModel>>(jsonSerializerOptions);
            return data ?? new List<PrecipitationModel>();
        }

        private string BuildTemperatureServiceEndpoint(string zip, int days)
        {
            var protocol = _weatherDataConfig.TempDataProtocol;
            var host = _weatherDataConfig.TempDataHost;
            var port = _weatherDataConfig.TempDataPort;
            return $"{protocol}://{host}:{port}/observation/{zip}?days={days}";
        }

        private string BuildPrecipitationServiceEndpoint(string zip, int days)
        {
            var protocol = _weatherDataConfig.PrecipDataProtocol;
            var host = _weatherDataConfig.PrecipDataHost;
            var port = _weatherDataConfig.PrecipDataPort;
            return $"{protocol}://{host}:{port}/observation/{zip}?days={days}";
        }
    }
}
