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

namespace Marus.Metocean
{
    [Serializable]
    public class MarineApiResponse
    {
        public CurrentMarineData current;
    }

    [Serializable]
    public class CurrentMarineData
    {
        public string time;
        public float wave_height;
        public int wave_direction;
        public float wave_period;
        public float wind_wave_height;
        public int wind_wave_direction;
        public float wind_wave_period;
        public float swell_wave_height;
        public int swell_wave_direction;
        public float swell_wave_period;
        public float ocean_current_velocity;
        public int ocean_current_direction;
    }

    /// <summary>
    /// Metocean provider that fetches live real-time oceanographic telemetry from the Open-Meteo Marine API.
    /// Provides sea surface wave conditions (significant wave height, peak period, direction, wind waves, swell)
    /// and ocean current velocity and direction.
    /// Replaces and modularizes the legacy prototype MarineDataFetcher.
    /// </summary>
    [AddComponentMenu("MARUS/Metocean/Providers/Open-Meteo Marine Provider")]
    public class OpenMeteoMarineProvider : RestApiMetoceanProviderBase
    {
        [Header("Live Marine Telemetry (Read Only)")]
        [SerializeField] private CurrentMarineData _liveData;

        public override string ProviderName => "Open-Meteo Marine API";

        /// <summary>
        /// Declares that this provider is authoritative for ocean waves and currents.
        /// </summary>
        public override MetoceanDataFlags ProvidedData => MetoceanDataFlags.Waves | MetoceanDataFlags.OceanCurrent;

        /// <summary>
        /// Read-only snapshot of the raw Open-Meteo marine response payload.
        /// </summary>
        public CurrentMarineData LiveData => _liveData;

        public OpenMeteoMarineProvider()
        {
            _apiUrl = "https://marine-api.open-meteo.com/v1/marine";
            _pollIntervalSeconds = 300.0f; // 5 minutes (standard marine model update resolution)
            _latitude = 43.73;             // Default: Šibenik coastal waters (Adriatic Sea)
            _longitude = 15.89;
        }

        private void Reset()
        {
            _apiUrl = "https://marine-api.open-meteo.com/v1/marine";
            _pollIntervalSeconds = 300.0f;
            _latitude = 43.73;
            _longitude = 15.89;
        }

        protected override string BuildRequestUrl()
        {
            string latStr = _latitude.ToString(CultureInfo.InvariantCulture);
            string lonStr = _longitude.ToString(CultureInfo.InvariantCulture);
            return $"{_apiUrl}?latitude={latStr}&longitude={lonStr}&current=wave_height,wave_direction,wave_period,wind_wave_height,wind_wave_direction,wind_wave_period,swell_wave_height,swell_wave_direction,swell_wave_period,ocean_current_velocity,ocean_current_direction";
        }

        protected override bool TryParseResponse(string responseText, out MetoceanData data)
        {
            data = _currentData;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return false;
            }

            MarineApiResponse response = JsonUtility.FromJson<MarineApiResponse>(responseText);
            if (response == null || response.current == null)
            {
                return false;
            }

            _liveData = response.current;

            // Significant wave height (m), peak period (s), direction (deg)
            var waveData = new WaveData(
                significantWaveHeight: response.current.wave_height,
                peakPeriod: response.current.wave_period,
                direction: response.current.wave_direction
            );

            // Open-Meteo ocean_current_velocity is reported in km/h; convert to SI m/s for MARUS
            float currentSpeedMs = response.current.ocean_current_velocity / 3.6f;
            var currentData = new OceanCurrentData(
                speed: currentSpeedMs,
                direction: response.current.ocean_current_direction,
                depth: 0f
            );

            var ocean = _currentData.Ocean;
            ocean.waves = waveData;
            ocean.current = currentData;

            DateTime timestamp = DateTime.UtcNow;
            if (DateTime.TryParse(response.current.time, out var parsedTime))
            {
                timestamp = parsedTime;
            }

            data = new MetoceanData(ocean, _currentData.Weather, timestamp);
            return true;
        }
    }
}

