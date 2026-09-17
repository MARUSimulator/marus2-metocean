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

namespace Marus.Metocean
{
    /// <summary>
    /// Bitmask flags identifying which metocean data fields or capabilities a provider supplies.
    /// Used for non-destructive merging, data fusion, and provider composition in MARUS.
    /// </summary>
    [Flags]
    public enum MetoceanDataFlags
    {
        None = 0,

        /// <summary>Surface wind velocity, direction, and gusting (WindData).</summary>
        Wind = 1 << 0,

        /// <summary>Surface waves (significant wave height, peak period, direction, wavelength, sea state).</summary>
        Waves = 1 << 1,

        /// <summary>Ocean currents (speed, direction, depth).</summary>
        OceanCurrent = 1 << 2,

        /// <summary>Seawater properties (water temperature, salinity, water density, sea level / tide offset).</summary>
        WaterProperties = 1 << 3,

        /// <summary>Ambient air temperature (°C).</summary>
        AirTemperature = 1 << 4,

        /// <summary>Atmospheric barometric pressure at sea level (hPa).</summary>
        AtmosphericPressure = 1 << 5,

        /// <summary>Relative humidity percentage (0 - 100%).</summary>
        RelativeHumidity = 1 << 6,

        /// <summary>Rain or precipitation intensity (0 - 1).</summary>
        Precipitation = 1 << 7,

        /// <summary>Atmospheric fog density and optical visibility distance (meters).</summary>
        VisibilityAndFog = 1 << 8,

        /// <summary>Cloud coverage fraction (0 - 1).</summary>
        CloudCoverage = 1 << 9,

        // High-level aggregates:
        /// <summary>All oceanographic data (Waves, OceanCurrent, WaterProperties).</summary>
        AllOcean = Waves | OceanCurrent | WaterProperties,

        /// <summary>All atmospheric and weather data (Wind, AirTemperature, AtmosphericPressure, RelativeHumidity, Precipitation, VisibilityAndFog, CloudCoverage).</summary>
        AllWeather = Wind | AirTemperature | AtmosphericPressure | RelativeHumidity | Precipitation | VisibilityAndFog | CloudCoverage,

        /// <summary>All supported metocean parameters.</summary>
        All = AllOcean | AllWeather
    }

    /// <summary>
    /// Helper utilities for merging partial metocean data snapshots without overwriting unspecified parameters.
    /// </summary>
    public static class MetoceanDataMerger
    {
        /// <summary>
        /// Merges only the fields specified by <paramref name="flagsToApply"/> from <paramref name="incoming"/> into <paramref name="baseData"/>.
        /// Preserves all other unflagged fields from <paramref name="baseData"/>.
        /// </summary>
        public static MetoceanData Merge(MetoceanData baseData, MetoceanData incoming, MetoceanDataFlags flagsToApply)
        {
            var ocean = baseData.Ocean;
            var weather = baseData.Weather;

            // Ocean flags
            if ((flagsToApply & MetoceanDataFlags.Waves) != 0)
            {
                ocean.waves = incoming.Ocean.waves;
            }

            if ((flagsToApply & MetoceanDataFlags.OceanCurrent) != 0)
            {
                ocean.current = incoming.Ocean.current;
            }

            if ((flagsToApply & MetoceanDataFlags.WaterProperties) != 0)
            {
                ocean.waterTemperature = incoming.Ocean.waterTemperature;
                ocean.salinity = incoming.Ocean.salinity;
                ocean.waterDensity = incoming.Ocean.waterDensity;
                ocean.seaLevelOffset = incoming.Ocean.seaLevelOffset;
            }

            // Weather flags
            if ((flagsToApply & MetoceanDataFlags.Wind) != 0)
            {
                weather.wind = incoming.Weather.wind;
            }

            if ((flagsToApply & MetoceanDataFlags.AirTemperature) != 0)
            {
                weather.airTemperature = incoming.Weather.airTemperature;
            }

            if ((flagsToApply & MetoceanDataFlags.AtmosphericPressure) != 0)
            {
                weather.atmosphericPressure = incoming.Weather.atmosphericPressure;
            }

            if ((flagsToApply & MetoceanDataFlags.RelativeHumidity) != 0)
            {
                weather.relativeHumidity = incoming.Weather.relativeHumidity;
            }

            if ((flagsToApply & MetoceanDataFlags.Precipitation) != 0)
            {
                weather.rainIntensity = incoming.Weather.rainIntensity;
            }

            if ((flagsToApply & MetoceanDataFlags.VisibilityAndFog) != 0)
            {
                weather.fogDensity = incoming.Weather.fogDensity;
                weather.visibility = incoming.Weather.visibility;
            }

            if ((flagsToApply & MetoceanDataFlags.CloudCoverage) != 0)
            {
                weather.cloudCoverage = incoming.Weather.cloudCoverage;
            }

            // Location: adopt incoming location if it has non-zero coordinates
            var location = (incoming.location.latitude != 0.0 || incoming.location.longitude != 0.0)
                ? incoming.location
                : baseData.location;

            return new MetoceanData(ocean, weather, incoming.Timestamp, location);
        }
    }
}

