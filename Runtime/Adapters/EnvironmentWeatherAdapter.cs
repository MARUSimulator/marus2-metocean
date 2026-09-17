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

        [Header("Sun & Solar Position")]
        [Tooltip("Direct sunlight representing the Sun in the scene.")]
        [SerializeField] private Light _sunLight;

        [Tooltip("Calculate and apply realistic Sun elevation and azimuth angles based on geographic coordinates and time of day.")]
        [SerializeField] private bool _syncSunAngle = true;

        [Tooltip("Source of time used for calculating solar position.")]
        [SerializeField] private SunTimeMode _timeMode = SunTimeMode.MetoceanTimestamp;

        [Range(0f, 24f)]
        [Tooltip("Time of day in decimal hours (e.g. 12 = noon, 18.5 = 18:30) when CustomTimeOfDay mode is active.")]
        [SerializeField] private float _customTimeOfDayHours = 12.0f;

        [Tooltip("Default latitude in degrees North used when Metocean coordinates are not provided.")]
        [SerializeField] private double _fallbackLatitude = 43.508133;

        [Tooltip("Default longitude in degrees East used when Metocean coordinates are not provided.")]
        [SerializeField] private double _fallbackLongitude = 16.440193;

        [Header("Sun Lighting Intensity")]
        [Tooltip("Sunlight intensity when the Sun is high in a clear sky.")]
        [SerializeField] private float _maxSunIntensity = 1.25f;

        [Tooltip("Sunlight intensity when near the horizon (dawn / dusk).")]
        [SerializeField] private float _horizonSunIntensity = 0.35f;

        [Tooltip("Sunlight intensity during fully overcast or rainy skies.")]
        [SerializeField] private float _overcastSunIntensity = 0.25f;

        [Tooltip("Sunlight intensity when the Sun is below the horizon (night).")]
        [SerializeField] private float _nightSunIntensity = 0.0f;

        [Header("Solar Telemetry (Read Only)")]
        [SerializeField] private float _solarElevation;
        [SerializeField] private float _solarAzimuth;
        [SerializeField] private string _calculatedSolarTime = string.Empty;

        private MetoceanData _lastData = MetoceanData.Default;

        public float SolarElevation => _solarElevation;
        public float SolarAzimuth => _solarAzimuth;
        public string CalculatedSolarTime => _calculatedSolarTime;

        protected override void Update()
        {
            base.Update();
            UpdateSun();
        }

        private void OnValidate()
        {
            UpdateSun();
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

            // 4. Sun & Sky
            UpdateSun();
        }

        private void UpdateSun()
        {
            if (_sunLight == null) return;

            DateTime utcTime = ResolveCurrentTime();

            double lat = _lastData.location.latitude != 0.0 ? _lastData.location.latitude :
                         (GeoOrigin.HasInstance ? GeoOrigin.Instance.Latitude : _fallbackLatitude);
            double lon = _lastData.location.longitude != 0.0 ? _lastData.location.longitude :
                         (GeoOrigin.HasInstance ? GeoOrigin.Instance.Longitude : _fallbackLongitude);

            CalculateSolarPosition(utcTime, lat, lon, out float elevation, out float azimuth);

            _solarElevation = elevation;
            _solarAzimuth = azimuth;
            _calculatedSolarTime = utcTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

            // Apply orientation: Unity Directional Light forward (+Z) points in illumination direction.
            // Light comes FROM azimuth, so it shines towards (azimuth - 180°).
            if (_syncSunAngle)
            {
                float northOffset = GeoOrigin.HasInstance ? GeoOrigin.Instance.TrueNorthOffset : 0f;
                _sunLight.transform.rotation = Quaternion.Euler(elevation, azimuth - 180f + northOffset, 0f);
            }

            // Calculate illumination intensity
            float intensity;
            if (elevation > 0f)
            {
                // Day: ramp intensity from horizon to zenith
                float heightFactor = Mathf.Clamp01(Mathf.Sin(elevation * Mathf.Deg2Rad));
                float clearSkyIntensity = Mathf.Lerp(_horizonSunIntensity, _maxSunIntensity, heightFactor);

                // Cloud / rain attenuation
                float cloudDimming = Mathf.Clamp01(_lastData.Weather.cloudCoverage + _lastData.Weather.rainIntensity * 0.3f);
                intensity = Mathf.Lerp(clearSkyIntensity, _overcastSunIntensity, cloudDimming);
            }
            else if (elevation > -6f)
            {
                // Civil twilight: fade smoothly to night
                float twilightFactor = (elevation + 6f) / 6f;
                intensity = Mathf.Lerp(_nightSunIntensity, _horizonSunIntensity * 0.5f, twilightFactor);
            }
            else
            {
                // Night
                intensity = _nightSunIntensity;
            }

            _sunLight.intensity = intensity;
        }

        private DateTime ResolveCurrentTime()
        {
            switch (_timeMode)
            {
                case SunTimeMode.SystemLocalTime:
                    return DateTime.Now.ToUniversalTime();

                case SunTimeMode.SystemUtcTime:
                    return DateTime.UtcNow;

                case SunTimeMode.CustomTimeOfDay:
                    DateTime baseDate = _lastData.Timestamp != default ? _lastData.Timestamp : DateTime.UtcNow;
                    int hours = (int)_customTimeOfDayHours;
                    int minutes = (int)((_customTimeOfDayHours - hours) * 60f);
                    int seconds = (int)(((_customTimeOfDayHours - hours) * 60f - minutes) * 60f);
                    return new DateTime(baseDate.Year, baseDate.Month, baseDate.Day, hours % 24, minutes % 60, seconds % 60, DateTimeKind.Utc);

                case SunTimeMode.MetoceanTimestamp:
                default:
                    return _lastData.Timestamp != default ? _lastData.Timestamp : DateTime.UtcNow;
            }
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
    }
}
