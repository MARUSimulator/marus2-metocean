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
    [CustomEditor(typeof(CompositeMetoceanProvider))]
    public class CompositeMetoceanProviderEditor : UnityEditor.Editor
    {
        private bool _showCoverage = true;

        public override void OnInspectorGUI()
        {
            CompositeMetoceanProvider composite = (CompositeMetoceanProvider)target;

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Composite Metocean Provider", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Merges multiple specialized providers (e.g. Open-Meteo for waves + Weather Display for atmosphere) into a unified metocean stream.", MessageType.Info);

            EditorGUILayout.Space();

            // Draw Providers list
            SerializedProperty providersProp = serializedObject.FindProperty("_providers");
            EditorGUILayout.PropertyField(providersProp, new GUIContent("Child Providers"), true);

            EditorGUILayout.Space();

            if (GUILayout.Button("Rebuild Merged State"))
            {
                composite.RebuildMergedState();
                EditorUtility.SetDirty(composite);
            }

            EditorGUILayout.Space();

            // Coverage Checklist
            _showCoverage = EditorGUILayout.Foldout(_showCoverage, "Parameter Coverage Checklist", true);
            if (_showCoverage)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                DrawCoverageItem(composite, MetoceanDataFlags.Waves, "🌊 Surface Waves (Hs, Tp, Dir)");
                DrawCoverageItem(composite, MetoceanDataFlags.OceanCurrent, "🧭 Ocean Current (Velocity, Dir)");
                DrawCoverageItem(composite, MetoceanDataFlags.Wind, "🌬 Surface Wind (Speed, Dir, Gusts)");
                DrawCoverageItem(composite, MetoceanDataFlags.AirTemperature, "🌡 Air Temperature");
                DrawCoverageItem(composite, MetoceanDataFlags.AtmosphericPressure, "⏲ Barometric Pressure");
                DrawCoverageItem(composite, MetoceanDataFlags.RelativeHumidity, "💧 Relative Humidity");
                DrawCoverageItem(composite, MetoceanDataFlags.Precipitation, "🌧 Rain / Precipitation");
                DrawCoverageItem(composite, MetoceanDataFlags.WaterProperties, "🌊 Water Properties (Salinity, Density, Temp)");
                DrawCoverageItem(composite, MetoceanDataFlags.VisibilityAndFog, "🌫 Fog & Visibility");
                DrawCoverageItem(composite, MetoceanDataFlags.CloudCoverage, "⛅ Cloud Coverage");

                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawCoverageItem(CompositeMetoceanProvider composite, MetoceanDataFlags flag, string label)
        {
            string source = "Not provided (Using Default)";
            bool provided = false;

            if (composite.Providers != null)
            {
                for (int i = 0; i < composite.Providers.Count; i++)
                {
                    var p = composite.Providers[i];
                    if (p != null && p.Provides(flag))
                    {
                        source = $"{p.gameObject.name} ({p.ProviderName})";
                        provided = true;
                        break;
                    }
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (provided)
            {
                GUI.color = new Color(0.4f, 1f, 0.4f);
                EditorGUILayout.LabelField("✓", GUILayout.Width(16));
                GUI.color = Color.white;
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(220));
                EditorGUILayout.LabelField($"[{source}]", EditorStyles.miniLabel);
            }
            else
            {
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                EditorGUILayout.LabelField("–", GUILayout.Width(16));
                EditorGUILayout.LabelField(label, GUILayout.Width(220));
                EditorGUILayout.LabelField($"[{source}]", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}

