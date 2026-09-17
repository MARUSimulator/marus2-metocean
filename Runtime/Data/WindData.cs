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
    /// Represents wind state with speed, direction, and gusting.
    /// Direction adheres to meteorological convention (direction wind is blowing FROM in degrees clockwise from North).
    /// </summary>
    [Serializable]
    public struct WindData : IEquatable<WindData>
    {
        [Tooltip("Wind speed in meters per second (m/s).")]
        [Min(0f)]
        public float speed;

        [Tooltip("Meteorological wind direction (degrees clockwise from True North that wind is blowing FROM). 0 = North, 90 = East, 180 = South, 270 = West.")]
        [Range(0f, 360f)]
        public float direction;

        [Tooltip("Wind gust speed in meters per second (m/s).")]
        [Min(0f)]
        public float gustSpeed;

        public WindData(float speed, float direction, float gustSpeed = 0f)
        {
            this.speed = Mathf.Max(0f, speed);
            this.direction = (direction % 360f + 360f) % 360f;
            this.gustSpeed = Mathf.Max(this.speed, gustSpeed);
        }

        /// <summary>
        /// Direction angle in degrees that the wind is blowing TOWARDS (clockwise from North).
        /// </summary>
        public float BlowToDirectionDegrees => (direction + 180f) % 360f;

        /// <summary>
        /// Wind speed converted to knots.
        /// </summary>
        public float SpeedKnots => speed * 1.943844f;

        /// <summary>
        /// Wind speed converted to km/h.
        /// </summary>
        public float SpeedKmH => speed * 3.6f;

        /// <summary>
        /// Returns normalized direction vector in Unity world coordinates (+Z = North, +X = East) pointing where the wind is blowing towards.
        /// </summary>
        public Vector3 ToDirectionVector()
        {
            float rad = BlowToDirectionDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        /// <summary>
        /// Returns velocity vector in Unity world coordinates (+Z = North, +X = East) with magnitude equal to speed.
        /// </summary>
        public Vector3 ToVelocityVector()
        {
            return ToDirectionVector() * speed;
        }

        /// <summary>
        /// Creates WindData from a horizontal velocity vector in Unity world coordinates (+Z = North, +X = East).
        /// </summary>
        public static WindData FromVelocity(Vector3 velocity)
        {
            velocity.y = 0f;
            float spd = velocity.magnitude;
            if (spd < 0.0001f)
            {
                return new WindData(0f, 0f, 0f);
            }

            // Direction vector points where wind blows towards.
            // Angle clockwise from +Z (North):
            float toAngle = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            float fromAngle = (toAngle + 180f) % 360f;
            return new WindData(spd, fromAngle, spd);
        }

        public static WindData Default => new WindData(0f, 0f, 0f);

        public bool Equals(WindData other)
        {
            return Mathf.Approximately(speed, other.speed) &&
                   Mathf.Approximately(direction, other.direction) &&
                   Mathf.Approximately(gustSpeed, other.gustSpeed);
        }

        public override bool Equals(object obj) => obj is WindData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(speed, direction, gustSpeed);

        public override string ToString() => $"Wind: {speed:F1} m/s ({SpeedKnots:F1} kts) from {direction:F0}°, Gusts: {gustSpeed:F1} m/s";
    }
}

