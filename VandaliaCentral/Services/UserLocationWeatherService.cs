using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace VandaliaCentral.Services
{
    public class UserLocationWeatherService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private static readonly IReadOnlyDictionary<string, OfficeWeatherLocation> OfficeLocations =
            new Dictionary<string, OfficeWeatherLocation>(StringComparer.OrdinalIgnoreCase)
            {
                ["0000"] = new("Vandalia, OH", 39.8906, -84.1988),
                ["0001"] = new("Vandalia, OH", 39.8906, -84.1988),
                ["0002"] = new("Vandalia, OH", 39.8906, -84.1988),
                ["0003"] = new("Franklin, OH", 39.5589, -84.3041),
                ["0004"] = new("Cincinnati, OH", 39.2655, -84.4210),
                ["0005"] = new("Columbus, OH", 39.9548, -82.9271),
                ["0006"] = new("Florence, KY", 38.9989, -84.6472),
                ["0007"] = new("Lima, OH", 40.7531, -84.1052),
                ["0008"] = new("Cincinnati, OH", 39.1155, -84.3797),
                ["0009"] = new("Cincinnati, OH", 39.2655, -84.4210),
                ["0010"] = new("Columbus, OH", 39.9879, -83.1345),
                ["0011"] = new("Columbus, OH", 39.9879, -83.1345)
            };

        public UserLocationWeatherService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<WeatherSnapshot?> GetWeatherSnapshotAsync(string? officeLocation, CancellationToken cancellationToken = default)
        {
            var officeCode = NormalizeOfficeCode(officeLocation);
            if (officeCode is null || !OfficeLocations.TryGetValue(officeCode, out var office))
            {
                return null;
            }

            var client = _httpClientFactory.CreateClient();
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=temperature_2m,weather_code&temperature_unit=fahrenheit&timezone=auto",
                office.Latitude,
                office.Longitude);

            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var weatherResponse = await JsonSerializer.DeserializeAsync<OpenMeteoResponse>(stream, cancellationToken: cancellationToken);

            if (weatherResponse?.Current is null || string.IsNullOrWhiteSpace(weatherResponse.Timezone))
            {
                return null;
            }

            return new WeatherSnapshot(
                office.DisplayLocation,
                weatherResponse.Timezone,
                weatherResponse.Current.Temperature2m,
                MapWeatherCode(weatherResponse.Current.WeatherCode));
        }



        private static string? NormalizeOfficeCode(string? officeLocation)
        {
            if (string.IsNullOrWhiteSpace(officeLocation))
            {
                return null;
            }

            var trimmed = officeLocation.Trim();
            if (OfficeLocations.ContainsKey(trimmed))
            {
                return trimmed;
            }

            var codeMatch = Regex.Match(trimmed, @"\b(\d{4})\b");
            if (codeMatch.Success && OfficeLocations.ContainsKey(codeMatch.Groups[1].Value))
            {
                return codeMatch.Groups[1].Value;
            }

            if (trimmed.Contains("Vandalia", StringComparison.OrdinalIgnoreCase)) return "0000";
            if (trimmed.Contains("Franklin", StringComparison.OrdinalIgnoreCase)) return "0003";
            if (trimmed.Contains("Florence", StringComparison.OrdinalIgnoreCase)) return "0006";
            if (trimmed.Contains("Lima", StringComparison.OrdinalIgnoreCase)) return "0007";
            if (trimmed.Contains("Cincinnati", StringComparison.OrdinalIgnoreCase)) return "0004";
            if (trimmed.Contains("Columbus", StringComparison.OrdinalIgnoreCase)) return "0005";

            return null;
        }

        public static bool TryResolveTimeZone(string timezoneId, out TimeZoneInfo timeZone)
        {
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
                return true;
            }
            catch
            {
                if (timezoneId.Equals("America/New_York", StringComparison.OrdinalIgnoreCase))
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    return true;
                }

                timeZone = TimeZoneInfo.Local;
                return false;
            }
        }

        private static string MapWeatherCode(int code) => code switch
        {
            0 => "Clear",
            1 => "Mostly Clear",
            2 => "Partly Cloudy",
            3 => "Overcast",
            45 or 48 => "Fog",
            51 or 53 or 55 => "Drizzle",
            56 or 57 => "Freezing Drizzle",
            61 or 63 or 65 => "Rain",
            66 or 67 => "Freezing Rain",
            71 or 73 or 75 => "Snow",
            77 => "Snow Grains",
            80 or 81 or 82 => "Rain Showers",
            85 or 86 => "Snow Showers",
            95 => "Thunderstorm",
            96 or 99 => "Thunderstorm + Hail",
            _ => "Weather Unavailable"
        };

        private sealed record OfficeWeatherLocation(string DisplayLocation, double Latitude, double Longitude);

        private sealed class OpenMeteoResponse
        {
            [JsonPropertyName("timezone")]
            public string? Timezone { get; set; }

            [JsonPropertyName("current")]
            public OpenMeteoCurrent? Current { get; set; }
        }

        private sealed class OpenMeteoCurrent
        {
            [JsonPropertyName("temperature_2m")]
            public double Temperature2m { get; set; }

            [JsonPropertyName("weather_code")]
            public int WeatherCode { get; set; }
        }

        public sealed record WeatherSnapshot(string LocationName, string TimezoneId, double TemperatureF, string Conditions);
    }
}
