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
using Marus.Utils;
using UnityEngine;

namespace Marus.Metocean
{
    /// <summary>
    /// Metocean singleton: centralized authority and query hub for all oceanographic and weather states in MARUS2.
    /// Provides data access for physics, sensors, graphics (Crest Ocean), and environment adapters.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("MARUS/Metocean/Metocean Manager")]
    public class Metocean : Singleton<Metocean>
    {
        [Tooltip("Active data provider supplying metocean states.")]
        [SerializeField] private MetoceanProviderBase _activeProvider;

        [Header("State Cache (Read Only)")]
        [SerializeField] private MetoceanData _lastCachedData = MetoceanData.Default;

        // Events
        public event Action<MetoceanData> OnMetoceanUpdated;
        public event Action<WindData> OnWindUpdated;
        public event Action<WaveData> OnWavesUpdated;
        public event Action<OceanCurrentData> OnCurrentUpdated;
        public event Action<WeatherStateData> OnWeatherUpdated;

        private IMetoceanProvider _currentSubscribedProvider;

        public IMetoceanProvider ActiveProvider => _activeProvider;

        #region Public Data Accessors

        /// <summary>
        /// Current comprehensive metocean data snapshot.
        /// </summary>
        public MetoceanData CurrentData => _activeProvider != null ? _activeProvider.CurrentData : _lastCachedData;

        /// <summary>
        /// Current surface wind conditions.
        /// </summary>
        public WindData Wind => CurrentData.Weather.wind;

        /// <summary>
        /// Current ocean current velocity and direction.
        /// </summary>
        public OceanCurrentData Current => CurrentData.Ocean.current;

        /// <summary>
        /// Current sea surface wave spectrum summary.
        /// </summary>
        public WaveData Waves => CurrentData.Ocean.waves;

        /// <summary>
        /// Current ocean state (water density, temperature, salinity, waves, current).
        /// </summary>
        public OceanStateData Ocean => CurrentData.Ocean;

        /// <summary>
        /// Current meteorological state (wind, rain, fog, temperature, pressure).
        /// </summary>
        public WeatherStateData Weather => CurrentData.Weather;

        // Convenient scalar / vector properties
        public Vector3 WindVelocity => Wind.ToVelocityVector();
        public float WindSpeed => Wind.speed;
        public Vector3 CurrentVelocity => Current.ToVelocityVector();
        public float CurrentSpeed => Current.speed;
        public float WaveHeight => Waves.significantWaveHeight;
        public float WavePeriod => Waves.peakPeriod;
        public float SeaLevelOffset => Ocean.seaLevelOffset;
        public float WaterDensity => Ocean.waterDensity;
        public float WaterTemperature => Ocean.waterTemperature;
        public float AirTemperature => Weather.airTemperature;
        public float AirPressure => Weather.atmosphericPressure;
        public float RainIntensity => Weather.rainIntensity;
        public float FogDensity => Weather.fogDensity;

        #endregion

        #region Spatial Queries

        /// <summary>
        /// Queries wind at a specific 3D world coordinate.
        /// </summary>
        public WindData GetWindAt(Vector3 worldPosition)
        {
            return _activeProvider != null ? _activeProvider.GetWindAt(worldPosition) : Wind;
        }

        /// <summary>
        /// Queries ocean current at a specific 3D world coordinate.
        /// </summary>
        public OceanCurrentData GetOceanCurrentAt(Vector3 worldPosition)
        {
            return _activeProvider != null ? _activeProvider.GetOceanCurrentAt(worldPosition) : Current;
        }

        /// <summary>
        /// Queries waves at a specific 3D world coordinate.
        /// </summary>
        public WaveData GetWavesAt(Vector3 worldPosition)
        {
            return _activeProvider != null ? _activeProvider.GetWavesAt(worldPosition) : Waves;
        }

        /// <summary>
        /// Queries full metocean snapshot at a specific 3D world coordinate.
        /// </summary>
        public MetoceanData GetDataAt(Vector3 worldPosition)
        {
            return _activeProvider != null ? _activeProvider.GetDataAt(worldPosition) : CurrentData;
        }

        #endregion

        #region Provider Management

        /// <summary>
        /// Switches the active metocean data provider at runtime.
        /// </summary>
        public void SetProvider(IMetoceanProvider newProvider)
        {
            if (ReferenceEquals(_currentSubscribedProvider, newProvider))
            {
                return;
            }

            if (_currentSubscribedProvider != null)
            {
                _currentSubscribedProvider.OnDataUpdated -= HandleProviderDataUpdated;
            }

            _currentSubscribedProvider = newProvider;
            _activeProvider = newProvider as MetoceanProviderBase;

            if (_currentSubscribedProvider != null)
            {
                _currentSubscribedProvider.OnDataUpdated += HandleProviderDataUpdated;
                HandleProviderDataUpdated(_currentSubscribedProvider.CurrentData);
            }
        }

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            if (instance != this)
            {
                return;
            }
            EnsureProviderAssigned();
        }

        private void OnEnable()
        {
            EnsureProviderAssigned();
            if (_activeProvider != null && !ReferenceEquals(_currentSubscribedProvider, _activeProvider))
            {
                SetProvider(_activeProvider);
            }
        }

        protected override void OnDestroy()
        {
            if (_currentSubscribedProvider != null)
            {
                _currentSubscribedProvider.OnDataUpdated -= HandleProviderDataUpdated;
                _currentSubscribedProvider = null;
            }
            base.OnDestroy();
        }

        private void OnValidate()
        {
            if (_activeProvider != null && !ReferenceEquals(_currentSubscribedProvider, _activeProvider))
            {
                SetProvider(_activeProvider);
            }
        }

        private void EnsureProviderAssigned()
        {
            if (_activeProvider == null)
            {
                // Attempt to discover a provider on this GameObject or in its children
                _activeProvider = GetComponentInChildren<MetoceanProviderBase>();

                // If still null, automatically add a default ConstantMetoceanProvider
                if (_activeProvider == null)
                {
                    _activeProvider = gameObject.AddComponent<ConstantMetoceanProvider>();
                    _activeProvider.hideFlags = HideFlags.None;
                }
            }

            if (_currentSubscribedProvider == null && _activeProvider != null)
            {
                SetProvider(_activeProvider);
            }
        }

        private void HandleProviderDataUpdated(MetoceanData newData)
        {
            bool windChanged = !_lastCachedData.Weather.wind.Equals(newData.Weather.wind);
            bool waveChanged = !_lastCachedData.Ocean.waves.Equals(newData.Ocean.waves);
            bool currentChanged = !_lastCachedData.Ocean.current.Equals(newData.Ocean.current);
            bool weatherChanged = !_lastCachedData.Weather.Equals(newData.Weather);

            _lastCachedData = newData;

            OnMetoceanUpdated?.Invoke(newData);

            if (windChanged) OnWindUpdated?.Invoke(newData.Weather.wind);
            if (waveChanged) OnWavesUpdated?.Invoke(newData.Ocean.waves);
            if (currentChanged) OnCurrentUpdated?.Invoke(newData.Ocean.current);
            if (weatherChanged) OnWeatherUpdated?.Invoke(newData.Weather);
        }

        #endregion
    }
}
