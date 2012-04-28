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
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Util;

namespace Spatial4n.Core.Distance
{
	public static class DistanceUtils
	{
		//pre-compute some angles that are commonly used
		public static readonly double DEG_45_AS_RADS = Math.PI / 4.0;
		public static readonly double SIN_45_AS_RADS = Math.Sin(DEG_45_AS_RADS);
		public static readonly double DEG_90_AS_RADS = Math.PI / 2;
		public static readonly double DEG_180_AS_RADS = Math.PI;
		public static readonly double DEG_225_AS_RADS = 5 * DEG_45_AS_RADS;
		public static readonly double DEG_270_AS_RADS = 3 * DEG_90_AS_RADS;
		public static readonly double DEGREES_TO_RADIANS = Math.PI / 180.0;


		public static readonly double KM_TO_MILES = 0.621371192;
		public static readonly double MILES_TO_KM = 1 / KM_TO_MILES;//1.609

		/**
		 * The International Union of Geodesy and Geophysics says the Earth's mean radius in KM is:
		 *
		 * [1] http://en.wikipedia.org/wiki/Earth_radius
		 */
		public static readonly double EARTH_MEAN_RADIUS_KM = 6371.0087714;
		public static readonly double EARTH_EQUATORIAL_RADIUS_KM = 6378.1370;

		public static readonly double EARTH_MEAN_RADIUS_MI = EARTH_MEAN_RADIUS_KM * KM_TO_MILES;
		public static readonly double EARTH_EQUATORIAL_RADIUS_MI = EARTH_EQUATORIAL_RADIUS_KM * KM_TO_MILES;

		/**
		 * Calculate the p-norm (i.e. length) between two vectors
		 *
		 * @param vec1  The first vector
		 * @param vec2  The second vector
		 * @param power The power (2 for cartesian distance, 1 for manhattan, etc.)
		 * @return The length.
		 *         <p/>
		 *         See http://en.wikipedia.org/wiki/Lp_space
		 * @see #vectorDistance(double[], double[], double, double)
		 */
		public static double VectorDistance(double[] vec1, double[] vec2, double power)
		{
			return VectorDistance(vec1, vec2, power, 1.0 / power);
		}

		/**
 * Calculate the p-norm (i.e. length) between two vectors
 *
 * @param vec1         The first vector
 * @param vec2         The second vector
 * @param power        The power (2 for cartesian distance, 1 for manhattan, etc.)
 * @param oneOverPower If you've precalculated oneOverPower and cached it, use this method to save one division operation over {@link #vectorDistance(double[], double[], double)}.
 * @return The length.
 */
		public static double VectorDistance(double[] vec1, double[] vec2, double power, double oneOverPower)
		{
			double result = 0;

			if (power == 0)
			{
				for (int i = 0; i < vec1.Length; i++)
				{
					result += vec1[i] - vec2[i] == 0 ? 0 : 1;
				}

			}
			else if (power == 1.0)
			{
				for (int i = 0; i < vec1.Length; i++)
				{
					result += vec1[i] - vec2[i];
				}
			}
			else if (power == 2.0)
			{
				result = Math.Sqrt(DistSquaredCartesian(vec1, vec2));
			}
			else if (power == int.MaxValue || Double.IsInfinity(power))
			{//infinite norm?
				for (int i = 0; i < vec1.Length; i++)
				{
					result = Math.Max(result, Math.Max(vec1[i], vec2[i]));
				}
			}
			else
			{
				for (int i = 0; i < vec1.Length; i++)
				{
					result += Math.Pow(vec1[i] - vec2[i], power);
				}
				result = Math.Pow(result, oneOverPower);
			}
			return result;
		}

		/**
		 * Return the coordinates of a vector that is the corner of a box (upper right or lower left), assuming a Rectangular
		 * coordinate system.  Note, this does not apply for points on a sphere or ellipse (although it could be used as an approximation).
		 *
		 * @param center     The center point
		 * @param result Holds the result, potentially resizing if needed.
		 * @param distance   The d from the center to the corner
		 * @param upperRight If true, return the coords for the upper right corner, else return the lower left.
		 * @return The point, either the upperLeft or the lower right
		 */
		public static double[] VectorBoxCorner(double[] center, double[] result, double distance, bool upperRight)
		{
			if (result == null || result.Length != center.Length)
			{
				result = new double[center.Length];
			}
			if (upperRight == false)
			{
				distance = -distance;
			}
			//We don't care about the power here,
			// b/c we are always in a rectangular coordinate system, so any norm can be used by
			//using the definition of sine
			distance = SIN_45_AS_RADS * distance; // sin(Pi/4) == (2^0.5)/2 == opp/hyp == opp/distance, solve for opp, similarly for cosine
			for (int i = 0; i < center.Length; i++)
			{
				result[i] = center[i] + distance;
			}
			return result;
		}

