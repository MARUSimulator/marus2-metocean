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
    [CustomEditor(typeof(RestApiMetoceanProviderBase), true)]
    [CanEditMultipleObjects]
    public class RestApiMetoceanProviderEditor : UnityEditor.Editor
    {
        private bool _showLiveTelemetry = true;

        public override void OnInspectorGUI()
        {
            RestApiMetoceanProviderBase provider = (RestApiMetoceanProviderBase)target;

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(provider.ProviderName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Capabilities: {provider.ProvidedData}", EditorStyles.miniLabel);

            // Connection Status Banner
            DrawStatusBanner(provider);

            EditorGUILayout.Space();

            // Interactive Fetch Now button
            EditorGUI.BeginDisabledGroup(!Application.isPlaying || provider.IsFetching);
            if (GUILayout.Button(provider.IsFetching ? "Fetching Live Telemetry..." : "Fetch Now (Asynchronous)"))
            {
                provider.FetchNow();
            }
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Live network requests run in Play Mode via Unity Coroutines.", MessageType.None);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();


            // Draw default serialized properties (excluding base cache if desired)
            DrawDefaultInspector();

            EditorGUILayout.Space();

            // Live Parsed Readings Foldout
            _showLiveTelemetry = EditorGUILayout.Foldout(_showLiveTelemetry, "Live Telemetry Snapshot", true);
            if (_showLiveTelemetry)
            {
                DrawProviderTelemetry(provider);
            }

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawStatusBanner(RestApiMetoceanProviderBase provider)
        {
            if (provider.IsFetching)
            {
                GUI.color = new Color(1f, 0.9f, 0.4f);
                EditorGUILayout.HelpBox("Connecting & fetching REST endpoint...", MessageType.Info);
                GUI.color = Color.white;
            }
            else if (provider.IsAvailable)
            {
                GUI.color = new Color(0.6f, 1f, 0.6f);
                EditorGUILayout.HelpBox($"ONLINE • Last fetched: {provider.LastFetchTimestamp}", MessageType.Info);
                GUI.color = Color.white;
            }
            else
            {
                string error = string.IsNullOrEmpty(provider.LastError) ? "No data received yet." : provider.LastError;
                EditorGUILayout.HelpBox($"OFFLINE • {error}", MessageType.Warning);
            }
        }

        private void DrawProviderTelemetry(RestApiMetoceanProviderBase provider)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (provider is OpenMeteoMarineProvider marine)
            {
                var live = marine.LiveData;
                if (live != null)
                {
                    EditorGUILayout.LabelField("Significant Wave Height:", $"{live.wave_height:F2} m");
                    EditorGUILayout.LabelField("Wave Peak Period:", $"{live.wave_period:F1} s");
                    EditorGUILayout.LabelField("Wave Direction:", $"{live.wave_direction}°");
                    EditorGUILayout.LabelField("Wind Waves:", $"{live.wind_wave_height:F2} m @ {live.wind_wave_period:F1}s (Dir: {live.wind_wave_direction}°)");
                    EditorGUILayout.LabelField("Swell Waves:", $"{live.swell_wave_height:F2} m @ {live.swell_wave_period:F1}s (Dir: {live.swell_wave_direction}°)");
                    EditorGUILayout.LabelField("Ocean Current:", $"{live.ocean_current_velocity:F2} km/h towards {live.ocean_current_direction}°");
                }
                else
                {
                    EditorGUILayout.LabelField("No marine data received yet.");
                }
            }
            else if (provider is WeatherDisplayClientRawProvider wd)
            {
                var data = wd.LiveWeatherData;
                if (!string.IsNullOrEmpty(data.stationName))
                {
                    EditorGUILayout.LabelField("Station Name:", data.stationName);
                    EditorGUILayout.LabelField("Station Update:", data.lastUpdateTime);
                    EditorGUILayout.LabelField("Air Temperature:", $"{data.airTemperatureC:F1} °C");
                    EditorGUILayout.LabelField("Humidity:", $"{data.humidityPercent:F0} %");
                    EditorGUILayout.LabelField("Barometer:", $"{data.barometerHPa:F1} hPa");
                    EditorGUILayout.LabelField("Wind Speed:", $"{data.windSpeedMs:F1} m/s ({data.windSpeedKmh:F1} km/h, {data.windSpeedKnots:F1} kts)");
                    EditorGUILayout.LabelField("Wind Gust:", $"{data.windGustMs:F1} m/s ({data.windGustKmh:F1} km/h)");
                    EditorGUILayout.LabelField("Wind Direction:", $"{data.windDirectionDegrees:F0}°");
                    EditorGUILayout.LabelField("Daily Rain:", $"{data.dailyRainMm:F1} mm (Rate: {data.rainRateMmHour:F1} mm/h)");
                    if (!string.IsNullOrEmpty(data.weatherConditionText))
                    {
                        EditorGUILayout.LabelField("Condition:", data.weatherConditionText);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No weather station data received yet.");
                }
            }
            else
            {
                var cur = provider.CurrentData;
                EditorGUILayout.LabelField("Waves:", $"{cur.Ocean.waves.significantWaveHeight:F2}m @ {cur.Ocean.waves.peakPeriod:F1}s");
                EditorGUILayout.LabelField("Wind:", $"{cur.Weather.wind.speed:F1} m/s from {cur.Weather.wind.direction:F0}°");
                EditorGUILayout.LabelField("Current:", $"{cur.Ocean.current.speed:F2} m/s");
                EditorGUILayout.LabelField("Air Temp:", $"{cur.Weather.airTemperature:F1} °C");
            }

            EditorGUILayout.EndVertical();
        }
    }
}

