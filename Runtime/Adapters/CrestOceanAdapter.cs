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
using System.Reflection;
using UnityEngine;

#if CREST_OCEAN
using Crest;
#endif

namespace Marus.Metocean
{
    /// <summary>
    /// Adapter that retrieves wave data, ocean current data, and sea level from Metocean
    /// and applies them to the Crest Ocean rendering and simulation system.
    /// </summary>
    [AddComponentMenu("MARUS/Metocean/Crest Ocean Adapter")]
    public class CrestOceanAdapter : MetoceanAdapterBase
    {
        [Header("Sea Level")]
        [Tooltip("Apply sea level / tidal elevation offset from Metocean to Crest OceanRenderer.")]
        [SerializeField] private bool _syncSeaLevel = true;
        [SerializeField] private float _baseSeaLevel = 0.0f;

        [Header("Waves")]
        [Tooltip("Apply significant wave height (Hs) to wave generator / spectrum weight.")]
        [SerializeField] private bool _syncWaveHeight = true;

        [Tooltip("Scale factor mapping significant wave height (meters) to Crest wave weight/amplitude.")]
        [SerializeField] private float _waveWeightMultiplier = 1.0f;

        [Tooltip("Apply wave travel direction to wave generator orientation.")]
        [SerializeField] private bool _syncWaveDirection = true;

        [Header("Ocean Current")]
        [Tooltip("Apply ocean current velocity vector to Crest current input if present.")]
        [SerializeField] private bool _syncOceanCurrent = true;

        [Tooltip("Optional transform for Crest Current Input or Flow Map.")]
        [SerializeField] private Transform _currentInputTransform;

        [Header("Status")]
        [SerializeField] private bool _crestFound = false;
        [SerializeField] private string _integrationStatus = "Initializing...";

        public bool CrestFound => _crestFound;
        public string IntegrationStatus => _integrationStatus;

        protected override void Start()
        {
            base.Start();
            CheckCrestAvailability();
        }

        private void CheckCrestAvailability()
        {
#if CREST_OCEAN
#if UNITY_6000_0_OR_NEWER
            var oceanRenderer = FindAnyObjectByType<OceanRenderer>();
#else
            var oceanRenderer = FindObjectOfType<OceanRenderer>();
#endif
            _crestFound = oceanRenderer != null;
            _integrationStatus = _crestFound ? "Crest OceanRenderer Connected" : "No OceanRenderer found in scene";
#else
            // Check via reflection if Crest exists in assembly without compile-time define
            var crestType = Type.GetType("Crest.OceanRenderer, Crest") ??
                            Type.GetType("Crest.OceanRenderer, Crest.HDRP");
            if (crestType != null)
            {
#if UNITY_6000_0_OR_NEWER
                var found = FindAnyObjectByType(crestType);
#else
                var found = FindObjectOfType(crestType);
#endif
                _crestFound = found != null;
                _integrationStatus = _crestFound
                    ? "Crest detected (Define CREST_OCEAN for direct bindings)"
                    : "Crest library loaded, but no OceanRenderer in scene";
            }
            else
            {
                _crestFound = false;
                _integrationStatus = "Crest not present (Define CREST_OCEAN when Crest is installed)";
            }
#endif
        }

        public override void OnMetoceanUpdated(MetoceanData data)
        {
            ApplyToCrest(data.Ocean);
        }

        private void ApplyToCrest(OceanStateData ocean)
        {
#if CREST_OCEAN
            if (OceanRenderer.Instance == null)
            {
                _crestFound = false;
                return;
            }
            _crestFound = true;

            // 1. Sea level / tide
            if (_syncSeaLevel)
            {
                OceanRenderer.Instance.SeaLevel = _baseSeaLevel + ocean.seaLevelOffset;
            }

            // 2. Wave direction and height
            var shapeGerstner = OceanRenderer.Instance.GetComponentInChildren<ShapeGerstnerBatched>();
            if (shapeGerstner != null)
            {
                if (_syncWaveDirection)
                {
                    // Rotate wave generator towards wave propagation direction
                    shapeGerstner.transform.rotation = Quaternion.Euler(0f, ocean.waves.TravelToDirectionDegrees, 0f);
                }

                if (_syncWaveHeight)
                {
                    shapeGerstner._weight = Mathf.Clamp(ocean.waves.significantWaveHeight * _waveWeightMultiplier, 0f, 10f);
                }
            }

            // 3. Ocean current
            if (_syncOceanCurrent && _currentInputTransform != null)
            {
                _currentInputTransform.rotation = Quaternion.Euler(0f, ocean.current.direction, 0f);
            }
#else
            // Fallback via reflection when CREST_OCEAN scripting define is not yet defined
            TryApplyViaReflection(ocean);
#endif
        }

#if !CREST_OCEAN
        private void TryApplyViaReflection(OceanStateData ocean)
        {
            var oceanRendererType = Type.GetType("Crest.OceanRenderer, Crest") ??
                                    Type.GetType("Crest.OceanRenderer, Crest.HDRP");
            if (oceanRendererType == null) return;

            var instanceProp = oceanRendererType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceProp?.GetValue(null);
            if (instance == null) return;

            if (_syncSeaLevel)
            {
                var seaLevelProp = oceanRendererType.GetProperty("SeaLevel", BindingFlags.Public | BindingFlags.Instance);
                seaLevelProp?.SetValue(instance, _baseSeaLevel + ocean.seaLevelOffset);
            }

            var shapeGerstnerType = Type.GetType("Crest.ShapeGerstnerBatched, Crest") ??
                                    Type.GetType("Crest.ShapeGerstnerBatched, Crest.HDRP");
            if (shapeGerstnerType != null && instance is Component comp)
            {
                var shape = comp.GetComponentInChildren(shapeGerstnerType);
                if (shape != null)
                {
                    if (_syncWaveDirection)
                    {
                        shape.transform.rotation = Quaternion.Euler(0f, ocean.waves.TravelToDirectionDegrees, 0f);
                    }

                    if (_syncWaveHeight)
                    {
                        var weightField = shapeGerstnerType.GetField("_weight", BindingFlags.Public | BindingFlags.Instance);
                        weightField?.SetValue(shape, Mathf.Clamp(ocean.waves.significantWaveHeight * _waveWeightMultiplier, 0f, 10f));
                    }
                }
            }

            if (_syncOceanCurrent && _currentInputTransform != null)
            {
                _currentInputTransform.rotation = Quaternion.Euler(0f, ocean.current.direction, 0f);
            }
        }
#endif
    }
}
