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
    /// Represents ocean current velocity and direction.
    /// Direction adheres to oceanographic convention (degrees clockwise from True North that the current is flowing TOWARDS).
    /// </summary>
    [Serializable]
    public struct OceanCurrentData : IEquatable<OceanCurrentData>
    {
        [Tooltip("Current speed in meters per second (m/s).")]
        [Min(0f)]
        public float speed;

        [Tooltip("Oceanographic current direction (degrees clockwise from True North that water is flowing TOWARDS). 0 = North, 90 = East, 180 = South, 270 = West.")]
        [Range(0f, 360f)]
        public float direction;

        [Tooltip("Depth in meters at which this current applies (0 = surface).")]
        [Min(0f)]
        public float depth;

        public OceanCurrentData(float speed, float direction, float depth = 0f)
        {
            this.speed = Mathf.Max(0f, speed);
            this.direction = (direction % 360f + 360f) % 360f;
            this.depth = Mathf.Max(0f, depth);
        }

        /// <summary>
        /// Current speed converted to knots.
        /// </summary>
        public float SpeedKnots => speed * 1.943844f;

        /// <summary>
        /// Normalized direction vector in Unity coordinates (+Z = North, +X = East) pointing where the current flows.
        /// </summary>
        public Vector3 ToDirectionVector()
        {
            float rad = direction * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        /// <summary>
        /// Velocity vector in Unity coordinates (+Z = North, +X = East) with magnitude equal to speed.
        /// </summary>
        public Vector3 ToVelocityVector()
        {
            return ToDirectionVector() * speed;
        }

        /// <summary>
        /// Creates OceanCurrentData from a horizontal velocity vector in Unity world coordinates (+Z = North, +X = East).
        /// </summary>
        public static OceanCurrentData FromVelocity(Vector3 velocity, float depth = 0f)
        {
            velocity.y = 0f;
            float spd = velocity.magnitude;
            if (spd < 0.0001f)
            {
                return new OceanCurrentData(0f, 0f, depth);
            }

            float dir = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
            return new OceanCurrentData(spd, dir, depth);
        }

        public static OceanCurrentData Default => new OceanCurrentData(0f, 0f, 0f);

        public bool Equals(OceanCurrentData other)
        {
            return Mathf.Approximately(speed, other.speed) &&
                   Mathf.Approximately(direction, other.direction) &&
                   Mathf.Approximately(depth, other.depth);
        }

        public override bool Equals(object obj) => obj is OceanCurrentData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(speed, direction, depth);

        public override string ToString() => $"Current: {speed:F2} m/s ({SpeedKnots:F2} kts) towards {direction:F0}° at {depth:F1}m depth";
    }
}

