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
    /// Abstract MonoBehaviour base class for metocean providers.
    /// Provides standard lifecycle management, event dispatching, and fallback spatial querying.
    /// </summary>
    public abstract class MetoceanProviderBase : MonoBehaviour, IMetoceanProvider
    {
        public virtual string ProviderName => GetType().Name;

        public virtual bool IsAvailable => true;

        public virtual MetoceanDataFlags ProvidedData => MetoceanDataFlags.All;

        public virtual bool Provides(MetoceanDataFlags flags) => (ProvidedData & flags) == flags;

        public event Action<MetoceanData> OnDataUpdated;

        protected MetoceanData _currentData = MetoceanData.Default;

        public virtual MetoceanData CurrentData => _currentData;
        public virtual WindData CurrentWind => _currentData.Weather.wind;
        public virtual OceanCurrentData CurrentOceanCurrent => _currentData.Ocean.current;
        public virtual WaveData CurrentWaves => _currentData.Ocean.waves;
        public virtual OceanStateData CurrentOceanState => _currentData.Ocean;
        public virtual WeatherStateData CurrentWeatherState => _currentData.Weather;

        public virtual WindData GetWindAt(Vector3 worldPosition) => CurrentWind;
        public virtual OceanCurrentData GetOceanCurrentAt(Vector3 worldPosition) => CurrentOceanCurrent;
        public virtual WaveData GetWavesAt(Vector3 worldPosition) => CurrentWaves;
        public virtual MetoceanData GetDataAt(Vector3 worldPosition) => CurrentData;

        /// <summary>
        /// Call this method in subclasses whenever metocean data changes or is fetched from a provider.
        /// </summary>
        protected virtual void NotifyDataUpdated(MetoceanData newData)
        {
            _currentData = newData;
            OnDataUpdated?.Invoke(_currentData);
        }

        protected virtual void OnEnable()
        {
            // If Metocean is already initialized, notify it of our availability
            if (Metocean.HasInstance && Metocean.Instance.ActiveProvider == (IMetoceanProvider)this)
            {
                NotifyDataUpdated(_currentData);
            }
        }
    }
}

