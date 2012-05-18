/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Spatial4n.Core.Distance
{
	/// <summary>
	/// Class representing difference distance units, currently only kilometers and
	/// miles
	/// </summary>
	public class DistanceUnits
	{
		//TODO do we need circumference?
		public static DistanceUnits KILOMETERS = new DistanceUnits("km", DistanceUtils.EARTH_MEAN_RADIUS_KM, 40076);
		public static DistanceUnits MILES = new DistanceUnits("miles", DistanceUtils.EARTH_MEAN_RADIUS_MI, 24902);
		public static DistanceUnits RADIANS = new DistanceUnits("radians", 1, Math.PI * 2);//experimental
		public static DistanceUnits CARTESIAN = new DistanceUnits("u", -1, -1);

		private readonly String units;

		private readonly double earthCircumference;

		private readonly double earthRadius;

		/// <summary>
		/// Creates a new DistanceUnit that represents the given unit
		/// </summary>
		/// <param name="units">Distance unit in String form</param>
		/// <param name="earthRadius">Radius of the Earth in the specific distance unit</param>
		/// <param name="earthCircumfence">Circumference of the Earth in the specific distance unit</param>
		DistanceUnits(String units, double earthRadius, double earthCircumfence)
		{
			this.units = units;
			this.earthCircumference = earthCircumfence;
			this.earthRadius = earthRadius;
		}

		/// <summary>
		/// Returns the DistanceUnit which represents the given unit
		/// </summary>
		/// <param name="unit">Unit whose DistanceUnit should be found</param>
		/// <returns>DistanceUnit representing the unit</returns>
		/// <throws>IllegalArgumentException if no DistanceUnit which represents the given unit is found</throws>
		public static DistanceUnits FindDistanceUnit(String unit)
		{
			if (MILES.GetUnits().Equals(unit, StringComparison.InvariantCultureIgnoreCase) || unit.Equals("mi", StringComparison.InvariantCultureIgnoreCase))
			{
				return MILES;
			}
			if (KILOMETERS.GetUnits().Equals(unit, StringComparison.InvariantCultureIgnoreCase))
			{
				return KILOMETERS;
			}
			if (CARTESIAN.GetUnits().Equals(unit, StringComparison.InvariantCultureIgnoreCase) || unit.Length == 0)
			{
				return CARTESIAN;
			}
			throw new ArgumentException("Unknown distance unit " + unit, "unit");
		}

		/// <summary>
		/// Converts the given distance in given DistanceUnit, to a distance in the unit represented by {@code this}
		/// </summary>
		/// <param name="distance">Distance to convert</param>
		/// <param name="from">Unit to convert the distance from</param>
		/// <returns>Given distance converted to the distance in the given unit</returns>
		public double Convert(double distance, DistanceUnits from)
		{
			if (from == this)
			{
				return distance;
			}
			if (this == CARTESIAN || from == CARTESIAN)
			{
				throw new InvalidOperationException("Can't convert cartesian distances: " + from + " -> " + this);
			}
			return (this == MILES) ? distance * DistanceUtils.KM_TO_MILES : distance * DistanceUtils.MILES_TO_KM;
		}

		/// <summary>
		/// Returns the string representation of the distance unit
		/// </summary>
		/// <returns>String representation of the distance unit</returns>
		public String GetUnits()
		{
			return units;
		}

		/// <summary>
		/// Returns the <a href="http://en.wikipedia.org/wiki/Earth_radius">average earth radius</a>
		/// </summary>
		/// <returns>the average earth radius</returns>
		public double EarthRadius()
		{
			return earthRadius;
		}

		/// <summary>
		/// Returns the <a href="http://www.lyberty.com/encyc/articles/earth.html">circumference of the Earth</a>
		/// </summary>
		/// <returns>the circumference of the Earth</returns>
		public double EarthCircumference()
		{
			return earthCircumference;
		}

		public bool IsGeo()
		{
			return earthRadius > 0;
		}

	}
}
