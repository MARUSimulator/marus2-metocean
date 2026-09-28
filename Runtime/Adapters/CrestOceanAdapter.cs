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

namespace Marus.Metocean
{
    /// <summary>
    /// Adapter that retrieves wave data, ocean current data, and sea level from Metocean
    /// and applies them to the Crest Ocean rendering and simulation system dynamically via reflection.
    /// Does not require a compile-time dependency on Crest assembly definition.
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

        private static Type _oceanRendererType;
        private static PropertyInfo _instanceProp;
        private static Type _shapeGerstnerType;
        private static FieldInfo _weightField;
        private static bool _typesResolved;

        private static void ResolveCrestTypes()
        {
            if (_typesResolved) return;
            _typesResolved = true;

            _oceanRendererType = Type.GetType("Crest.OceanRenderer, Crest") ??
                                 Type.GetType("Crest.OceanRenderer, Crest.HDRP") ??
                                 FindTypeInAssemblies("Crest.OceanRenderer");

            if (_oceanRendererType != null)
            {
                _instanceProp = _oceanRendererType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            }

            _shapeGerstnerType = Type.GetType("Crest.ShapeGerstnerBatched, Crest") ??
                                 Type.GetType("Crest.ShapeGerstnerBatched, Crest.HDRP") ??
                                 FindTypeInAssemblies("Crest.ShapeGerstnerBatched");

            if (_shapeGerstnerType != null)
            {
                _weightField = _shapeGerstnerType.GetField("_weight", BindingFlags.Public | BindingFlags.Instance);
            }
        }

        private static Type FindTypeInAssemblies(string typeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(typeName);
                if (t != null) return t;
            }
            return null;
        }

        protected override void Start()
        {
            base.Start();
            CheckCrestAvailability();
        }

        private void CheckCrestAvailability()
        {
            ResolveCrestTypes();

            if (_oceanRendererType == null)
            {
                _crestFound = false;
                _integrationStatus = "Crest not present";
                return;
            }

#if UNITY_6000_0_OR_NEWER
            var found = FindAnyObjectByType(_oceanRendererType);
#else
            var found = FindObjectOfType(_oceanRendererType);
#endif
            _crestFound = found != null;
            _integrationStatus = _crestFound ? "Crest OceanRenderer Connected" : "Crest library loaded, but no OceanRenderer in scene";
        }

        public override void OnMetoceanUpdated(MetoceanData data)
        {
            ApplyToCrest(data.Ocean);
        }

        private void ApplyToCrest(OceanStateData ocean)
        {
            ResolveCrestTypes();
            if (_oceanRendererType == null || _instanceProp == null)
            {
                _crestFound = false;
                return;
            }

            var instance = _instanceProp.GetValue(null) as Component;
            if (instance == null)
            {
                _crestFound = false;
                return;
            }
            _crestFound = true;

            // 1. Sea level / tide (in Crest, sea level corresponds to OceanRenderer transform position y)
            if (_syncSeaLevel)
            {
                var pos = instance.transform.position;
                pos.y = _baseSeaLevel + ocean.seaLevelOffset;
                instance.transform.position = pos;
            }

            // 2. Wave direction and height
            if (_shapeGerstnerType != null)
            {
                var shape = instance.GetComponentInChildren(_shapeGerstnerType);
                if (shape != null)
                {
                    if (_syncWaveDirection)
                    {
                        // Rotate wave generator towards wave propagation direction
                        shape.transform.rotation = Quaternion.Euler(0f, ocean.waves.TravelToDirectionDegrees, 0f);
                    }

                    if (_syncWaveHeight && _weightField != null)
                    {
                        _weightField.SetValue(shape, Mathf.Clamp(ocean.waves.significantWaveHeight * _waveWeightMultiplier, 0f, 10f));
                    }
                }
            }

            // 3. Ocean current
            if (_syncOceanCurrent && _currentInputTransform != null)
            {
                _currentInputTransform.rotation = Quaternion.Euler(0f, ocean.current.direction, 0f);
            }
        }
    }
}
