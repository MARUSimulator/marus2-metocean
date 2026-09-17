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
using System.Collections.Generic;
using UnityEngine;

namespace Marus.Metocean
{
    /// <summary>
    /// Composite metocean provider that aggregates multiple specialized providers (e.g. Open-Meteo for waves
    /// and currents + Weather Display for atmospheric telemetry) into a single, unified data stream.
    /// Uses <see cref="MetoceanDataFlags"/> to perform non-destructive multi-source data fusion.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("MARUS/Metocean/Providers/Composite Metocean Provider")]
    public class CompositeMetoceanProvider : MetoceanProviderBase
    {
        [Tooltip("Ordered list of child providers. When multiple providers supply the same field, the first provider in the list takes priority.")]
        [SerializeField] private List<MetoceanProviderBase> _providers = new List<MetoceanProviderBase>();

        public override string ProviderName => "Composite Metocean Provider";

        public IReadOnlyList<MetoceanProviderBase> Providers => _providers;

        public override bool IsAvailable
        {
            get
            {
                if (_providers == null || _providers.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < _providers.Count; i++)
                {
                    if (_providers[i] != null && _providers[i].IsAvailable)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Aggregates capabilities of all registered child providers using bitwise OR.
        /// </summary>
        public override MetoceanDataFlags ProvidedData
        {
            get
            {
                MetoceanDataFlags aggregate = MetoceanDataFlags.None;
                if (_providers != null)
                {
                    for (int i = 0; i < _providers.Count; i++)
                    {
                        if (_providers[i] != null)
                        {
                            aggregate |= _providers[i].ProvidedData;
                        }
                    }
                }
                return aggregate;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToProviders();
            RebuildMergedState();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromProviders();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                SubscribeToProviders();
                RebuildMergedState();
            }
        }

        /// <summary>
        /// Dynamically registers a child provider.
        /// </summary>
        public void AddProvider(MetoceanProviderBase provider)
        {
            if (provider == null || ReferenceEquals(provider, this) || _providers.Contains(provider))
            {
                return;
            }

            _providers.Add(provider);
            provider.OnDataUpdated += data => HandleChildDataUpdated(provider, data);
            RebuildMergedState();
        }

        /// <summary>
        /// Removes a child provider.
        /// </summary>
        public void RemoveProvider(MetoceanProviderBase provider)
        {
            if (provider != null && _providers.Remove(provider))
            {
                provider.OnDataUpdated -= data => HandleChildDataUpdated(provider, data);
                RebuildMergedState();
            }
        }

        private readonly Dictionary<MetoceanProviderBase, Action<MetoceanData>> _subscribedHandlers
            = new Dictionary<MetoceanProviderBase, Action<MetoceanData>>();

        private void SubscribeToProviders()
        {
            UnsubscribeFromProviders();

            if (_providers == null) return;

            for (int i = 0; i < _providers.Count; i++)
            {
                var p = _providers[i];
                if (p != null && !ReferenceEquals(p, this) && !_subscribedHandlers.ContainsKey(p))
                {
                    Action<MetoceanData> handler = data => HandleChildDataUpdated(p, data);
                    _subscribedHandlers[p] = handler;
                    p.OnDataUpdated += handler;
                }
            }
        }

        private void UnsubscribeFromProviders()
        {
            foreach (var kvp in _subscribedHandlers)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.OnDataUpdated -= kvp.Value;
                }
            }
            _subscribedHandlers.Clear();
        }

        private void HandleChildDataUpdated(MetoceanProviderBase provider, MetoceanData childData)
        {
            if (provider == null) return;

            // Only merge the specific fields that this child provider produces
            _currentData = MetoceanDataMerger.Merge(_currentData, childData, provider.ProvidedData);
            NotifyDataUpdated(_currentData);
        }

        /// <summary>
        /// Recalculates the full merged metocean state by polling each child provider in sequence.
        /// </summary>
        public void RebuildMergedState()
        {
            MetoceanData merged = MetoceanData.Default;

            if (_providers != null)
            {
                for (int i = 0; i < _providers.Count; i++)
                {
                    var p = _providers[i];
                    if (p != null && !ReferenceEquals(p, this))
                    {
                        merged = MetoceanDataMerger.Merge(merged, p.CurrentData, p.ProvidedData);
                    }
                }
            }

            NotifyDataUpdated(merged);
        }

        #region Spatial Query Delegation

        public override WindData GetWindAt(Vector3 worldPosition)
        {
            var provider = FindProviderFor(MetoceanDataFlags.Wind);
            return provider != null ? provider.GetWindAt(worldPosition) : base.GetWindAt(worldPosition);
        }

        public override WaveData GetWavesAt(Vector3 worldPosition)
        {
            var provider = FindProviderFor(MetoceanDataFlags.Waves);
            return provider != null ? provider.GetWavesAt(worldPosition) : base.GetWavesAt(worldPosition);
        }

        public override OceanCurrentData GetOceanCurrentAt(Vector3 worldPosition)
        {
            var provider = FindProviderFor(MetoceanDataFlags.OceanCurrent);
            return provider != null ? provider.GetOceanCurrentAt(worldPosition) : base.GetOceanCurrentAt(worldPosition);
        }

        public override MetoceanData GetDataAt(Vector3 worldPosition)
        {
            MetoceanData data = _currentData;
            var windProv = FindProviderFor(MetoceanDataFlags.Wind);
            var waveProv = FindProviderFor(MetoceanDataFlags.Waves);
            var currProv = FindProviderFor(MetoceanDataFlags.OceanCurrent);

            if (windProv != null) data.Weather.wind = windProv.GetWindAt(worldPosition);
            if (waveProv != null) data.Ocean.waves = waveProv.GetWavesAt(worldPosition);
            if (currProv != null) data.Ocean.current = currProv.GetOceanCurrentAt(worldPosition);

            return data;
        }

        private MetoceanProviderBase FindProviderFor(MetoceanDataFlags flag)
        {
            if (_providers == null) return null;
            for (int i = 0; i < _providers.Count; i++)
            {
                var p = _providers[i];
                if (p != null && p.Provides(flag))
                {
                    return p;
                }
            }
            return null;
        }

        #endregion
    }
}
