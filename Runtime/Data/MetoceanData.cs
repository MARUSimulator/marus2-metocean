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
using Marus.Core;
using UnityEngine;

namespace Marus.Metocean
{
    /// <summary>
    /// Immutable comprehensive snapshot of metocean conditions at a given point in space and time.
    /// </summary>
    [Serializable]
    public struct MetoceanData : IEquatable<MetoceanData>
    {
        [Tooltip("Oceanographic states (waves, currents, sea level, water properties).")]
        public OceanStateData Ocean;

        [Tooltip("Meteorological / atmospheric states (wind, rain, fog, temperature, pressure).")]
        public WeatherStateData Weather;

        [Tooltip("UTC Timestamp when this data snapshot was sampled or observed (represented as ISO 8601 string in Inspector).")]
        public string timestampUtc;

        [Tooltip("Geographic coordinates (WGS84) where this observation was made or simulated.")]
        public GeoPoint location;

        public MetoceanData(OceanStateData ocean, WeatherStateData weather, DateTime? timestamp = null, GeoPoint? location = null)
        {
            Ocean = ocean;
            Weather = weather;
            DateTime ts = timestamp ?? DateTime.UtcNow;
            if (ts.Kind == DateTimeKind.Local)
            {
                ts = ts.ToUniversalTime();
            }
            else if (ts.Kind == DateTimeKind.Unspecified)
            {
                ts = DateTime.SpecifyKind(ts, DateTimeKind.Utc);
            }
            this.timestampUtc = ts.ToString("o");
            this.location = location ?? new GeoPoint(0.0, 0.0, 0.0);
        }

        public DateTime Timestamp
        {
            get
            {
                if (DateTime.TryParse(timestampUtc, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
                {
                    return dt;
                }
                return DateTime.UtcNow;
            }
            set
            {
                DateTime ts = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() :
                              value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value;
                timestampUtc = ts.ToString("o");
            }
        }

        public static MetoceanData Default => new MetoceanData(
            OceanStateData.Default,
            WeatherStateData.Default,
            DateTime.UtcNow,
            new GeoPoint(0.0, 0.0, 0.0)
        );

        public bool Equals(MetoceanData other)
        {
            return Ocean.Equals(other.Ocean) &&
                   Weather.Equals(other.Weather) &&
                   string.Equals(timestampUtc, other.timestampUtc, StringComparison.Ordinal) &&
                   location.latitude.Equals(other.location.latitude) &&
                   location.longitude.Equals(other.location.longitude);
        }

        public override bool Equals(object obj) => obj is MetoceanData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Ocean, Weather, timestampUtc, location.latitude, location.longitude);
    }
}

