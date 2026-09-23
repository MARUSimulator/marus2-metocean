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
using Marus.Core;
using UnityEngine;

namespace Marus.Metocean
{
    public enum SunTimeMode
    {
        [Tooltip("Use the timestamp reported by the active Metocean data feed.")]
        MetoceanTimestamp,

        [Tooltip("Use current local computer clock.")]
        SystemLocalTime,

        [Tooltip("Use current UTC computer clock.")]
        SystemUtcTime,

        [Tooltip("Use the manual time of day slider below (0 - 24 hours).")]
        CustomTimeOfDay
    }

    /// <summary>
    /// Adapter that applies Metocean weather states (fog, rain particles, wind, cloudiness) and
    /// astronomical solar positioning (sun angle, time of day, geographic coordinates) to scene environment components.
    /// Controls RenderSettings fog, rain ParticleSystems, Unity WindZones, and directional Sun lighting.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("MARUS/Metocean/Environment Weather Adapter")]
    public class EnvironmentWeatherAdapter : MetoceanAdapterBase
    {
        [Header("Fog Settings")]
        [Tooltip("Sync RenderSettings fog with Metocean fog density / visibility.")]
        [SerializeField] private bool _syncFog = true;
        [SerializeField] private float _maxFogDensity = 0.08f;
        [SerializeField] private Color _fogColor = new Color(0.75f, 0.8f, 0.85f, 1.0f);

        [Header("Rain Settings")]
        [Tooltip("Sync rain particle system emission with rain intensity.")]
        [SerializeField] private bool _syncRain = true;
        [SerializeField] private ParticleSystem _rainParticleSystem;
        [SerializeField] private float _maxRainEmissionRate = 2000f;

        [Header("Wind Zone Settings")]
        [Tooltip("Sync a Unity WindZone with Metocean wind speed and direction.")]
        [SerializeField] private bool _syncWindZone = true;
        [SerializeField] private WindZone _windZone;
        [SerializeField] private float _windZoneSpeedMultiplier = 1.0f;

        [Header("Time & Date Settings")]
        [Tooltip("Source of time used for calculating celestial positions.")]
        [SerializeField] private SunTimeMode _timeMode = SunTimeMode.MetoceanTimestamp;

        [Range(0f, 24f)]
        [Tooltip("Time of day in decimal hours (e.g. 12 = noon, 18.5 = 18:30) when CustomTimeOfDay mode is active.")]
        [SerializeField] private float _customTimeOfDayHours = 12.0f;

        [Tooltip("When enabled, overrides the year, month, and day with the custom date below.")]
        [SerializeField] private bool _useCustomDate = false;

        [Tooltip("Custom year (e.g. 2026).")]
        [SerializeField] private int _customYear = 2026;

        [Range(1, 12)]
        [Tooltip("Custom month (1 - 12).")]
        [SerializeField] private int _customMonth = 9;

        [Range(1, 31)]
        [Tooltip("Custom day (1 - 31).")]
        [SerializeField] private int _customDay = 23;

        [Tooltip("Default latitude in degrees North used when Metocean coordinates are not provided.")]
        [SerializeField] private double _fallbackLatitude = 43.508133;

        [Tooltip("Default longitude in degrees East used when Metocean coordinates are not provided.")]
        [SerializeField] private double _fallbackLongitude = 16.440193;

        [Header("Sun Lighting")]
        [Tooltip("Direct sunlight representing the Sun in the scene.")]
        [SerializeField] private Light _sunLight;

        [Tooltip("Calculate and apply realistic Sun elevation and azimuth angles based on geographic coordinates and time of day.")]
        [SerializeField] private bool _syncSunAngle = true;

        [Tooltip("Sunlight intensity when the Sun is high in a clear sky.")]
        [SerializeField] private float _maxSunIntensity = 1.25f;

        [Tooltip("Sunlight intensity when near the horizon (dawn / dusk).")]
        [SerializeField] private float _horizonSunIntensity = 0.35f;

        [Tooltip("Sunlight intensity during fully overcast or rainy skies.")]
        [SerializeField] private float _overcastSunIntensity = 0.25f;

        [Tooltip("Sunlight/moonlight intensity when the Sun is below the horizon (night). Set > 0 to prevent pitch black scene at night.")]
        [SerializeField] private float _nightSunIntensity = 0.02f;

