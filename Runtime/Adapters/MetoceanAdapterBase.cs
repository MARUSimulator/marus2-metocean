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

using UnityEngine;

namespace Marus.Metocean
{
    /// <summary>
    /// Base class for components that consume Metocean data and apply it to visual renderers, physics, or simulation models.
    /// Manages subscription lifecycle automatically.
    /// </summary>
    [ExecuteAlways]
    public abstract class MetoceanAdapterBase : MonoBehaviour, IMetoceanAdapter
    {
        [Tooltip("If true, polls Metocean each frame in Update to support smooth temporal smoothing/interpolation.")]
        [SerializeField] protected bool _updateContinuously = false;

        protected virtual void OnEnable()
        {
            if (Metocean.HasInstance)
            {
                Subscribe();
                OnMetoceanUpdated(Metocean.Instance.CurrentData);
            }
        }

        protected virtual void Start()
        {
            // Initial synchronization if Metocean was instantiated on Start
            if (Metocean.HasInstance)
            {
                Subscribe();
                OnMetoceanUpdated(Metocean.Instance.CurrentData);
            }
        }

        protected virtual void OnDisable()
        {
            Unsubscribe();
        }

        protected virtual void Update()
        {
            if (_updateContinuously && Metocean.HasInstance)
            {
                OnMetoceanUpdated(Metocean.Instance.CurrentData);
            }
        }

        private bool _isSubscribed = false;

        private void Subscribe()
        {
            if (_isSubscribed || !Metocean.HasInstance) return;
            Metocean.Instance.OnMetoceanUpdated += OnMetoceanUpdated;
            Metocean.Instance.OnWindUpdated += OnWindUpdated;
            Metocean.Instance.OnWavesUpdated += OnWavesUpdated;
            Metocean.Instance.OnCurrentUpdated += OnCurrentUpdated;
            Metocean.Instance.OnWeatherUpdated += OnWeatherUpdated;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || !Metocean.HasInstance) return;
            Metocean.Instance.OnMetoceanUpdated -= OnMetoceanUpdated;
            Metocean.Instance.OnWindUpdated -= OnWindUpdated;
            Metocean.Instance.OnWavesUpdated -= OnWavesUpdated;
            Metocean.Instance.OnCurrentUpdated -= OnCurrentUpdated;
            Metocean.Instance.OnWeatherUpdated -= OnWeatherUpdated;
            _isSubscribed = false;
        }

        public abstract void OnMetoceanUpdated(MetoceanData data);

        protected virtual void OnWindUpdated(WindData wind) { }
        protected virtual void OnWavesUpdated(WaveData waves) { }
        protected virtual void OnCurrentUpdated(OceanCurrentData current) { }
        protected virtual void OnWeatherUpdated(WeatherStateData weather) { }
    }
}

