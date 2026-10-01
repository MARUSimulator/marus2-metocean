// Copyright 2026 Laboratory for Underwater Systems and Technologies (LABUST)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;

namespace Marus.Metocean
{
    [Serializable]
    public class MetNoCompactResponse
    {
        public MetNoProperties properties;
    }

    [Serializable]
    public class MetNoProperties
    {
        public MetNoTimeseries[] timeseries;
    }

    [Serializable]
    public class MetNoTimeseries
    {
        public string time;
        public MetNoData data;
    }

    [Serializable]
    public class MetNoData
    {
        public MetNoInstant instant;
        public MetNoNextPeriod next_1_hours;
        public MetNoNextPeriod next_6_hours;
        public MetNoNextPeriod next_12_hours;
    }

    [Serializable]
    public class MetNoInstant
    {
        public MetNoInstantDetails details;
    }

    [Serializable]
    public class MetNoInstantDetails
    {
        public float air_pressure_at_sea_level;
        public float air_temperature;
        public float cloud_area_fraction;
        public float relative_humidity;
        public float wind_from_direction;
        public float wind_speed;
        public float wind_speed_of_gust;
        public float fog_area_fraction;
    }

    [Serializable]
    public class MetNoNextPeriod
    {
        public MetNoSummary summary;
        public MetNoPeriodDetails details;
    }

    [Serializable]
    public class MetNoSummary
    {
        public string symbol_code;
    }

    [Serializable]
    public class MetNoPeriodDetails
    {
        public float precipitation_amount;
    }

    /// <summary>
    /// Metocean provider ingesting live atmospheric and weather telemetry from MET Norway (Meteorologisk institutt)
    /// Locationforecast 2.0 service (compact endpoint).
    /// <para>
    /// Specification: https://api.met.no/weatherapi/locationforecast/2.0/compact
    /// Supports direct latitude, longitude, and altitude queries as well as scene GeoOrigin binding.
    /// </para>
    /// </summary>
    [AddComponentMenu("MARUS/Metocean/Providers/MET Norway Locationforecast Provider")]
    public class MetNoLocationforecastProvider : RestApiMetoceanProviderBase
    {
        [Header("MET Norway Configuration")]
        [Tooltip("Mandatory User-Agent header identifying the client to MET Norway servers.")]
        [SerializeField] private string _userAgent = "SmartMusselFarmDT/1.0 (https://github.com/labust/smart-mussel-farm-dt)";

        [Tooltip("Terrain / station elevation above sea level in meters.")]
        [SerializeField] private int _altitude = 265;

        [Tooltip("Precipitation rate in mm/hour considered maximum (1.0) rain intensity. Default is 25 mm/hr.")]
        [SerializeField] private float _maxPrecipitationMmHr = 25.0f;

        [Header("Live Weather Telemetry (Read Only)")]
        [SerializeField] private MetNoInstantDetails _liveDetails;
        [SerializeField] private string _symbolCode;
        [SerializeField] private float _precipitationAmount;

        public override string ProviderName => "MET Norway Locationforecast 2.0";

        /// <summary>
        /// Authoritative atmospheric parameters supplied by MET Norway.
        /// </summary>
        public override MetoceanDataFlags ProvidedData =>
            MetoceanDataFlags.AirTemperature |
            MetoceanDataFlags.AtmosphericPressure |
            MetoceanDataFlags.RelativeHumidity |
            MetoceanDataFlags.Wind |
            MetoceanDataFlags.CloudCoverage |
            MetoceanDataFlags.Precipitation;

        public MetNoInstantDetails LiveDetails => _liveDetails;
        public string SymbolCode => _symbolCode;
        public float PrecipitationAmount => _precipitationAmount;
        public int Altitude => _altitude;

        public MetNoLocationforecastProvider()
        {
            _apiUrl = "https://api.met.no/weatherapi/locationforecast/2.0/compact";
            _pollIntervalSeconds = 300.0f; // 5 minutes
            _latitude = 45.3546;          // Rijeka coordinates
            _longitude = 14.4073;
            _altitude = 265;
            _useGeoOrigin = false;
        }

        private void Reset()
        {
            _apiUrl = "https://api.met.no/weatherapi/locationforecast/2.0/compact";
            _pollIntervalSeconds = 300.0f;
            _latitude = 45.3546;
            _longitude = 14.4073;
            _altitude = 265;
            _useGeoOrigin = false;
        }

        protected override void CustomizeWebRequest(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(_userAgent))
            {
                request.SetRequestHeader("User-Agent", _userAgent);
            }
        }