        [Header("Moon & Night Lighting")]
        [Tooltip("Optional Directional Light representing the Moon in the scene.")]
        [SerializeField] private Light _moonLight;

        [Tooltip("Calculate and apply realistic Moon elevation and azimuth angles based on geographic coordinates and date/time.")]
        [SerializeField] private bool _syncMoonAngle = true;

        [Tooltip("Calculate and apply realistic Moon phase (crescent, quarter, gibbous, full) based on date.")]
        [SerializeField] private bool _syncMoonPhase = true;

        [Tooltip("Moonlight intensity when the Moon is full and high in a clear night sky (typically 0.1 - 0.5).")]
        [SerializeField] private float _maxMoonIntensity = 0.25f;

        [Tooltip("Moonlight color tint (cool lunar blue-white).")]
        [SerializeField] private Color _moonColor = new Color(0.75f, 0.82f, 0.95f, 1.0f);

        private float _solarElevation;
        private float _solarAzimuth;
        private float _moonElevation;
        private float _moonAzimuth;
        private float _moonPhaseProgress;
        private float _moonIlluminationFraction;
        private string _calculatedSolarTime = string.Empty;

        private MetoceanData _lastData = MetoceanData.Default;

        public float SolarElevation => _solarElevation;
        public float SolarAzimuth => _solarAzimuth;
        public float MoonElevation => _moonElevation;
        public float MoonAzimuth => _moonAzimuth;
        public float MoonPhaseProgress => _moonPhaseProgress;
        public float MoonIlluminationFraction => _moonIlluminationFraction;
        public string MoonPhaseName => GetMoonPhaseName(_moonPhaseProgress);
        public string CalculatedSolarTime => _calculatedSolarTime;

        protected override void Update()
        {
            base.Update();
            UpdateCelestialBodies();
        }

        private void OnValidate()
        {
            UpdateCelestialBodies();
        }

        private void Reset()
        {
            var now = DateTime.Now;
            _customYear = now.Year;
            _customMonth = now.Month;
            _customDay = now.Day;
            _customTimeOfDayHours = (float)(now.Hour + now.Minute / 60.0);
        }

        public override void OnMetoceanUpdated(MetoceanData data)
        {
            _lastData = data;
            ApplyWeather(data);
        }

        private void ApplyWeather(MetoceanData data)
        {
            var weather = data.Weather;

            // 1. Fog
            if (_syncFog)
            {
                RenderSettings.fog = weather.fogDensity > 0.001f || weather.visibility < 5000f;
                if (RenderSettings.fog)
                {
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                    RenderSettings.fogColor = _fogColor;
                    RenderSettings.fogDensity = weather.fogDensity * _maxFogDensity;
                }
            }

            // 2. Rain Particles
            if (_syncRain && _rainParticleSystem != null)
            {
                var emission = _rainParticleSystem.emission;
                float targetRate = weather.rainIntensity * _maxRainEmissionRate;
                emission.rateOverTime = targetRate;

                if (weather.IsRaining && !_rainParticleSystem.isPlaying)
                {
                    _rainParticleSystem.Play();
                }
                else if (!weather.IsRaining && _rainParticleSystem.isPlaying)
                {
                    _rainParticleSystem.Stop();
                }
            }

            // 3. Wind Zone
            if (_syncWindZone && _windZone != null)
            {
                float northOffset = GeoOrigin.HasInstance ? GeoOrigin.Instance.TrueNorthOffset : 0f;
                _windZone.windMain = weather.wind.speed * _windZoneSpeedMultiplier;
                _windZone.windTurbulence = Mathf.Max(0f, weather.wind.gustSpeed - weather.wind.speed);
                _windZone.transform.rotation = Quaternion.Euler(0f, weather.wind.BlowToDirectionDegrees + northOffset, 0f);
            }

            // 4. Sun & Moon (Celestial Bodies)
            UpdateCelestialBodies();
        }

