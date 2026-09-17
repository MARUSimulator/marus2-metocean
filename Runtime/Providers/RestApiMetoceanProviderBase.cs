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
using System.Collections;
using Marus.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Marus.Metocean
{
    /// <summary>
    /// Abstract base class for REST API and live buoy metocean providers.
    /// Provides coroutine-based scheduled polling, network error handling, geographic binding, and response caching.
    /// </summary>
    public abstract class RestApiMetoceanProviderBase : MetoceanProviderBase
    {
        [Header("API Configuration")]
        [Tooltip("Base endpoint URL for the weather / buoy REST service.")]
        [SerializeField] protected string _apiUrl = "https://api.open-meteo.com/v1/forecast";

        [Tooltip("Polling interval in seconds between REST requests.")]
        [Min(1f)]
        [SerializeField] protected float _pollIntervalSeconds = 60.0f;

        [Tooltip("If true, automatically starts polling on Start/Enable.")]
        [SerializeField] protected bool _autoPoll = true;

        [Tooltip("Request timeout in seconds.")]
        [Range(1, 60)]
        [SerializeField] protected int _timeoutSeconds = 10;

        [Header("Geographic Location")]
        [Tooltip("If true, automatically synchronizes coordinates from the scene's GeoOrigin singleton.")]
        [SerializeField] protected bool _useGeoOrigin = true;

        [Tooltip("Latitude in decimal degrees (WGS84).")]
        [SerializeField] protected double _latitude = 43.508133; // Split, Croatia (Adriatic default)

        [Tooltip("Longitude in decimal degrees (WGS84).")]
        [SerializeField] protected double _longitude = 16.440193;

        [Header("Connection Status")]
        [SerializeField] protected bool _isOnline = false;
        [SerializeField] protected string _lastFetchTimestamp = "Never";
        [SerializeField] protected string _lastError = string.Empty;

        public override bool IsAvailable => _isOnline;
        public bool IsFetching { get; private set; }
        public string LastError => _lastError;
        public string LastFetchTimestamp => _lastFetchTimestamp;
        public bool UseGeoOrigin => _useGeoOrigin;

        protected Coroutine _pollCoroutine;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_useGeoOrigin && GeoOrigin.HasInstance)
            {
                SetCoordinates(GeoOrigin.Instance.Latitude, GeoOrigin.Instance.Longitude);
                GeoOrigin.Instance.OnOriginChanged += HandleOriginChanged;
            }

            if (_autoPoll && Application.isPlaying)
            {
                StartPolling();
            }
        }

        protected virtual void OnDisable()
        {
            if (GeoOrigin.HasInstance)
            {
                GeoOrigin.Instance.OnOriginChanged -= HandleOriginChanged;
            }
            StopPolling();
        }

        private void HandleOriginChanged(GeoPoint newOrigin)
        {
            if (_useGeoOrigin)
            {
                SetCoordinates(newOrigin.latitude, newOrigin.longitude);
                if (Application.isPlaying && _isOnline)
                {
                    FetchNow();
                }
            }
        }

        /// <summary>
        /// Starts the automatic background polling coroutine.
        /// </summary>
        public void StartPolling()
        {
            if (_pollCoroutine != null)
            {
                StopCoroutine(_pollCoroutine);
            }
            _pollCoroutine = StartCoroutine(PollRoutine());
        }

        /// <summary>
        /// Halts polling.
        /// </summary>
        public void StopPolling()
        {
            if (_pollCoroutine != null)
            {
                StopCoroutine(_pollCoroutine);
                _pollCoroutine = null;
            }
        }

        /// <summary>
        /// Forces an immediate asynchronous fetch.
        /// </summary>
        public void FetchNow()
        {
            if (!IsFetching && gameObject.activeInHierarchy)
            {
                StartCoroutine(FetchRoutine());
            }
        }

        protected virtual IEnumerator PollRoutine()
        {
            while (true)
            {
                yield return FetchRoutine();
                yield return new WaitForSecondsRealtime(_pollIntervalSeconds);
            }
        }

        protected virtual IEnumerator FetchRoutine()
        {
            IsFetching = true;
            string requestUrl = BuildRequestUrl();

            using (UnityWebRequest request = UnityWebRequest.Get(requestUrl))
            {
                request.timeout = _timeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    try
                    {
                        if (TryParseResponse(json, out MetoceanData data))
                        {
                            data.location = new GeoPoint(_latitude, _longitude, 0.0);
                            _isOnline = true;
                            _lastError = string.Empty;
                            _lastFetchTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

                            MetoceanData merged = MetoceanDataMerger.Merge(_currentData, data, ProvidedData);
                            NotifyDataUpdated(merged);
                        }
                        else
                        {
                            _lastError = "Failed to parse API payload.";
                            Debug.LogWarning($"[{ProviderName}] Parse error from URL '{requestUrl}'", this);
                        }
                    }
                    catch (Exception ex)
                    {
                        _lastError = ex.Message;
                        Debug.LogError($"[{ProviderName}] Error parsing response: {ex.Message}", this);
                    }
                }
                else
                {
                    _isOnline = false;
                    _lastError = $"{request.result}: {request.error}";
                    Debug.LogWarning($"[{ProviderName}] Request error: {_lastError} (URL: {requestUrl})", this);
                }
            }

            IsFetching = false;
        }

        /// <summary>
        /// Constructs the full HTTP request URL including query parameters.
        /// </summary>
        protected virtual string BuildRequestUrl()
        {
            return _apiUrl;
        }

        /// <summary>
        /// Parses raw API response string (JSON / XML) into a MetoceanData struct.
        /// </summary>
        protected abstract bool TryParseResponse(string responseText, out MetoceanData data);

        /// <summary>
        /// Sets geographic target coordinates for this provider.
        /// </summary>
        public void SetCoordinates(double latitude, double longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
        }
    }
}

