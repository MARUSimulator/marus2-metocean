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
    /// Holds physical oceanographic states including water properties, waves, and currents.
    /// </summary>
    [Serializable]
    public struct OceanStateData : IEquatable<OceanStateData>
    {
        [Tooltip("Water temperature at the surface in Celsius (°C).")]
        public float waterTemperature;

        [Tooltip("Salinity in Practical Salinity Units (PSU / ppt). Typical ocean salinity is ~35 PSU.")]
        [Min(0f)]
        public float salinity;

        [Tooltip("Water density in kg/m³. Standard seawater density is ~1025 kg/m³.")]
        [Min(900f)]
        public float waterDensity;

        [Tooltip("Sea level vertical elevation offset in meters (e.g. tides, storm surge).")]
        public float seaLevelOffset;

        [Tooltip("Surface wave field characteristics.")]
        public WaveData waves;

        [Tooltip("Ocean current velocity and direction.")]
        public OceanCurrentData current;

        public OceanStateData(
            float waterTemperature = 15.0f,
            float salinity = 35.0f,
            float waterDensity = 1025.0f,
            float seaLevelOffset = 0.0f,
            WaveData waves = default,
            OceanCurrentData current = default)
        {
            this.waterTemperature = waterTemperature;
            this.salinity = Mathf.Max(0f, salinity);
            this.waterDensity = waterDensity > 100f ? waterDensity : CalculateSeawaterDensity(waterTemperature, this.salinity);
            this.seaLevelOffset = seaLevelOffset;
            this.waves = waves.peakPeriod > 0.05f ? waves : WaveData.Default;
            this.current = current;
        }

        /// <summary>
        /// Calculates approximate seawater density at atmospheric pressure using UNESCO 1-atmosphere simplified equation.
        /// </summary>
        public static float CalculateSeawaterDensity(float tempC, float salinityPsu)
        {
            // UNESCO polynomial simplification:
            float rho0 = 999.842594f + 6.793952e-2f * tempC - 9.095290e-3f * tempC * tempC + 1.001685e-4f * Mathf.Pow(tempC, 3);
            float a = 8.24493e-1f - 4.0899e-3f * tempC + 7.6438e-5f * tempC * tempC;
            float b = -5.72466e-3f + 1.0227e-4f * tempC;
            float c = 4.8314e-4f;
            return rho0 + a * salinityPsu + b * Mathf.Pow(salinityPsu, 1.5f) + c * salinityPsu * salinityPsu;
        }

        public static OceanStateData Default => new OceanStateData(
            waterTemperature: 15.0f,
            salinity: 35.0f,
            waterDensity: 1025.0f,
            seaLevelOffset: 0.0f,
            waves: WaveData.Default,
            current: OceanCurrentData.Default
        );

        public bool Equals(OceanStateData other)
        {
            return Mathf.Approximately(waterTemperature, other.waterTemperature) &&
                   Mathf.Approximately(salinity, other.salinity) &&
                   Mathf.Approximately(waterDensity, other.waterDensity) &&
                   Mathf.Approximately(seaLevelOffset, other.seaLevelOffset) &&
                   waves.Equals(other.waves) &&
                   current.Equals(other.current);
        }

        public override bool Equals(object obj) => obj is OceanStateData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(waterTemperature, salinity, waterDensity, seaLevelOffset, waves, current);
    }
}