        private void UpdateCelestialBodies()
        {
            DateTime utcTime = ResolveCurrentTime();

            double lat = _lastData.location.latitude != 0.0 ? _lastData.location.latitude :
                         (GeoOrigin.HasInstance ? GeoOrigin.Instance.Latitude : _fallbackLatitude);
            double lon = _lastData.location.longitude != 0.0 ? _lastData.location.longitude :
                         (GeoOrigin.HasInstance ? GeoOrigin.Instance.Longitude : _fallbackLongitude);

            UpdateSun(utcTime, lat, lon);
            UpdateMoon(utcTime, lat, lon);
        }

        private void UpdateSun(DateTime utcTime, double lat, double lon)
        {
            if (_sunLight == null) return;

            CalculateSolarPosition(utcTime, lat, lon, out float elevation, out float azimuth);

            _solarElevation = elevation;
            _solarAzimuth = azimuth;

            // Compute local solar time for informative Inspector telemetry
            int dayOfYear = utcTime.DayOfYear;
            double hour = utcTime.Hour + utcTime.Minute / 60.0 + utcTime.Second / 3600.0 + utcTime.Millisecond / 3600000.0;
            double gamma = 2.0 * Math.PI / 365.0 * (dayOfYear - 1.0 + (hour - 12.0) / 24.0);
            double eqTime = 229.18 * (0.000075 + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma)
                           - 0.014615 * Math.Cos(2.0 * gamma) - 0.040849 * Math.Sin(2.0 * gamma));
            double solarTimeMin = (hour * 60.0 + eqTime + 4.0 * lon) % 1440.0;
            if (solarTimeMin < 0.0) solarTimeMin += 1440.0;
            int sH = (int)(solarTimeMin / 60.0);
            int sM = (int)(solarTimeMin % 60.0);

            DateTime localDisplay = utcTime.ToLocalTime();
            _calculatedSolarTime = $"{localDisplay:HH:mm:ss} Local ({utcTime:HH:mm:ss} UTC, Solar {sH:02d}:{sM:02d})";

            // Apply orientation: Unity Directional Light forward (+Z) points in illumination direction.
            // Light comes FROM azimuth, so it shines towards (azimuth - 180°).
            if (_syncSunAngle)
            {
                float northOffset = GeoOrigin.HasInstance ? GeoOrigin.Instance.TrueNorthOffset : 0f;
                // During day, use exact sun elevation.
                // During twilight (0 to -6°), keep light grazing above horizon (0.5°) so horizontal surfaces (water/terrain) receive twilight illumination.
                // At night, if no separate moon light is active, angle light from above as fallback night light.
                float lightElevation = elevation >= 0.5f ? elevation :
                    (elevation > -6f ? 0.5f : (_moonLight != null ? Mathf.Max(-10f, elevation) : 35f));
                _sunLight.transform.rotation = Quaternion.Euler(lightElevation, azimuth - 180f + northOffset, 0f);
            }

            // Calculate illumination intensity
            float intensity;
            if (elevation > 0f)
            {
                // Day: smooth atmospheric falloff curve (preserves natural afternoon illumination without premature darkening)
                float sinEl = Mathf.Clamp01(Mathf.Sin(elevation * Mathf.Deg2Rad));
                float heightFactor = Mathf.Sqrt(sinEl);
                float clearSkyIntensity = Mathf.Lerp(_horizonSunIntensity, _maxSunIntensity, heightFactor);

                // Cloud / rain attenuation
                float cloudDimming = Mathf.Clamp01(_lastData.Weather.cloudCoverage + _lastData.Weather.rainIntensity * 0.3f);
                intensity = Mathf.Lerp(clearSkyIntensity, _overcastSunIntensity, cloudDimming);
            }
            else if (elevation > -6f)
            {
                // Civil twilight: fade smoothly to night
                float twilightFactor = (elevation + 6f) / 6f;
                intensity = Mathf.Lerp(_moonLight != null ? 0f : _nightSunIntensity, _horizonSunIntensity * 0.5f, twilightFactor);
            }
            else
            {
                // Night
                intensity = _moonLight != null ? 0f : _nightSunIntensity;
            }

            _sunLight.intensity = intensity;
        }