        protected override string BuildRequestUrl()
        {
            // If full URL with query parameters is provided, use it directly
            if (_apiUrl.Contains("?") && (_apiUrl.Contains("lat=") || _apiUrl.Contains("lon=")))
            {
                return _apiUrl;
            }

            string latStr = _latitude.ToString("F4", CultureInfo.InvariantCulture);
            string lonStr = _longitude.ToString("F4", CultureInfo.InvariantCulture);
            return $"{_apiUrl}?lat={latStr}&lon={lonStr}&altitude={_altitude}";
        }

        protected override bool TryParseResponse(string responseText, out MetoceanData data)
        {
            data = _currentData;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return false;
            }

            MetNoCompactResponse response = JsonUtility.FromJson<MetNoCompactResponse>(responseText);
            if (response == null || response.properties == null || response.properties.timeseries == null || response.properties.timeseries.Length == 0)
            {
                return false;
            }

            MetNoTimeseries currentTs = response.properties.timeseries[0];
            if (currentTs.data == null || currentTs.data.instant == null || currentTs.data.instant.details == null)
            {
                return false;
            }

            MetNoInstantDetails d = currentTs.data.instant.details;
            _liveDetails = d;

            string symbol = string.Empty;
            float precip = 0f;
            if (currentTs.data.next_1_hours != null)
            {
                if (currentTs.data.next_1_hours.summary != null)
                {
                    symbol = currentTs.data.next_1_hours.summary.symbol_code ?? string.Empty;
                }
                if (currentTs.data.next_1_hours.details != null)
                {
                    precip = currentTs.data.next_1_hours.details.precipitation_amount;
                }
            }
            else if (currentTs.data.next_6_hours != null)
            {
                if (currentTs.data.next_6_hours.summary != null)
                {
                    symbol = currentTs.data.next_6_hours.summary.symbol_code ?? string.Empty;
                }
                if (currentTs.data.next_6_hours.details != null)
                {
                    precip = currentTs.data.next_6_hours.details.precipitation_amount / 6f;
                }
            }
            _symbolCode = symbol;
            _precipitationAmount = precip;

            float rainIntensity = Mathf.Clamp01(precip / _maxPrecipitationMmHr);
            float cloudCoverage = Mathf.Clamp01(d.cloud_area_fraction / 100.0f);
            CloudCondition cloudCond = MapSymbolToCloudCondition(symbol, cloudCoverage);
            string condText = FormatConditionText(symbol);

            float gust = d.wind_speed_of_gust > 0f ? d.wind_speed_of_gust : d.wind_speed * 1.3f;
            WindData wind = new WindData(d.wind_speed, d.wind_from_direction, gust);

            float fog = d.fog_area_fraction > 0f ? Mathf.Clamp01(d.fog_area_fraction / 100.0f) : 0f;
            float visibility = fog > 0.5f ? 1000f : 15000f;

            WeatherStateData weather = new WeatherStateData(
                airTemperature: d.air_temperature,
                atmosphericPressure: d.air_pressure_at_sea_level > 0f ? d.air_pressure_at_sea_level : 1013.25f,
                relativeHumidity: d.relative_humidity,
                rainIntensity: rainIntensity,
                fogDensity: fog,
                visibility: visibility,
                cloudCoverage: cloudCoverage,
                wind: wind,
                cloudCondition: cloudCond,
                conditionText: condText
            );

            DateTime timestamp = DateTime.UtcNow;
            if (DateTime.TryParse(currentTs.time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedTime))
            {
                timestamp = parsedTime;
            }

            data = new MetoceanData(_currentData.Ocean, weather, timestamp);
            return true;
        }

        private static CloudCondition MapSymbolToCloudCondition(string symbol, float coverage)
        {
            if (string.IsNullOrEmpty(symbol))
            {
                if (coverage < 0.2f) return CloudCondition.Clear;
                if (coverage < 0.5f) return CloudCondition.Sparse;
                if (coverage < 0.8f) return CloudCondition.Cloudy;
                return CloudCondition.Overcast;
            }

            string s = symbol.ToLowerInvariant();
            if (s.Contains("clearsky")) return CloudCondition.Clear;
            if (s.Contains("fair") || s.Contains("partlycloudy")) return CloudCondition.Sparse;
            if (s.Contains("cloudy")) return CloudCondition.Cloudy;
            if (s.Contains("overcast") || s.Contains("fog")) return CloudCondition.Overcast;
            if (s.Contains("rain") || s.Contains("sleet") || s.Contains("snow") || s.Contains("thunder"))
            {
                return s.Contains("heavy") || s.Contains("thunder") ? CloudCondition.Stormy : CloudCondition.Overcast;
            }

            return CloudCondition.Custom;
        }

        private static string FormatConditionText(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return string.Empty;
            string clean = symbol.Replace("_day", "").Replace("_night", "").Replace("_polartwilight", "");
            clean = clean.Replace("_", " ");
            TextInfo ti = CultureInfo.InvariantCulture.TextInfo;
            return ti.ToTitleCase(clean);
        }
    }
}
