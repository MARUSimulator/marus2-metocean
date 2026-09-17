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
    /// World Meteorological Organization (WMO) / Douglas Sea State scale.
    /// </summary>
    public enum SeaState
    {
        CalmGlassy = 0,    // 0 m
        CalmRippled = 1,   // 0 - 0.1 m
        Smooth = 2,        // 0.1 - 0.5 m
        Slight = 3,        // 0.5 - 1.25 m
        Moderate = 4,      // 1.25 - 2.5 m
        Rough = 5,         // 2.5 - 4.0 m
        VeryRough = 6,     // 4.0 - 6.0 m
        High = 7,          // 6.0 - 9.0 m
        VeryHigh = 8,      // 9.0 - 14.0 m
        Phenomenal = 9     // > 14.0 m
    }

    /// <summary>
    /// Represents ocean surface wave spectrum summary data.
    /// </summary>
    [Serializable]
    public struct WaveData : IEquatable<WaveData>
    {
        [Tooltip("Significant wave height (Hs) in meters.")]
        [Min(0f)]
        public float significantWaveHeight;

        [Tooltip("Peak wave period (Tp) in seconds.")]
        [Min(0.1f)]
        public float peakPeriod;

        [Tooltip("Mean wave period (Tm / Tz) in seconds.")]
        [Min(0.1f)]
        public float meanPeriod;

        [Tooltip("Mean wave direction (degrees clockwise from True North waves are coming FROM).")]
        [Range(0f, 360f)]
        public float direction;

        [Tooltip("Peak wavelength in meters. If set to 0, automatically computed using linear deep-water dispersion.")]
        [Min(0f)]
        public float wavelength;

        [Tooltip("Douglas / WMO Sea State designation.")]
        public SeaState seaState;

        public WaveData(float significantWaveHeight, float peakPeriod, float direction, float meanPeriod = 0f, float wavelength = 0f)
        {
            this.significantWaveHeight = Mathf.Max(0f, significantWaveHeight);
            this.peakPeriod = Mathf.Max(0.1f, peakPeriod);
            this.meanPeriod = meanPeriod > 0.05f ? meanPeriod : this.peakPeriod * 0.78f;
            this.direction = (direction % 360f + 360f) % 360f;
            this.wavelength = wavelength > 0.1f ? wavelength : CalculateDeepWaterWavelength(this.peakPeriod);
            this.seaState = CalculateSeaState(this.significantWaveHeight);
        }

        /// <summary>
        /// Direction angle in degrees that the waves are traveling TOWARDS.
        /// </summary>
        public float TravelToDirectionDegrees => (direction + 180f) % 360f;

        /// <summary>
        /// Normalized vector in Unity coordinates (+Z = North, +X = East) pointing in the direction of wave propagation.
        /// </summary>
        public Vector3 ToTravelDirectionVector()
        {
            float rad = TravelToDirectionDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        /// <summary>
        /// Linear deep-water dispersion wavelength: L = (g * T^2) / (2 * pi) ~= 1.5613 * T^2.
        /// </summary>
        public static float CalculateDeepWaterWavelength(float period)
        {
            return 1.5613f * period * period;
        }

        /// <summary>
        /// Deep-water wave phase speed / celerity: c = g * T / (2 * pi) ~= 1.5613 * T (m/s).
        /// </summary>
        public float PhaseSpeed => 1.5613f * peakPeriod;

        /// <summary>
        /// Resolves Douglas sea state from significant wave height.
        /// </summary>
        public static SeaState CalculateSeaState(float hs)
        {
            if (hs <= 0.01f) return SeaState.CalmGlassy;
            if (hs <= 0.1f) return SeaState.CalmRippled;
            if (hs <= 0.5f) return SeaState.Smooth;
            if (hs <= 1.25f) return SeaState.Slight;
            if (hs <= 2.5f) return SeaState.Moderate;
            if (hs <= 4.0f) return SeaState.Rough;
            if (hs <= 6.0f) return SeaState.VeryRough;
            if (hs <= 9.0f) return SeaState.High;
            if (hs <= 14.0f) return SeaState.VeryHigh;
            return SeaState.Phenomenal;
        }

        public static WaveData Default => new WaveData(0.5f, 4.0f, 0f);

        public bool Equals(WaveData other)
        {
            return Mathf.Approximately(significantWaveHeight, other.significantWaveHeight) &&
                   Mathf.Approximately(peakPeriod, other.peakPeriod) &&
                   Mathf.Approximately(meanPeriod, other.meanPeriod) &&
                   Mathf.Approximately(direction, other.direction) &&
                   Mathf.Approximately(wavelength, other.wavelength) &&
                   seaState == other.seaState;
        }

        public override bool Equals(object obj) => obj is WaveData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(significantWaveHeight, peakPeriod, direction, wavelength, seaState);

        public override string ToString() => $"Waves: Hs={significantWaveHeight:F2}m, Tp={peakPeriod:F1}s, Dir={direction:F0}°, SeaState={seaState}";
    }
}