        private void UpdateMoon(DateTime utcTime, double lat, double lon)
        {
            if (_moonLight == null)
            {
                // Auto-detect Moon Light in scene if not manually assigned
                var moonGo = GameObject.Find("Directional Moon Light") ?? GameObject.Find("Moon Light") ?? GameObject.Find("Moon");
                if (moonGo != null) _moonLight = moonGo.GetComponent<Light>();
                if (_moonLight == null) return;
            }

            CalculateMoonPosition(utcTime, lat, lon, out float elevation, out float azimuth);
            CalculateMoonPhase(utcTime, out float phaseProgress, out float illuminationFraction);

            _moonElevation = elevation;
            _moonAzimuth = azimuth;
            _moonPhaseProgress = phaseProgress;
            _moonIlluminationFraction = illuminationFraction;

            EnsureHdMoonSettings(_moonLight, _syncMoonPhase, phaseProgress);

            if (_syncMoonAngle)
            {
                float northOffset = GeoOrigin.HasInstance ? GeoOrigin.Instance.TrueNorthOffset : 0f;
                float lightElevation = Mathf.Max(0.5f, elevation);
                _moonLight.transform.rotation = Quaternion.Euler(lightElevation, azimuth - 180f + northOffset, 0f);
            }

            // Moonlight illumination:
            // High when moon is high at night; scales with phase illumination; fades out during bright daytime (sun elevation > 0); dims with clouds.
            float intensity = 0f;
            if (elevation > 0f)
            {
                float heightFactor = Mathf.Sqrt(Mathf.Clamp01(Mathf.Sin(elevation * Mathf.Deg2Rad)));
                float phaseFactor = _syncMoonPhase ? Mathf.Max(0.05f, illuminationFraction) : 1f;
                float baseIntensity = _maxMoonIntensity * heightFactor * phaseFactor;

                // Day / night crossfade: moonlight dims when sun is high
                float sunSuppression = Mathf.Clamp01((_solarElevation - 0f) / 10f); // 0 at sunset/night, 1 when sun > 10°
                intensity = Mathf.Lerp(baseIntensity, baseIntensity * 0.05f, sunSuppression);

                // Cloud dimming
                float cloudDimming = Mathf.Clamp01(_lastData.Weather.cloudCoverage + _lastData.Weather.rainIntensity * 0.3f);
                intensity *= (1f - cloudDimming * 0.8f);
            }

            _moonLight.intensity = intensity;
            _moonLight.color = _moonColor;
        }

        private static bool s_hdMoonSettingsResolved;
        private static System.Reflection.PropertyInfo s_hdInteractsWithSkyProp;
        private static System.Reflection.FieldInfo s_hdShadingSourceField;
        private static System.Reflection.FieldInfo s_hdDiameterOverrideField;
        private static System.Reflection.FieldInfo s_hdDistanceField;
        private static System.Reflection.FieldInfo s_hdMoonPhaseField;
        private static System.Reflection.FieldInfo s_hdSunIntensityField;
        private static System.Reflection.FieldInfo s_hdSunColorField;

