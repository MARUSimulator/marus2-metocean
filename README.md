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
  - `EnvironmentWeatherAdapter`: Controls scene fog, rain particles, wind zones, directional Sun orientation, and time-of-day solar lighting.
- **MARUS Standard**: Implements `Marus.Utils.Singleton<Metocean>`, uses geodetic coordinates via `Marus.Core.GeoPoint`, and provides full spatial and global queries.

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
