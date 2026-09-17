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
using UnityEngine;

namespace Marus.Metocean
{
    /// <summary>
    /// Common interface for all meteorological and oceanographic data sources (REST APIs, buoys, procedural, constants).
    /// </summary>
    public interface IMetoceanProvider
    {
        /// <summary>
        /// Display name identifying this provider.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Returns true if this provider has valid data and is operating normally.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Flags indicating which metocean parameters are produced and managed by this provider.
        /// </summary>
        MetoceanDataFlags ProvidedData { get; }

        /// <summary>
        /// Checks whether this provider supplies the specified parameter(s).
        /// </summary>
        bool Provides(MetoceanDataFlags flags);

        /// <summary>
        /// Invoked whenever new metocean data is fetched, updated, or modified.
        /// </summary>
        event Action<MetoceanData> OnDataUpdated;

        /// <summary>
        /// Current global metocean data snapshot.
        /// </summary>
        MetoceanData CurrentData { get; }

        /// <summary>
        /// Current global wind conditions.
        /// </summary>
        WindData CurrentWind { get; }

        /// <summary>
        /// Current global ocean current conditions.
        /// </summary>
        OceanCurrentData CurrentOceanCurrent { get; }

        /// <summary>
        /// Current global wave field conditions.
        /// </summary>
        WaveData CurrentWaves { get; }

        /// <summary>
        /// Current global ocean state.
        /// </summary>
        OceanStateData CurrentOceanState { get; }

        /// <summary>
        /// Current global weather / atmospheric state.
        /// </summary>
        WeatherStateData CurrentWeatherState { get; }

        /// <summary>
        /// Queries wind state at a specific world coordinate.
        /// </summary>
        WindData GetWindAt(Vector3 worldPosition);

        /// <summary>
        /// Queries ocean current at a specific world coordinate.
        /// </summary>
        OceanCurrentData GetOceanCurrentAt(Vector3 worldPosition);

        /// <summary>
        /// Queries wave conditions at a specific world coordinate.
        /// </summary>
        WaveData GetWavesAt(Vector3 worldPosition);

        /// <summary>
        /// Queries full metocean snapshot at a specific world coordinate.
        /// </summary>
        MetoceanData GetDataAt(Vector3 worldPosition);
    }
}