        private static void EnsureHdMoonSettings(Light moonLight, bool syncPhase, float phaseProgress)
        {
            if (moonLight == null) return;
            var comp = moonLight.GetComponent("HDAdditionalLightData");
            if (comp == null) return;

            if (!s_hdMoonSettingsResolved)
            {
                s_hdMoonSettingsResolved = true;
                var type = comp.GetType();
                s_hdInteractsWithSkyProp = type.GetProperty("interactsWithSky");
                s_hdShadingSourceField = type.GetField("celestialBodyShadingSource");
                s_hdDiameterOverrideField = type.GetField("diameterOverride");
                s_hdDistanceField = type.GetField("distance");
                s_hdMoonPhaseField = type.GetField("moonPhase");
                s_hdSunIntensityField = type.GetField("sunIntensity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                s_hdSunColorField = type.GetField("sunColor", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            }

            // Ensure the moon renders in the sky
            if (s_hdInteractsWithSkyProp != null)
            {
                s_hdInteractsWithSkyProp.SetValue(comp, true);
            }

            // Shading source:
            // Manual (2) enables HDRP's 3D celestial sphere shading with realistic lunar phase and earthshine.
            // Emission (1) renders a uniform glowing disk.
            if (s_hdShadingSourceField != null)
            {
                int targetSource = syncPhase ? 2 : 1;
                s_hdShadingSourceField.SetValue(comp, System.Enum.ToObject(s_hdShadingSourceField.FieldType, targetSource));
            }

            // Apply calculated moon phase to HDRP celestial body
            if (syncPhase && s_hdMoonPhaseField != null)
            {
                s_hdMoonPhaseField.SetValue(comp, phaseProgress);
            }

            if (s_hdSunIntensityField != null)
            {
                float intensity = (float)s_hdSunIntensityField.GetValue(comp);
                if (intensity < 1000f)
                {
                    s_hdSunIntensityField.SetValue(comp, 130000.0f);
                }
            }

            if (s_hdSunColorField != null)
            {
                Color col = (Color)s_hdSunColorField.GetValue(comp);
                if (col.maxColorComponent < 0.1f)
                {
                    s_hdSunColorField.SetValue(comp, Color.white);
                }
            }

            // Ensure visible size in the sky (default 0.5° is tiny)
            if (s_hdDiameterOverrideField != null)
            {
                float d = (float)s_hdDiameterOverrideField.GetValue(comp);
                if (d < 2.0f)
                {
                    s_hdDiameterOverrideField.SetValue(comp, 4.0f);
                }
            }

            // Ensure lunar distance
            if (s_hdDistanceField != null)
            {
                float dist = (float)s_hdDistanceField.GetValue(comp);
                if (dist < 1000f)
                {
                    s_hdDistanceField.SetValue(comp, 384400000f);
                }
            }
        }

        private DateTime ResolveCurrentTime()
        {
            DateTime baseDate;
            if (_useCustomDate)
            {
                int year = _customYear > 0 ? Mathf.Clamp(_customYear, 1970, 2100) : DateTime.Now.Year;
                int month = Mathf.Clamp(_customMonth, 1, 12);
                int maxDays = DateTime.DaysInMonth(year, month);
                int day = Mathf.Clamp(_customDay, 1, maxDays);
                baseDate = new DateTime(year, month, day);
            }
            else
            {
                baseDate = _lastData.Timestamp != default ? _lastData.Timestamp : DateTime.Now;
            }

            DateTime time;
            switch (_timeMode)
            {
                case SunTimeMode.SystemLocalTime:
                    if (_useCustomDate)
                    {
                        var now = DateTime.Now;
                        var localCustom = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, now.Hour, now.Minute, now.Second, now.Millisecond, DateTimeKind.Local);
                        time = localCustom.ToUniversalTime();
                    }
                    else
                    {
                        time = DateTime.Now.ToUniversalTime();
                    }
                    break;

                case SunTimeMode.SystemUtcTime:
                    if (_useCustomDate)
                    {
                        var utc = DateTime.UtcNow;
                        var utcCustom = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, utc.Hour, utc.Minute, utc.Second, utc.Millisecond, DateTimeKind.Utc);
                        time = utcCustom;
                    }
                    else
                    {
                        time = DateTime.UtcNow;
                    }
                    break;

                case SunTimeMode.CustomTimeOfDay:
                    int hours = (int)_customTimeOfDayHours;
                    float remM = (_customTimeOfDayHours - hours) * 60f;
                    int minutes = (int)remM;
                    int seconds = (int)((remM - minutes) * 60f);

                    // Interpret _customTimeOfDayHours as local wall-clock time (matching the user's watch, system timezone, and Google sunrise/sunset)
                    DateTime localClockTime = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, hours % 24, minutes % 60, seconds % 60, DateTimeKind.Local);
                    time = localClockTime.ToUniversalTime();
                    break;

                case SunTimeMode.MetoceanTimestamp:
                default:
                    if (_useCustomDate && _lastData.Timestamp != default)
                    {
                        var ts = _lastData.Timestamp;
                        time = new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, ts.Hour, ts.Minute, ts.Second, ts.Millisecond, DateTimeKind.Utc);
                    }
                    else
                    {
                        time = _lastData.Timestamp != default ? _lastData.Timestamp : DateTime.UtcNow;
                    }
                    break;
            }

            if (time.Kind != DateTimeKind.Utc)
            {
                time = time.ToUniversalTime();
            }
            return time;
        }

        /// <summary>
        /// Calculates solar elevation (degrees above horizon) and solar azimuth (degrees clockwise from North)
        /// using NOAA / Spencer astronomical equations for a given UTC time, latitude, and longitude.
        /// </summary>
        public static void CalculateSolarPosition(DateTime utcTime, double latitudeDeg, double longitudeDeg, out float elevationDeg, out float azimuthDeg)
        {
            int dayOfYear = utcTime.DayOfYear;
            double hour = utcTime.Hour + utcTime.Minute / 60.0 + utcTime.Second / 3600.0 + utcTime.Millisecond / 3600000.0;

            // Fractional year in radians
            double gamma = 2.0 * Math.PI / 365.0 * (dayOfYear - 1.0 + (hour - 12.0) / 24.0);

            // Equation of time in minutes
            double eqTime = 229.18 * (0.000075 + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma)
                           - 0.014615 * Math.Cos(2.0 * gamma) - 0.040849 * Math.Sin(2.0 * gamma));

            // Solar declination in radians
            double decl = 0.006918 - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma)
                          - 0.006758 * Math.Cos(2.0 * gamma) + 0.000907 * Math.Sin(2.0 * gamma)
                          - 0.002697 * Math.Cos(3.0 * gamma) + 0.00148 * Math.Sin(3.0 * gamma);

            // Solar time offset in minutes
            double timeOffset = eqTime + 4.0 * longitudeDeg;

            // True solar time in minutes
            double trueSolarTime = (hour * 60.0 + timeOffset) % 1440.0;
            if (trueSolarTime < 0.0) trueSolarTime += 1440.0;

            // Hour angle in radians (0 at solar noon, negative before noon, positive after noon)
            double haDeg = trueSolarTime / 4.0 - 180.0;
            double haRad = haDeg * (Math.PI / 180.0);

            double latRad = latitudeDeg * (Math.PI / 180.0);

            // Solar elevation angle
            double sinElevation = Math.Sin(latRad) * Math.Sin(decl) + Math.Cos(latRad) * Math.Cos(decl) * Math.Cos(haRad);
            sinElevation = Math.Clamp(sinElevation, -1.0, 1.0);
            double elevationRad = Math.Asin(sinElevation);
            elevationDeg = (float)(elevationRad * (180.0 / Math.PI));

            // Solar azimuth angle (degrees clockwise from True North)
            // East component = -cos(decl) * sin(ha)
            // North component = sin(decl) * cos(lat) - cos(decl) * sin(lat) * cos(ha)
            double y = -Math.Cos(decl) * Math.Sin(haRad);
            double x = Math.Sin(decl) * Math.Cos(latRad) - Math.Cos(decl) * Math.Sin(latRad) * Math.Cos(haRad);
            double azRad = Math.Atan2(y, x);
            double azDeg = azRad * (180.0 / Math.PI);
            azDeg = (azDeg % 360.0 + 360.0) % 360.0;

            azimuthDeg = (float)azDeg;
        }

        /// <summary>
        /// Calculates Moon elevation and azimuth for a given UTC time, latitude, and longitude
        /// using Schlyter's lunar orbital elements.
        /// </summary>
        public static void CalculateMoonPosition(DateTime utcTime, double latitudeDeg, double longitudeDeg, out float elevationDeg, out float azimuthDeg)
        {
            DateTime epoch = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            double d = (utcTime - epoch).TotalDays;

            // Orbital elements (in degrees)
            double N = (125.1228 - 0.0529538083 * d) % 360.0;
            double i = 5.1454;
            double w = (318.0634 + 0.1643573223 * d) % 360.0;
            double a = 60.2666; // Earth radii
            double e = 0.054900;
            double M = (115.3654 + 13.0649929509 * d) % 360.0;

            double mRad = M * (Math.PI / 180.0);
            double E = M + (180.0 / Math.PI) * e * Math.Sin(mRad) * (1.0 + e * Math.Cos(mRad));
            double eRad = E * (Math.PI / 180.0);

            double x = a * (Math.Cos(eRad) - e);
            double y = a * Math.Sqrt(1.0 - e * e) * Math.Sin(eRad);

            double r = Math.Sqrt(x * x + y * y);
            double vRad = Math.Atan2(y, x);

            double nRad = N * (Math.PI / 180.0);
            double iRad = i * (Math.PI / 180.0);
            double vwRad = vRad + w * (Math.PI / 180.0);

            // Ecliptic coordinates
            double xEcl = r * (Math.Cos(nRad) * Math.Cos(vwRad) - Math.Sin(nRad) * Math.Sin(vwRad) * Math.Cos(iRad));
            double yEcl = r * (Math.Sin(nRad) * Math.Cos(vwRad) + Math.Cos(nRad) * Math.Sin(vwRad) * Math.Cos(iRad));
            double zEcl = r * (Math.Sin(vwRad) * Math.Sin(iRad));

            // Ecliptic obliquity
            double eclObliq = (23.4393 - 3.563e-7 * d) * (Math.PI / 180.0);

            // Equatorial coordinates
            double xEq = xEcl;
            double yEq = yEcl * Math.Cos(eclObliq) - zEcl * Math.Sin(eclObliq);
            double zEq = yEcl * Math.Sin(eclObliq) + zEcl * Math.Cos(eclObliq);

            double ra = Math.Atan2(yEq, xEq) * (180.0 / Math.PI);
            ra = (ra % 360.0 + 360.0) % 360.0;
            double dec = Math.Atan2(zEq, Math.Sqrt(xEq * xEq + yEq * yEq));

            // Local Sidereal Time
            double gmst = (280.46061837 + 360.98564736629 * d) % 360.0;
            double lst = (gmst + longitudeDeg) % 360.0;

            // Hour angle
            double haDeg = (lst - ra) % 360.0;
            if (haDeg > 180.0) haDeg -= 360.0;
            if (haDeg < -180.0) haDeg += 360.0;
            double haRad = haDeg * (Math.PI / 180.0);

            double latRad = latitudeDeg * (Math.PI / 180.0);

            // Horizontal coordinates (elevation, azimuth)
            double sinEl = Math.Sin(latRad) * Math.Sin(dec) + Math.Cos(latRad) * Math.Cos(dec) * Math.Cos(haRad);
            sinEl = Math.Clamp(sinEl, -1.0, 1.0);
            elevationDeg = (float)(Math.Asin(sinEl) * (180.0 / Math.PI));

            double yAz = -Math.Cos(dec) * Math.Sin(haRad);
            double xAz = Math.Sin(dec) * Math.Cos(latRad) - Math.Cos(dec) * Math.Sin(latRad) * Math.Cos(haRad);
            double azRad = Math.Atan2(yAz, xAz);
            double az = (azRad * (180.0 / Math.PI)) % 360.0;
            if (az < 0.0) az += 360.0;
            azimuthDeg = (float)az;
        }

        /// <summary>
        /// Calculates the Moon's phase progress (0.0 = New Moon, 0.25 = First Quarter, 0.5 = Full Moon, 0.75 = Last Quarter)
        /// and illumination fraction (0.0 = 0% to 1.0 = 100%) for a given UTC time using the standard synodic month cycle.
        /// </summary>
        public static void CalculateMoonPhase(DateTime utcTime, out float phaseProgress, out float illuminationFraction)
        {
            // Known reference New Moon: January 6, 2000 at 18:14 UTC
            DateTime referenceNewMoon = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
            const double synodicMonth = 29.530588853; // Mean length of lunar synodic month in days

            double daysSinceNew = (utcTime - referenceNewMoon).TotalDays;
            double phase = (daysSinceNew % synodicMonth + synodicMonth) % synodicMonth;
            phaseProgress = (float)(phase / synodicMonth);

            // Geometric fraction of visible disk illuminated by sunlight (0.0 = 0% to 1.0 = 100%)
            illuminationFraction = (float)((1.0 - Math.Cos(phaseProgress * 2.0 * Math.PI)) * 0.5);
        }

        /// <summary>
        /// Returns the standard English name of the Moon phase corresponding to a phase progress value (0.0 - 1.0).
        /// </summary>
        public static string GetMoonPhaseName(float phaseProgress)
        {
            float p = (phaseProgress % 1f + 1f) % 1f;
            if (p < 0.03f || p >= 0.97f) return "New Moon";
            if (p < 0.22f) return "Waxing Crescent";
            if (p <= 0.28f) return "First Quarter";
            if (p < 0.47f) return "Waxing Gibbous";
            if (p <= 0.53f) return "Full Moon";
            if (p < 0.72f) return "Waning Gibbous";
            if (p <= 0.78f) return "Last Quarter";
            return "Waning Crescent";
        }
    }
}