		/**
		 * Given a start point (startLat, startLon) and a bearing on a sphere of radius <i>sphereRadius</i>, return the destination point.
		 *
		 *
		 * @param startLat The starting point latitude, in radians
		 * @param startLon The starting point longitude, in radians
		 * @param distanceRAD The distance to travel along the bearing in radians.
		 * @param bearingRAD The bearing, in radians.  North is a 0, moving clockwise till radians(360).
		 * @param result A preallocated array to hold the results.  If null, a new one is constructed.
		 * @return The destination point, in radians.  First entry is latitude, second is longitude
		 */
		public static double[] PointOnBearingRAD(double startLat, double startLon, double distanceRAD, double bearingRAD, double[] result)
		{
			/*
			lat2 = asin(sin(lat1)*cos(d/R) + cos(lat1)*sin(d/R)*cos(θ))
			lon2 = lon1 + atan2(sin(θ)*sin(d/R)*cos(lat1), cos(d/R)−sin(lat1)*sin(lat2))

			 */
			double cosAngDist = Math.Cos(distanceRAD);
			double cosStartLat = Math.Cos(startLat);
			double sinAngDist = Math.Sin(distanceRAD);
			double sinStartLat = Math.Sin(startLat);
			double lat2 = Math.Asin(sinStartLat * cosAngDist +
					cosStartLat * sinAngDist * Math.Cos(bearingRAD));

			double lon2 = startLon + Math.Atan2(Math.Sin(bearingRAD) * sinAngDist * cosStartLat,
					cosAngDist - sinStartLat * Math.Sin(lat2));

			/*lat2 = (lat2*180)/Math.PI;
			lon2 = (lon2*180)/Math.PI;*/

			// normalize lon first
			if (result == null || result.Length != 2)
			{
				result = new double[2];
			}
			result[0] = lat2;
			result[1] = lon2;
			NormLngRAD(result);

			// normalize lat - could flip poles
			NormLatRAD(result);
			return result;
		}

		/**
		 * @param latLng The lat/lon, in radians. lat in position 0, lon in position 1
		 */
		public static void NormLatRAD(double[] latLng)
		{

			if (latLng[0] > DEG_90_AS_RADS)
			{
				latLng[0] = DEG_90_AS_RADS - (latLng[0] - DEG_90_AS_RADS);
				if (latLng[1] < 0)
				{
					latLng[1] = latLng[1] + DEG_180_AS_RADS;
				}
				else
				{
					latLng[1] = latLng[1] - DEG_180_AS_RADS;
				}
			}
			else if (latLng[0] < -DEG_90_AS_RADS)
			{
				latLng[0] = -DEG_90_AS_RADS - (latLng[0] + DEG_90_AS_RADS);
				if (latLng[1] < 0)
				{
					latLng[1] = latLng[1] + DEG_180_AS_RADS;
				}
				else
				{
					latLng[1] = latLng[1] - DEG_180_AS_RADS;
				}
			}

		}

		/**
		 * Returns a normalized Lng rectangle shape for the bounding box
		 *
		 * @param latLng The lat/lon, in radians, lat in position 0, lon in position 1
		 */
		//@Deprecated
		public static void NormLngRAD(double[] latLng) {
		  if (latLng[1] > DEG_180_AS_RADS) {
		    latLng[1] = -1.0 * (DEG_180_AS_RADS - (latLng[1] - DEG_180_AS_RADS));
		  } else if (latLng[1] < -DEG_180_AS_RADS) {
		    latLng[1] = (latLng[1] + DEG_180_AS_RADS) + DEG_180_AS_RADS;
		  }
		}

		/**
		 * Puts in range -180 <= lon_deg < +180.
		 */
		public static double NormLonDEG(double lon_deg)
		{
			if (lon_deg >= -180 && lon_deg < 180)
				return lon_deg;//common case, and avoids slight double precision shifting
			double off = (lon_deg + 180) % 360;
			return off < 0 ? 180 + off : -180 + off;
		}

