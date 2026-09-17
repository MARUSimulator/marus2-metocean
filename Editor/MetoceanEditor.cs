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

using UnityEditor;
using UnityEngine;

namespace Marus.Metocean.Editor
{
    [CustomEditor(typeof(Metocean))]
    public class MetoceanEditor : UnityEditor.Editor
    {
        private bool _showTelemetry = true;
        private bool _showAdapters = false;

        public override void OnInspectorGUI()
        {
            Metocean metocean = (Metocean)target;

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("MARUS Metocean Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Central authority for weather, wind, waves, and ocean currents across the simulation.", MessageType.Info);

            EditorGUILayout.Space();

            // 1. Active Provider Selection (Standard Unity Object Field)
            SerializedProperty providerProp = serializedObject.FindProperty("_activeProvider");
            EditorGUILayout.PropertyField(providerProp, new GUIContent("Active Provider", "The provider supplying metocean data. Drag any MetoceanProviderBase here or click the circle picker to select."));

            serializedObject.ApplyModifiedProperties();

            // 2. If no provider is assigned, offer clear quick-add options
            if (metocean.ActiveProvider == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("No active provider is assigned. Drag a provider into 'Active Provider' above, or choose an option below:", MessageType.Warning);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Constant / Offline Provider"))
                {
                    var prov = metocean.GetComponent<ConstantMetoceanProvider>() ?? metocean.gameObject.AddComponent<ConstantMetoceanProvider>();
                    metocean.SetProvider(prov);
                    EditorUtility.SetDirty(metocean);
                }

                if (GUILayout.Button("Add Live Web Providers"))
                {
                    SetupLiveProviders(metocean);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            // 3. Visual & Physics Adapters
            DrawAdaptersSection(metocean);

            EditorGUILayout.Space();

            // 4. Live Telemetry Readings Box
            _showTelemetry = EditorGUILayout.Foldout(_showTelemetry, "Current Readings", true);
            if (_showTelemetry)
            {
                var data = metocean.CurrentData;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Wind
                EditorGUILayout.LabelField("🌬 Wind", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Speed:", $"{data.Weather.wind.speed:F1} m/s ({data.Weather.wind.SpeedKmH:F1} km/h, {data.Weather.wind.SpeedKnots:F1} kts)");
                EditorGUILayout.LabelField("Direction:", $"from {data.Weather.wind.direction:F0}° (blowing towards {data.Weather.wind.BlowToDirectionDegrees:F0}°)");
                EditorGUILayout.LabelField("Gusts:", $"{data.Weather.wind.gustSpeed:F1} m/s");
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(4);

                // Waves
                EditorGUILayout.LabelField("🌊 Waves", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Significant Height (Hs):", $"{data.Ocean.waves.significantWaveHeight:F2} m");
                EditorGUILayout.LabelField("Peak Period (Tp):", $"{data.Ocean.waves.peakPeriod:F1} s");
                EditorGUILayout.LabelField("Wave Direction:", $"from {data.Ocean.waves.direction:F0}°");
                EditorGUILayout.LabelField("Sea State:", $"{data.Ocean.waves.seaState}");
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(4);

                // Current
                EditorGUILayout.LabelField("🧭 Ocean Current", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Speed:", $"{data.Ocean.current.speed:F2} m/s ({data.Ocean.current.SpeedKnots:F2} kts)");
                EditorGUILayout.LabelField("Direction:", $"towards {data.Ocean.current.direction:F0}°");
                EditorGUILayout.LabelField("Depth:", $"{data.Ocean.current.depth:F1} m");
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(4);

                // Weather
                EditorGUILayout.LabelField("⛅ Atmosphere & Weather", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Air Temperature:", $"{data.Weather.airTemperature:F1} °C");
                EditorGUILayout.LabelField("Water Temperature:", $"{data.Ocean.waterTemperature:F1} °C");
                EditorGUILayout.LabelField("Atmospheric Pressure:", $"{data.Weather.atmosphericPressure:F1} hPa");
                EditorGUILayout.LabelField("Relative Humidity:", $"{data.Weather.relativeHumidity:F0} %");
                EditorGUILayout.LabelField("Rain Intensity:", $"{data.Weather.rainIntensity * 100f:F0}%");
                EditorGUILayout.LabelField("Fog Density:", $"{data.Weather.fogDensity * 100f:F0}% (Visibility: {data.Weather.visibility:F0}m)");
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawAdaptersSection(Metocean metocean)
        {
            var crestAdapter = metocean.GetComponent<CrestOceanAdapter>();
            var weatherAdapter = metocean.GetComponent<EnvironmentWeatherAdapter>();
            bool hasBoth = crestAdapter != null && weatherAdapter != null;

            _showAdapters = EditorGUILayout.Foldout(_showAdapters, $"Visual Adapters ({(hasBoth ? "Connected" : "Setup")})", true);
            if (_showAdapters)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Adapters take Metocean data and update scene visuals & physics:", EditorStyles.wordWrappedMiniLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Crest Ocean Adapter:", crestAdapter != null ? "✓ Connected" : "– Not added");
                if (crestAdapter == null && GUILayout.Button("Add Crest Adapter", GUILayout.Width(140)))
                {
                    metocean.gameObject.AddComponent<CrestOceanAdapter>();
                    EditorUtility.SetDirty(metocean.gameObject);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Weather Visuals Adapter:", weatherAdapter != null ? "✓ Connected" : "– Not added");
                if (weatherAdapter == null && GUILayout.Button("Add Weather Adapter", GUILayout.Width(140)))
                {
                    metocean.gameObject.AddComponent<EnvironmentWeatherAdapter>();
                    EditorUtility.SetDirty(metocean.gameObject);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
        }

        private void SetupLiveProviders(Metocean metocean)
        {
            var composite = metocean.GetComponent<CompositeMetoceanProvider>() ?? metocean.gameObject.AddComponent<CompositeMetoceanProvider>();
            var marine = metocean.GetComponent<OpenMeteoMarineProvider>() ?? metocean.gameObject.AddComponent<OpenMeteoMarineProvider>();
            var weather = metocean.GetComponent<WeatherDisplayClientRawProvider>() ?? metocean.gameObject.AddComponent<WeatherDisplayClientRawProvider>();

            composite.AddProvider(marine);
            composite.AddProvider(weather);
            metocean.SetProvider(composite);

            EditorUtility.SetDirty(metocean.gameObject);
        }
    }
}
