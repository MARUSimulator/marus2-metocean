# MARUS2 Metocean

Meteorological and Oceanographic (Metocean) state management module for MARUS2.

## Overview

The `Metocean` module acts as the single source of truth for weather, atmospheric, and oceanographic environmental states in MARUS2 simulations. It decouples data sources from environmental visual/physics consumers using a flexible provider-adapter architecture.

### Key Features
- **Central Metocean Singleton**: Query wind, currents, waves, water temperature, salinity, density, rain, fog, visibility, and atmospheric pressure anywhere in code via `Metocean.Instance`.
- **Pluggable Provider Architecture (`IMetoceanProvider`)**:
  - `ConstantMetoceanProvider`: Configurable provider with Inspector controls and ocean/weather presets.
  - `OpenMeteoMarineProvider`: Fetches real-time wave heights, peak periods, directions, wind waves, swell, and ocean current velocity/direction from the [Open-Meteo Marine API](https://open-meteo.com/en/docs/marine-weather-api).
  - `WeatherDisplayClientRawProvider`: Ingests real-time weather observations from a Weather Display `clientraw.txt` feed (temperature, humidity, barometric pressure, wind speed, gust, direction, rain rate).
  - `CompositeMetoceanProvider`: Performs multi-source data fusion, combining specialized ocean and weather providers into a unified stream.
  - `RestApiMetoceanProviderBase`: Template base class for asynchronous REST APIs and real-time buoy feeds.
- **Provider Capability System (`MetoceanDataFlags`)**: Providers advertise which physical parameters they supply (e.g. `Waves`, `OceanCurrent`, `Wind`, `AirTemperature`, `Precipitation`). The `MetoceanDataMerger` non-destructively merges updates so partial feeds never overwrite unproduced fields.
- **Consumer Adapters**:
  - `CrestOceanAdapter`: Applies wave spectrum, significant wave height, peak period, currents, and sea level to Crest Ocean.
  - `EnvironmentWeatherAdapter`: Controls scene fog, rain particles, wind zones, directional Sun and Moon orientation, astronomical ephemeris, dynamic lunar phase shading, and calendar date controls.
- **MARUS Standard**: Implements `Marus.Utils.Singleton<Metocean>`, uses geodetic coordinates via `Marus.Core.GeoPoint`, and provides full spatial and global queries.

---

## Environment & Celestial Weather Adapter

The `EnvironmentWeatherAdapter` bridges Metocean atmospheric conditions into Unity's rendering pipeline (supporting Unity High Definition Render Pipeline / HDRP Physically Based Sky).

### 1. Astronomical Solar & Lunar Positioning
* **Solar Ephemeris (NOAA / Spencer Model)**:
  * Computes solar declination ($\delta$), fractional year ($\gamma$), equation of time, and hour angle based on geographic coordinates (latitude, longitude) and UTC time.
  * Accurately calculates seasonal sun paths: higher elevation and longer days in summer, lower elevation and shorter days in winter.
  * **Twilight Handling**: Grazes light at $0.5^\circ$ during civil twilight ($0^\circ$ to $-6^\circ$) to provide realistic horizontal surface illumination without sudden pitch-black transitions at sunset.
* **Lunar Ephemeris (Paul Schlyter Model)**:
  * Computes the Moon's 3D geocentric coordinates, ecliptic-to-equatorial transformation, Greenwich Mean Sidereal Time (GMST), and Local Sidereal Time (LST).
  * Outputs real-world **Moon Elevation** and **Moon Azimuth**, naturally advancing $\approx 13.2^\circ$ east across the sky per day.
* **Local Wall-Clock Alignment**:
  * In `CustomTimeOfDay` mode, entered hours (e.g., `18.86` = 18:52) are treated as local wall-clock time matching your local system timezone / daylight saving time, ensuring simulated sunset matches Google and real-world observations.

### 2. Dynamic Moon Phase & Physical HDRP Shading
* **Synodic Cycle Calculation**:
  * Tracks progress through the $29.53059$-day lunar cycle from astronomical reference epochs.
  * Outputs normalized **Phase Progress** ($0.0$ to $1.0$), **Illumination Fraction** ($0\%$ to $100\%$), and phase name (`New Moon`, `Waxing Crescent`, `First Quarter`, `Waxing Gibbous`, `Full Moon`, `Waning Gibbous`, `Last Quarter`, `Waning Crescent`).
* **HDRP Physically Based Sky Integration**:
  * Configures `HDAdditionalLightData` with `CelestialBodyShadingSource.Manual`.
  * Shaded dynamically as a 3D celestial sphere in the sky: lit areas reflect simulated sunlight while unlit portions show realistic **earthshine**.
  * Overrides the angular diameter to $4.0^\circ$ for a cinematic, clearly visible lunar circle (overcoming HDRP's default tiny $0.5^\circ$ diameter).
* **Moonlight Illumination & Crossfade**:
  * Direct scene moonlight intensity scales dynamically with the lunar illumination fraction (full moon provides maximum illumination, crescent/new moon is realistically dark).
  * Automatically crossfades with the Sun (moonlight is suppressed when the Sun is high in the sky) and dims under cloud coverage and precipitation.

### 3. Date & Calendar Controls
* **Time Modes (`SunTimeMode`)**:
  * `MetoceanTimestamp`: Uses the timestamp provided by the active data feed (e.g. Open-Meteo or live weather stations).
  * `SystemLocalTime`: Synchronizes celestial bodies with the machine's local time.
  * `SystemUtcTime`: Synchronizes celestial bodies with current UTC time.
  * `CustomTimeOfDay`: Manual time slider (`0.0` to `24.0` hours) for rapid day/night scrubbing.
* **Custom Date Override**:
  * `Use Custom Date` checkbox: Overrides the simulated calendar date.
  * `Custom Year` (e.g. `2026`).
  * `Custom Month` (slider `1`–`12`): Scrub to observe seasonal sun elevation changes.
  * `Custom Day` (slider `1`–`31`): Scrub to observe real-time lunar orbit motion and phase progression.

### 4. HDRP Volumetric Clouds Synchronization & Wind Drift
* **Decoupled HDRP Integration**:
  * Seamlessly controls Unity HDRP's `VolumetricClouds` and `VisualEnvironment` via reflection (preserving zero hard compile-time dependencies on the HDRP package in `Marus.Metocean`).
  * Automatically detects the scene's `Sky and Fog Global Volume` (or accepts a manual volume assignment).
* **Automatic Cloud Presets**:
  * **`Clear`** ($< 5\%$ coverage): Disables volumetric clouds and sets `VisualEnvironment.cloudType = None`, rendering a crystal-clear blue sky with unobstructed views of the Sun, Moon, and stars.
  * **`Sparse`** ($5\% - 35\%$ coverage): Sets HDRP preset to `Sparse` (scattered cumulus formations).
  * **`Cloudy`** ($35\% - 70\%$ coverage): Sets HDRP preset to `Cloudy`.
  * **`Overcast`** ($\ge 70\%$ coverage): Sets HDRP preset to `Overcast`.
  * **`Stormy`** (rain intensity $\ge 0.35$ or heavy storms): Sets HDRP preset to `Stormy`.
* **Wind-Driven Cloud Drift**:
  * Clouds naturally drift across the sky dome driven by the Metocean wind speed and blow-to direction vector, factoring in `TrueNorthOffset`.
  * Smoothly shifts the HDRP `cloudOffset` UV coordinates with configurable speed multiplier.
* **Live Weather Station Cloud Estimation**:
  * `WeatherDisplayClientRawProvider` translates real-time observation descriptions (e.g. `Clear`, `Sunny`, `Partly Cloudy`, `Overcast`, `Rain/Storm`) into continuous cloud coverage values.

### 5. Setting Up Sun, Moon, and Clouds in Unity HDRP
1. **Sun Light**: Assign an existing Directional Light to `Sun Light` on `EnvironmentWeatherAdapter`.
2. **Moon Light**:
   - In the Unity menu, select **`GameObject` → `Light` → `Directional Moon Light`**.
   - Drag this light into the **`Moon Light`** field (or leave it named `Directional Moon Light` for auto-detection).
   - In the Moon's `HDAdditionalLightData` component, under **Celestial Body**, assign the built-in HDRP texture `MoonAlbedo` to **Surface Texture**.
3. **Clouds**:
   - Ensure your scene's Global Volume Profile includes `VolumetricClouds` and `VisualEnvironment`.
   - `EnvironmentWeatherAdapter` automatically finds the volume and syncs cloud coverage and wind drift.

---

## Weather Display (`clientraw.txt`) Format

The `WeatherDisplayClientRawProvider` parses live data formatted according to the **Weather Display Real-Time Clientraw Specification**:
- **Format Overview**: A single-line, space-delimited indexed string generated in real-time by the *Weather Display* software (by Brian Hamilton / Weather Display Live).
- **Used by**: Davis Vantage weather stations, personal weather stations, and meteorological portals (e.g. `sibenik-meteo.hr`, Pljusak).
- **Format Reference**:
  - [Official Weather Display Portal](http://www.weather-display.com)
  - [Home Assistant Weather Display Integration](https://www.home-assistant.io/integrations/weatherdisplay/)

### Key Positional Indices Parsed
| Index | Metric | Raw Units | Converted MARUS Metric |
|---|---|---|---|
| `[1]` | Current wind speed | knots | `WindData.speed` (SI m/s: $v_{\text{knots}} \times 0.514444$) |
| `[2]` | Peak wind gust | knots | `WindData.gustSpeed` (SI m/s) |
| `[3]` | Wind direction | degrees (0–360°) | `WindData.direction` (from True North) |
| `[4]` | Outside temperature | °C | `WeatherStateData.airTemperature` |
| `[5]` | Relative humidity | % (0–100) | `WeatherStateData.relativeHumidity` |
| `[6]` | Barometric pressure | hPa | `WeatherStateData.atmosphericPressure` |
| `[7]` | Daily rain | mm | Normalized rain intensity |
| `[10]` | Current rain rate | mm/hr | `WeatherStateData.rainIntensity` |
| `[32]` | Station identifier | string | Station Name (e.g. `Šubićevac`) |
| `[48]` | Weather condition | string | Condition text (e.g. `Cloudy/Dry`) |
| `[74]` | Update timestamp | string | Station last update time |
| `[150, 151]` | GPS Coordinates | decimal deg | Provider geographic location |

---

## Quick Start

1. Add the `Metocean` component to a GameObject in your scene.
2. Choose your provider:
   - For constant/preset weather, use `ConstantMetoceanProvider`.
   - For live ocean wave/current data, add `OpenMeteoMarineProvider`.
   - For live terrestrial weather, add `WeatherDisplayClientRawProvider`.
   - For both, add a `CompositeMetoceanProvider` and attach both child providers to it.
   - Alternatively, click **"Create Live Adriatic Setup (Open-Meteo + Weather Display)"** in the `Metocean` Inspector to configure everything with one click.
3. Add consumer adapters such as `CrestOceanAdapter` or `EnvironmentWeatherAdapter` to the scene.
4. Query data from any script:
   ```csharp
   using Marus.Metocean;
   using UnityEngine;

   public class VesselSensor : MonoBehaviour
   {
       void Update()
       {
           Vector3 windVel = Metocean.Instance.Wind.ToVelocityVector();
           float waveHeight = Metocean.Instance.Waves.significantWaveHeight;
           float currentSpeed = Metocean.Instance.Current.speed;
           float airTemp = Metocean.Instance.AirTemperature;
           Debug.Log($"Wind: {windVel}, Waves: {waveHeight}m, Current: {currentSpeed}m/s, Temp: {airTemp}°C");
       }
   }
   ```