		/**
		 * Puts in range -90 <= lat_deg <= 90.
		 */
		public static double NormLatDEG(double lat_deg)
		{
			if (lat_deg >= -90 && lat_deg <= 90)
				return lat_deg;//common case, and avoids slight double precision shifting
			double off = Math.Abs((lat_deg + 90) % 360);
			return (off <= 180 ? off : 360 - off) - 90;
		}

		public static Rectangle CalcBoxByDistFromPtDEG(double lat, double lon, double distance, SpatialContext ctx)
		{
			//See http://janmatuschek.de/LatitudeLongitudeBoundingCoordinates Section 3.1, 3.2 and 3.3

			double radius = ctx.GetUnits().EarthRadius();
			double dist_rad = distance / radius;
			double dist_deg = MathHelper.ToDegrees(dist_rad);

			if (dist_deg == 0)
				return ctx.MakeRect(lon, lon, lat, lat);

			if (dist_deg >= 180)//distance is >= opposite side of the globe
				return ctx.GetWorldBounds();

			//--calc latitude bounds
			double latN_deg = lat + dist_deg;
			double latS_deg = lat - dist_deg;

			if (latN_deg >= 90 || latS_deg <= -90)
			{//touches either pole
				//we have special logic for longitude
				double lonW_deg = -180, lonE_deg = 180;//world wrap: 360 deg
				if (latN_deg <= 90 && latS_deg >= -90)
				{//doesn't pass either pole: 180 deg
					lonW_deg = lon - 90;
					lonE_deg = lon + 90;
				}
				if (latN_deg > 90)
					latN_deg = 90;
				if (latS_deg < -90)
					latS_deg = -90;

				return ctx.MakeRect(lonW_deg, lonE_deg, latS_deg, latN_deg);
			}
			else
			{
				//--calc longitude bounds
				double lon_delta_deg = CalcBoxByDistFromPtVertAxisOffsetDEG(lat, lon, distance, radius);

				double lonW_deg = lon - lon_delta_deg;
				double lonE_deg = lon + lon_delta_deg;

				return ctx.MakeRect(lonW_deg, lonE_deg, latS_deg, latN_deg);//ctx will normalize longitude
			}
		}

		public static double CalcBoxByDistFromPtVertAxisOffsetDEG(double lat, double lon, double distance, double radius)
		{
			//http://gis.stackexchange.com/questions/19221/find-tangent-point-on-circle-furthest-east-or-west
			if (distance == 0)
				return 0;
			double lat_rad = ToRadians(lat);
			double dist_rad = distance / radius;
			double result_rad = Math.Asin(Math.Sin(dist_rad) / Math.Cos(lat_rad));

			if (!Double.IsNaN(result_rad))
				return MathHelper.ToDegrees(result_rad);
			return 90;
		}

		public static double CalcBoxByDistFromPtHorizAxisDEG(double lat, double lon, double distance, double radius)
		{
			//http://gis.stackexchange.com/questions/19221/find-tangent-point-on-circle-furthest-east-or-west
			if (distance == 0)
				return lat;
			double lat_rad = ToRadians(lat);
			double dist_rad = distance / radius;
			double result_rad = Math.Asin(Math.Sin(lat_rad) / Math.Cos(dist_rad));
			if (!Double.IsNaN(result_rad))
				return MathHelper.ToDegrees(result_rad);
			//TODO should we use use ctx.getBoundaryNudgeDegrees() offsets here or let caller?
			if (lat > 0)
				return 90;
			if (lat < 0)
				return -90;
			return lat;
		}

		/**
		 * The square of the cartesian Distance.  Not really a distance, but useful if all that matters is
		 * comparing the result to another one.
		 *
		 * @param vec1 The first point
		 * @param vec2 The second point
		 * @return The squared cartesian distance
		 */
		public static double DistSquaredCartesian(double[] vec1, double[] vec2)
		{
			double result = 0;
			for (int i = 0; i < vec1.Length; i++)
			{
				double v = vec1[i] - vec2[i];
				result += v * v;
			}
			return result;
		}

