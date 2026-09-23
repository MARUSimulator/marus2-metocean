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
    public enum MetoceanPreset
    {
        Custom,
        Calm,
        ModerateBreeze,
        RoughSea,
        Stormy,
        DenseFog,
        HeavyRain
    }

    public enum CloudCondition
    {
        Custom,
        Clear,
        Sparse,
        Cloudy,
        Overcast,
        Stormy
    }

    /// <summary>
    /// Manual/Constant provider that exposes configurable metocean values in the Unity Inspector.
    /// Provides built-in presets and real-time tweaking during editor and play modes.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("MARUS/Metocean/Constant Metocean Provider")]
    public class ConstantMetoceanProvider : MetoceanProviderBase
    {
        [Header("Presets")]
        [SerializeField] private MetoceanPreset _preset = MetoceanPreset.ModerateBreeze;

        [Header("Wind")]
        [SerializeField] private WindData _wind = new WindData(5.5f, 45f, 7.0f);

        [Header("Waves")]
        [SerializeField] private WaveData _waves = new WaveData(1.2f, 5.0f, 45f);

        [Header("Ocean Current")]
        [SerializeField] private OceanCurrentData _current = new OceanCurrentData(0.4f, 90f, 0f);

        [Header("Water Properties")]
        [SerializeField] private float _waterTemperature = 16.0f;
        [SerializeField] private float _salinity = 35.0f;
        [SerializeField] private float _waterDensity = 1025.0f;
        [SerializeField] private float _seaLevelOffset = 0.0f;

        [Header("Atmosphere & Weather")]
        [SerializeField] private float _airTemperature = 19.0f;
        [SerializeField] private float _atmosphericPressure = 1013.25f;
        [Range(0f, 100f)]
        [SerializeField] private float _relativeHumidity = 65.0f;
        [Range(0f, 1f)]
        [SerializeField] private float _rainIntensity = 0.0f;
        [Range(0f, 1f)]
        [SerializeField] private float _fogDensity = 0.0f;
        [SerializeField] private float _visibility = 10000.0f;

        [Tooltip("Standard cloud condition preset (Clear, Sparse, Cloudy, Overcast, Stormy, Custom).")]
        [SerializeField] private CloudCondition _cloudCondition = CloudCondition.Sparse;

        [Range(0f, 1f)]
        [Tooltip("Fractional cloud coverage (0 = clear sky, 1 = completely overcast).")]
        [SerializeField] private float _cloudCoverage = 0.25f;

        public override string ProviderName => "Constant / Manual Provider";

        private void Awake()
        {
            ApplyCurrentValues();
        }

        protected override void OnEnable()
        {
            ApplyCurrentValues();
            base.OnEnable();
        }

        private void OnValidate()
        {
            if (_preset != MetoceanPreset.Custom)
            {
                ApplyPresetData(_preset);
            }
            else
            {
                ApplyCloudCondition(_cloudCondition);
            }
            ApplyCurrentValues();
        }

        /// <summary>
        /// Applies one of the standard meteorological/oceanographic presets.
        /// </summary>
        public void ApplyPreset(MetoceanPreset preset)
        {
            _preset = preset;
            ApplyPresetData(preset);
            ApplyCurrentValues();
        }

        private void ApplyCloudCondition(CloudCondition condition)
        {
            switch (condition)
            {
                case CloudCondition.Clear:
                    _cloudCoverage = 0.0f;
                    break;
                case CloudCondition.Sparse:
                    _cloudCoverage = 0.2f;
                    break;
                case CloudCondition.Cloudy:
                    _cloudCoverage = 0.5f;
                    break;
                case CloudCondition.Overcast:
                    _cloudCoverage = 0.85f;
                    break;
                case CloudCondition.Stormy:
                    _cloudCoverage = 0.95f;
                    _rainIntensity = Mathf.Max(_rainIntensity, 0.5f);
                    break;
                case CloudCondition.Custom:
                default:
                    break;
            }
        }

        private void ApplyPresetData(MetoceanPreset preset)
        {
            switch (preset)
            {
                case MetoceanPreset.Calm:
                    _wind = new WindData(1.0f, 0f, 1.5f);
                    _waves = new WaveData(0.1f, 3.0f, 0f);
                    _current = new OceanCurrentData(0.1f, 0f);
                    _rainIntensity = 0.0f;
                    _fogDensity = 0.0f;
                    _visibility = 15000f;
                    _cloudCondition = CloudCondition.Clear;
                    _cloudCoverage = 0.0f;
                    break;

                case MetoceanPreset.ModerateBreeze:
                    _wind = new WindData(6.5f, 60f, 8.5f);
                    _waves = new WaveData(1.25f, 5.2f, 60f);
                    _current = new OceanCurrentData(0.4f, 80f);
                    _rainIntensity = 0.0f;
                    _fogDensity = 0.0f;
                    _visibility = 10000f;
                    _cloudCondition = CloudCondition.Sparse;
                    _cloudCoverage = 0.25f;
                    break;

                case MetoceanPreset.RoughSea:
                    _wind = new WindData(14.0f, 220f, 18.0f);
                    _waves = new WaveData(3.5f, 7.5f, 220f);
                    _current = new OceanCurrentData(0.8f, 210f);
                    _rainIntensity = 0.2f;
                    _fogDensity = 0.05f;
                    _visibility = 6000f;
                    _cloudCondition = CloudCondition.Cloudy;
                    _cloudCoverage = 0.6f;
                    break;

                case MetoceanPreset.Stormy:
                    _wind = new WindData(24.0f, 290f, 32.0f);
                    _waves = new WaveData(6.5f, 10.0f, 290f);
                    _current = new OceanCurrentData(1.5f, 280f);
                    _rainIntensity = 0.85f;
                    _fogDensity = 0.2f;
                    _visibility = 2000f;
                    _cloudCondition = CloudCondition.Stormy;
                    _cloudCoverage = 0.95f;
                    _seaLevelOffset = 0.5f;
                    break;

                case MetoceanPreset.DenseFog:
                    _wind = new WindData(2.0f, 180f, 3.0f);
                    _waves = new WaveData(0.3f, 4.0f, 180f);
                    _current = new OceanCurrentData(0.2f, 180f);
                    _rainIntensity = 0.05f;
                    _fogDensity = 0.85f;
                    _visibility = 200f;
                    _cloudCondition = CloudCondition.Overcast;
                    _cloudCoverage = 0.9f;
                    break;

                case MetoceanPreset.HeavyRain:
                    _wind = new WindData(12.0f, 120f, 16.0f);
                    _waves = new WaveData(2.0f, 6.0f, 120f);
                    _current = new OceanCurrentData(0.5f, 110f);
                    _rainIntensity = 0.95f;
                    _fogDensity = 0.4f;
                    _visibility = 1500f;
                    _cloudCondition = CloudCondition.Stormy;
                    _cloudCoverage = 1.0f;
                    break;

                case MetoceanPreset.Custom:
                default:
                    break;
            }
        }

        private void ApplyCurrentValues()
        {
            var ocean = new OceanStateData(
                waterTemperature: _waterTemperature,
                salinity: _salinity,
                waterDensity: _waterDensity,
                seaLevelOffset: _seaLevelOffset,
                waves: _waves,
                current: _current
            );

            var weather = new WeatherStateData(
                airTemperature: _airTemperature,
                atmosphericPressure: _atmosphericPressure,
                relativeHumidity: _relativeHumidity,
                rainIntensity: _rainIntensity,
                fogDensity: _fogDensity,
                visibility: _visibility,
                cloudCoverage: _cloudCoverage,
                wind: _wind
            );

            NotifyDataUpdated(new MetoceanData(ocean, weather));
        }

        // Public setters for runtime programmatic control
        public void SetWind(float speed, float direction, float gust = 0f)
        {
            _preset = MetoceanPreset.Custom;
            _wind = new WindData(speed, direction, gust);
            ApplyCurrentValues();
        }

        public void SetWaves(float hs, float tp, float direction)
        {
            _preset = MetoceanPreset.Custom;
            _waves = new WaveData(hs, tp, direction);
            ApplyCurrentValues();
        }

        public void SetCurrent(float speed, float direction, float depth = 0f)
        {
            _preset = MetoceanPreset.Custom;
            _current = new OceanCurrentData(speed, direction, depth);
            ApplyCurrentValues();
        }

        public void SetRain(float intensity)
        {
            _preset = MetoceanPreset.Custom;
            _rainIntensity = Mathf.Clamp01(intensity);
            ApplyCurrentValues();
        }

        public void SetFog(float density, float visibilityMeters = -1f)
        {
            _preset = MetoceanPreset.Custom;
            _fogDensity = Mathf.Clamp01(density);
            if (visibilityMeters > 0f) _visibility = visibilityMeters;
            ApplyCurrentValues();
        }
    }
}

