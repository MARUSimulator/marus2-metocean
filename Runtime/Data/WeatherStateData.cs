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
using UnityEngine;

namespace Marus.Metocean
{
    /// <summary>
    /// Holds atmospheric and meteorological states.
    /// </summary>
    [Serializable]
    public struct WeatherStateData : IEquatable<WeatherStateData>
    {
        [Tooltip("Air temperature in Celsius (°C).")]
        public float airTemperature;

        [Tooltip("Atmospheric pressure at sea level in hectopascals / millibars (hPa). Standard is 1013.25 hPa.")]
        [Min(800f)]
        public float atmosphericPressure;

        [Tooltip("Relative humidity percentage (0 to 100%).")]
        [Range(0f, 100f)]
        public float relativeHumidity;

        [Tooltip("Normalized rain precipitation intensity (0 = none, 1 = torrential downpour).")]
        [Range(0f, 1f)]
        public float rainIntensity;

        [Tooltip("Normalized fog density (0 = clear, 1 = dense fog).")]
        [Range(0f, 1f)]
        public float fogDensity;

        [Tooltip("Meteorological optical range / visibility distance in meters. Typically 10,000m+ in clear weather.")]
        [Min(10f)]
        public float visibility;

        [Tooltip("Cloud cover fraction (0 = clear sky, 1 = completely overcast).")]
        [Range(0f, 1f)]
        public float cloudCoverage;

        [Tooltip("Surface wind conditions.")]
        public WindData wind;

        public WeatherStateData(
            float airTemperature = 20.0f,
            float atmosphericPressure = 1013.25f,
            float relativeHumidity = 65.0f,
            float rainIntensity = 0.0f,
            float fogDensity = 0.0f,
            float visibility = 10000.0f,
            float cloudCoverage = 0.2f,
            WindData wind = default)
        {
            this.airTemperature = airTemperature;
            this.atmosphericPressure = Mathf.Max(800f, atmosphericPressure);
            this.relativeHumidity = Mathf.Clamp(relativeHumidity, 0f, 100f);
            this.rainIntensity = Mathf.Clamp01(rainIntensity);
            this.fogDensity = Mathf.Clamp01(fogDensity);
            this.visibility = Mathf.Max(10f, visibility);
            this.cloudCoverage = Mathf.Clamp01(cloudCoverage);
            this.wind = wind;
        }

        public bool IsRaining => rainIntensity > 0.02f;
        public bool IsFoggy => fogDensity > 0.02f || visibility < 1000f;

        /// <summary>
        /// Calculates approximate dry air density in kg/m³: rho = P / (R_specific * T_kelvin).
        /// Standard sea-level dry air density at 15°C is ~1.225 kg/m³.
        /// </summary>
        public float CalculateAirDensity()
        {
            float tKelvin = airTemperature + 273.15f;
            float pPascals = atmosphericPressure * 100f;
            const float rSpecific = 287.058f; // J/(kg*K)
            return pPascals / (rSpecific * tKelvin);
        }

        public static WeatherStateData Default => new WeatherStateData(
            airTemperature: 20.0f,
            atmosphericPressure: 1013.25f,
            relativeHumidity: 65.0f,
            rainIntensity: 0.0f,
            fogDensity: 0.0f,
            visibility: 10000.0f,
            cloudCoverage: 0.2f,
            wind: WindData.Default
        );

        public bool Equals(WeatherStateData other)
        {
            return Mathf.Approximately(airTemperature, other.airTemperature) &&
                   Mathf.Approximately(atmosphericPressure, other.atmosphericPressure) &&
                   Mathf.Approximately(relativeHumidity, other.relativeHumidity) &&
                   Mathf.Approximately(rainIntensity, other.rainIntensity) &&
                   Mathf.Approximately(fogDensity, other.fogDensity) &&
                   Mathf.Approximately(visibility, other.visibility) &&
                   Mathf.Approximately(cloudCoverage, other.cloudCoverage) &&
                   wind.Equals(other.wind);
        }

        public override bool Equals(object obj) => obj is WeatherStateData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(airTemperature, atmosphericPressure, relativeHumidity, rainIntensity, fogDensity, visibility, cloudCoverage, wind);
    }
}