		/**
		 *
		 * @param lat1     The y coordinate of the first point, in radians
		 * @param lon1     The x coordinate of the first point, in radians
		 * @param lat2     The y coordinate of the second point, in radians
		 * @param lon2     The x coordinate of the second point, in radians
		 * @return The distance between the two points, as determined by the Haversine formula, in radians.
		 */
		public static double DistHaversineRAD(double lat1, double lon1, double lat2, double lon2)
		{
			//TODO investigate slightly different formula using asin() and min() http://www.movable-type.co.uk/scripts/gis-faq-5.1.html

			// Check for same position
			if (lat1 == lat2 && lon1 == lon2)
				return 0.0;
			double hsinX = Math.Sin((lon1 - lon2) * 0.5);
			double hsinY = Math.Sin((lat1 - lat2) * 0.5);
			double h = hsinY * hsinY +
					(Math.Cos(lat1) * Math.Cos(lat2) * hsinX * hsinX);
			return 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
		}

		/**
		 * Calculates the distance between two lat/lng's using the Law of Cosines. Due to numeric conditioning
		 * errors, it is not as accurate as the Haversine formula for small distances.  But with
		 * double precision, it isn't that bad -- <a href="http://www.movable-type.co.uk/scripts/latlong.html">
		 *   allegedly 1 meter</a>.
		 * <p/>
		 * See <a href="http://gis.stackexchange.com/questions/4906/why-is-law-of-cosines-more-preferable-than-haversine-when-calculating-distance-b">
		 *  Why is law of cosines more preferable than haversine when calculating distance between two latitude-longitude points?</a>
		 * <p/>
		 * The arguments and return value are in radians.
		 */
		public static double DistLawOfCosinesRAD(double lat1, double lon1, double lat2, double lon2)
		{
			//TODO validate formula

			//(MIGRATED FROM org.apache.lucene.spatial.geometry.LatLng.arcDistance()) (Lucene 3x)
			// Imported from mq java client.  Variable references changed to match.

			// Check for same position
			if (lat1 == lat2 && lon1 == lon2)
				return 0.0;

			// Get the m_dLongitude difference. Don't need to worry about
			// crossing 180 since cos(x) = cos(-x)
			double dLon = lon2 - lon1;

			double a = DEG_90_AS_RADS - lat1;
			double c = DEG_90_AS_RADS - lat2;
			double cosB = (Math.Cos(a) * Math.Cos(c))
				+ (Math.Sin(a) * Math.Sin(c) * Math.Cos(dLon));

			// Find angle subtended (with some bounds checking) in radians
			if (cosB < -1.0)
				return Math.PI;
			else if (cosB >= 1.0)
				return 0;
			else
				return Math.Acos(cosB);
		}

		/**
		 * Calculates the great circle distance using the Vincenty Formula, simplified for a spherical model. This formula
		 * is accurate for any pair of points. The equation
		 * was taken from <a href="http://en.wikipedia.org/wiki/Great-circle_distance">Wikipedia</a>.
		 * <p/>
		 * The arguments are in radians, and the result is in radians.
		 */
		public static double DistVincentyRAD(double lat1, double lon1, double lat2, double lon2)
		{
			// Check for same position
			if (lat1 == lat2 && lon1 == lon2)
				return 0.0;

			double cosLat1 = Math.Cos(lat1);
			double cosLat2 = Math.Cos(lat2);
			double sinLat1 = Math.Sin(lat1);
			double sinLat2 = Math.Sin(lat2);
			double dLon = lon2 - lon1;
			double cosDLon = Math.Cos(dLon);
			double sinDLon = Math.Sin(dLon);

			double a = cosLat2 * sinDLon;
			double b = cosLat1 * sinLat2 - sinLat1 * cosLat2 * cosDLon;
			double c = sinLat1 * sinLat2 + cosLat1 * cosLat2 * cosDLon;

			return Math.Atan2(Math.Sqrt(a * a + b * b), c);
		}

		/**
		 * Converts a distance in the units of the radius to degrees (360 degrees are in a circle). A spherical
		 * earth model is assumed.
		 */
		public static double Dist2Degrees(double dist, double radius)
		{
			return MathHelper.ToDegrees(Dist2Radians(dist, radius));
		}

		/**
		 * Converts a distance in the units of the radius to radians (multiples of the radius). A spherical
		 * earth model is assumed.
		 */
		public static double Dist2Radians(double dist, double radius)
		{
			return dist / radius;
		}

		public static double Radians2Dist(double radians, double radius)
		{
			return radians * radius;
		}

		/**
		 * About 3x faster Math.toRadians().  See CompareRadiansSnippit
		 */
		public static double ToRadians(double deg)
		{
			return deg * DEGREES_TO_RADIANS;
		}
	}
}
