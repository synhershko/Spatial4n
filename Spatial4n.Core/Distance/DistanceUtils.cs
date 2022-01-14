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

using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using System;

namespace Spatial4n.Core.Distance
{
    /// <summary>
    /// Various distance calculations and constants. To the extent possible, a 
    /// <see cref="IDistanceCalculator"/>, retrieved from <see cref="SpatialContext.DistCalc"/>
    /// should be used in preference to calling these methods directly.
    /// <para>
    /// This code came from <a href="https://issues.apache.org/jira/browse/LUCENE-1387">Apache
    /// Lucene, LUCENE-1387</a>, which in turn came from "LocalLucene".
    /// </para>
    /// </summary>
    public static class DistanceUtils
    {
        //pre-compute some angles that are commonly used
        //[Obsolete]
        //public const double Degrees45AsRadians = Math.PI / 4;
        //[Obsolete]
        //public static readonly double Sine45AsRadians = Math.Sin(Degrees45AsRadians);

        public const double Degrees90AsRadians = Math.PI / 2;
        public const double Degrees180AsRadians = Math.PI;

        //[Obsolete]
        //public const double Degrees225AsRadians = 5 * Degrees45AsRadians;
        //[Obsolete]
        //public const double Degrees270AsRadians = 3 * Degrees90AsRadians;

        public const double DegreesToRadians = Math.PI / 180;
        public const double RadiansToDegrees = 1 / DegreesToRadians;

        public const double KilometersToMiles = 0.621371192;
        public const double MilesToKilometers = 1 / KilometersToMiles;//1.609

        /// <summary>
        /// The International Union of Geodesy and Geophysics says the Earth's mean radius in KM is:
        ///
        /// [1] http://en.wikipedia.org/wiki/Earth_radius
        /// </summary>
        public const double EarthMeanRadiusKilometers = 6371.0087714;
        public const double EarthEquatorialRadiusKilometers = 6378.1370;

        /// <summary>
        /// Equivalent to Degrees2Dist(1, EarthMeanRadiusKilometers)
        /// </summary>
        public const double DegreesToKilometers = DegreesToRadians * EarthMeanRadiusKilometers;
        public const double KilometersToDegrees = 1 / DegreesToKilometers;

        public const double EarthMeanRadiusMiles = EarthMeanRadiusKilometers * KilometersToMiles;
        public const double EarthEquatorialRadiusMiles = EarthEquatorialRadiusKilometers * KilometersToMiles;


        //pre-compute some angles that are commonly used
        [Obsolete, System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double DEG_45_AS_RADS = Math.PI / 4;
        [Obsolete, System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double SIN_45_AS_RADS = Math.Sin(DEG_45_AS_RADS);

        [Obsolete("Use None instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double DEG_90_AS_RADS = Math.PI / 2;
        [Obsolete("Use None instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double DEG_180_AS_RADS = Math.PI;

        [Obsolete, System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double DEG_225_AS_RADS = 5 * DEG_45_AS_RADS;
        [Obsolete, System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double DEG_270_AS_RADS = 3 * DEG_90_AS_RADS;

        [Obsolete("Use DegreesToRadians instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double DEGREES_TO_RADIANS = Math.PI / 180;
        [Obsolete("Use RadiansToDegrees instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double RADIANS_TO_DEGREES = 1 / DEGREES_TO_RADIANS;

        [Obsolete("Use KilometersToMiles instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double KM_TO_MILES = 0.621371192;
        [Obsolete("Use MilesToKilometers instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double MILES_TO_KM = 1 / KM_TO_MILES;//1.609

        [Obsolete("Use EarthMeanRadiusKilometers instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double EARTH_MEAN_RADIUS_KM = 6371.0087714;
        [Obsolete("Use EarthEquatorialRadiusKilometers instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double EARTH_EQUATORIAL_RADIUS_KM = 6378.1370;

        [Obsolete("Use DegreesToKilometers instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double DEG_TO_KM = DEGREES_TO_RADIANS * EARTH_MEAN_RADIUS_KM;
        [Obsolete("Use KilometersToDegrees instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double KM_TO_DEG = 1 / DEG_TO_KM;

        [Obsolete("Use EarthMeanRadiusMiles instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double EARTH_MEAN_RADIUS_MI = EARTH_MEAN_RADIUS_KM * KM_TO_MILES;
        [Obsolete("Use EarthEquatorialRadiusMiles instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static readonly double EARTH_EQUATORIAL_RADIUS_MI = EARTH_EQUATORIAL_RADIUS_KM * KM_TO_MILES;

        /// <summary>
        /// Calculate the p-norm (i.e. length) between two vectors
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <param name="power">The power (2 for cartesian distance, 1 for manhattan, etc.)</param>
        /// <returns>The length. See http://en.wikipedia.org/wiki/Lp_space </returns>
        /// <seealso cref="VectorDistance(double[], double[], double, double)"/>
        [Obsolete, System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static double VectorDistance(double[] vec1, double[] vec2, double power)
        {
            //only calc oneOverPower if it's needed
            double oneOverPower = (power == 0 || power == 1.0 || power == 2.0) ? double.NaN : 1.0 / power;
            return VectorDistance(vec1, vec2, power, oneOverPower);
        }

        /// <summary>
        /// Calculate the p-norm (i.e. length) between two vectors
        /// </summary>
        /// <param name="vec1">The first vector</param>
        /// <param name="vec2">The second vector</param>
        /// <param name="power">The power (2 for cartesian distance, 1 for manhattan, etc.)</param>
        /// <param name="oneOverPower">If you've precalculated <paramref name="oneOverPower"/> and cached it, 
        /// use this method to save one division operation over <seealso cref="VectorDistance(double[], double[], double)"/>.</param>
        /// <returns>The length.</returns>
        [Obsolete, System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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
                    result += Math.Abs(vec1[i] - vec2[i]);
                }
            }
            else if (power == 2.0)
            {
                result = Math.Sqrt(DistSquaredCartesian(vec1, vec2));
            }
            else if (power == int.MaxValue || double.IsInfinity(power))
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

        /// <summary>
        /// Return the coordinates of a vector that is the corner of a box (upper right or lower left), assuming a Rectangular
        /// coordinate system.  Note, this does not apply for points on a sphere or ellipse (although it could be used as an approximation).
        /// </summary>
        /// <param name="center">The center point</param>
        /// <param name="result">Holds the result, potentially resizing if needed.</param>
        /// <param name="distance">The distance from the center to the corner</param>
        /// <param name="upperRight">If true, return the coords for the upper right corner, else return the lower left.</param>
        /// <returns>The point, either the upperLeft or the lower right</returns>
        [Obsolete, System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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

        /// <summary>
        /// Given a start point (startLat, startLon) and a bearing on a sphere of radius <i>sphereRadius</i>, return the destination point.
        /// </summary>
        /// <param name="startLat">The starting point latitude, in radians</param>
        /// <param name="startLon">The starting point longitude, in radians</param>
        /// <param name="distanceRAD">The distance to travel along the bearing in radians.</param>
        /// <param name="bearingRAD">The bearing, in radians.  North is a 0, moving clockwise till radians(360).</param>
        /// <param name="ctx"></param>
        /// <param name="reuse">A preallocated object to hold the results.</param>
        /// <returns>The destination point, in radians.  First entry is latitude, second is longitude</returns>
        public static IPoint PointOnBearingRAD(double startLat, double startLon, double distanceRAD, double bearingRAD, SpatialContext ctx, IPoint? reuse)
        {
            /*
               lat2 = asin(sin(lat1)*cos(d/R) + cos(lat1)*sin(d/R)*cos(θ))
             lon2 = lon1 + atan2(sin(θ)*sin(d/R)*cos(lat1), cos(d/R)−sin(lat1)*sin(lat2))
              */
            double cosAngDist = Math.Cos(distanceRAD);
            double cosStartLat = Math.Cos(startLat);
            double sinAngDist = Math.Sin(distanceRAD);
            double sinStartLat = Math.Sin(startLat);
            double sinLat2 = sinStartLat * cosAngDist +
                cosStartLat * sinAngDist * Math.Cos(bearingRAD);
            double lat2 = Math.Asin(sinLat2);
            double lon2 = startLon + Math.Atan2(Math.Sin(bearingRAD) * sinAngDist * cosStartLat,
                cosAngDist - sinStartLat * sinLat2);

            // normalize lon first
            if (lon2 > Degrees180AsRadians)
            {
                lon2 = -1.0 * (Degrees180AsRadians - (lon2 - Degrees180AsRadians));
            }
            else if (lon2 < -Degrees180AsRadians)
            {
                lon2 = (lon2 + Degrees180AsRadians) + Degrees180AsRadians;
            }

            // normalize lat - could flip poles
            if (lat2 > Degrees90AsRadians)
            {
                lat2 = Degrees90AsRadians - (lat2 - Degrees90AsRadians);
                if (lon2 < 0)
                {
                    lon2 = lon2 + Degrees180AsRadians;
                }
                else
                {
                    lon2 = lon2 - Degrees180AsRadians;
                }
            }
            else if (lat2 < -Degrees90AsRadians)
            {
                lat2 = -Degrees90AsRadians - (lat2 + Degrees90AsRadians);
                if (lon2 < 0)
                {
                    lon2 = lon2 + Degrees180AsRadians;
                }
                else
                {
                    lon2 = lon2 - Degrees180AsRadians;
                }
            }

            if (reuse is null)
            {
                return ctx.MakePoint(lon2, lat2);
            }
            else
            {
                reuse.Reset(lon2, lat2); //x y
                return reuse;
            }
        }

        /// <summary>
        /// Puts in range -180 &lt;= lon_deg &lt;= +180.
        /// </summary>
        /// <param name="lon_deg"></param>
        /// <returns></returns>
        public static double NormLonDEG(double lon_deg)
        {
            if (lon_deg >= -180 && lon_deg <= 180)
                return lon_deg; //common case, and avoids slight double precision shifting
            double off = (lon_deg + 180) % 360;
            if (off < 0)
                return 180 + off;
            else if (off == 0 && lon_deg > 0)
                return 180;
            else
                return -180 + off;
        }

        /// <summary>
        /// Puts in range -90 &lt;= lat_deg &lt;= 90.
        /// </summary>
        /// <param name="lat_deg"></param>
        /// <returns></returns>
        public static double NormLatDEG(double lat_deg)
        {
            if (lat_deg >= -90 && lat_deg <= 90)
                return lat_deg;//common case, and avoids slight double precision shifting
            double off = Math.Abs((lat_deg + 90) % 360);
            return (off <= 180 ? off : 360 - off) - 90;
        }

        /// <summary>
        /// Calculates the bounding box of a circle, as specified by its center point
        /// and distance. <paramref name="reuse"/> is an optional argument to store the
        /// results to avoid object creation.
        /// </summary>
        public static IRectangle CalcBoxByDistFromPtDEG(double lat, double lon, double distDEG, SpatialContext ctx, IRectangle? reuse)
        {
            //See http://janmatuschek.de/LatitudeLongitudeBoundingCoordinates Section 3.1, 3.2 and 3.3
            double minX;
            double maxX;
            double minY;
            double maxY;
            if (distDEG == 0)
            {
                minX = lon;
                maxX = lon;
                minY = lat;
                maxY = lat;
            }
            else if (distDEG >= 180)
            {
                //distance is >= opposite side of the globe
                minX = -180;
                maxX = 180;
                minY = -90;
                maxY = 90;
            }
            else
            {
                //--calc latitude bounds
                maxY = lat + distDEG;
                minY = lat - distDEG;

                if (maxY >= 90 || minY <= -90)
                {
                    //touches either pole
                    //we have special logic for longitude
                    minX = -180;
                    maxX = 180; //world wrap: 360 deg
                    if (maxY <= 90 && minY >= -90)
                    {
                        //doesn't pass either pole: 180 deg
                        minX = NormLonDEG(lon - 90);
                        maxX = NormLonDEG(lon + 90);
                    }
                    if (maxY > 90)
                        maxY = 90;
                    if (minY < -90)
                        minY = -90;
                }
                else
                {
                    //--calc longitude bounds
                    double lon_delta_deg = CalcBoxByDistFromPt_deltaLonDEG(lat, lon, distDEG);

                    minX = NormLonDEG(lon - lon_delta_deg);
                    maxX = NormLonDEG(lon + lon_delta_deg);
                }
            }
            if (reuse is null)
            {
                return ctx.MakeRectangle(minX, maxX, minY, maxY);
            }
            else
            {
                reuse.Reset(minX, maxX, minY, maxY);
                return reuse;
            }
        }

        /// <summary>
        /// The delta longitude of a point-distance. In other words, half the width of
        /// the bounding box of a circle.
        /// </summary>
        public static double CalcBoxByDistFromPt_deltaLonDEG(double lat, double lon, double distDEG)
        {
            //http://gis.stackexchange.com/questions/19221/find-tangent-point-on-circle-furthest-east-or-west
            if (distDEG == 0)
                return 0;
            double lat_rad = ToRadians(lat);
            double dist_rad = ToRadians(distDEG);
            double result_rad = Math.Asin(Math.Sin(dist_rad) / Math.Cos(lat_rad));

            if (!double.IsNaN(result_rad))
                return ToDegrees(result_rad);
            return 90;
        }

        /// <summary>
        /// The latitude of the horizontal axis (e.g. left-right line)
        /// of a circle.  The horizontal axis of a circle passes through its furthest
        /// left-most and right-most edges. On a 2D plane, this result is always
        /// <c>from.Y</c> but, perhaps surprisingly, on a sphere it is going
        /// to be slightly different.
        /// </summary>
        public static double CalcBoxByDistFromPt_latHorizAxisDEG(double lat, double lon, double distDEG)
        {
            //http://gis.stackexchange.com/questions/19221/find-tangent-point-on-circle-furthest-east-or-west
            if (distDEG == 0)
                return lat;
            // if we don't do this when == 90 or -90, computed result can be (+/-)89.9999 when at pole.
            //     No biggie but more accurate.
            else if (lat + distDEG >= 90)
                return 90;
            else if (lat - distDEG <= -90)
                return -90;

            double lat_rad = ToRadians(lat);
            double dist_rad = ToRadians(distDEG);
            double result_rad = Math.Asin(Math.Sin(lat_rad) / Math.Cos(dist_rad));
            if (!double.IsNaN(result_rad))
                return ToDegrees(result_rad);
            if (lat > 0)
                return 90;
            if (lat < 0)
                return -90;
            return lat;
        }

        /// <summary>
        /// Calculates the degrees longitude distance at latitude <paramref name="lat"/> to cover
        /// a distance <paramref name="dist"/>.
        /// <para>
        /// Used to calculate a new expanded buffer distance to account for skewing
        /// effects for shapes that use the lat-lon space as a 2D plane instead of a
        /// sphere. The expanded buffer will be sure to cover the intended area, but
        /// the shape is still skewed and so it will cover a larger area. For latitude
        /// 0 (the equator) the result is the same buffer. At 60 (or -60) degrees, the
        /// result is twice the buffer, meaning that a shape at 60 degrees is twice as
        /// high as it is wide when projected onto a lat-lon plane even if in the real
        /// world it's equal all around.
        /// </para>
        /// If the result added to abs(<paramref name="lat"/>) is &gt;= 90 degrees, then skewing is
        /// so severe that the caller should consider tossing the shape and
        /// substituting a spherical cap instead.
        /// </summary>
        /// <param name="lat">latitude in degrees</param>
        /// <param name="dist">distance in degrees</param>
        /// <returns>longitudinal degrees (x delta) at input latitude that is &gt;= 
        /// <paramref name="dist"/> distance. Will be &gt;= dist and &lt;= 90.</returns>
        public static double CalcLonDegreesAtLat(double lat, double dist)
        {
            //This code was pulled out of DistanceUtils.pointOnBearingRAD() and
            // optimized
            // for bearing = 90 degrees, and so we can get an intermediate calculation.
            double distanceRAD = DistanceUtils.ToRadians(dist);
            double startLat = DistanceUtils.ToRadians(lat);

            double cosAngDist = Math.Cos(distanceRAD);
            double cosStartLat = Math.Cos(startLat);
            double sinAngDist = Math.Sin(distanceRAD);
            double sinStartLat = Math.Sin(startLat);

            double lonDelta = Math.Atan2(sinAngDist * cosStartLat,
                cosAngDist * (1 - sinStartLat * sinStartLat));

            return DistanceUtils.ToDegrees(lonDelta);
        }

        /// <summary>
        /// The square of the cartesian Distance.  Not really a distance, but useful if all that matters is
        /// comparing the result to another one.
        /// </summary>
        /// <param name="vec1">The first point</param>
        /// <param name="vec2">The second point</param>
        /// <returns>The squared cartesian distance</returns>
        [Obsolete, System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lat1">The y coordinate of the first point, in radians</param>
        /// <param name="lon1">The x coordinate of the first point, in radians</param>
        /// <param name="lat2">The y coordinate of the second point, in radians</param>
        /// <param name="lon2">The x coordinate of the second point, in radians</param>
        /// <returns>The distance between the two points, as determined by the Haversine formula, in radians.</returns>
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

        /// <summary>
        /// Calculates the distance between two lat/lng's using the Law of Cosines. Due to numeric conditioning
        /// errors, it is not as accurate as the Haversine formula for small distances.  But with
        /// double precision, it isn't that bad -- <a href="http://www.movable-type.co.uk/scripts/latlong.html">
        /// allegedly 1 meter</a>.
        /// <para>
        /// See <a href="http://gis.stackexchange.com/questions/4906/why-is-law-of-cosines-more-preferable-than-haversine-when-calculating-distance-b">
        /// Why is law of cosines more preferable than haversine when calculating distance between two latitude-longitude points?</a>
        /// </para>
        /// The arguments and return value are in radians.
        /// </summary>
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

            double a = Degrees90AsRadians - lat1;
            double c = Degrees90AsRadians - lat2;
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

        /// <summary>
        /// Calculates the great circle distance using the Vincenty Formula, simplified for a spherical model. This formula
        /// is accurate for any pair of points. The equation
        /// was taken from <a href="http://en.wikipedia.org/wiki/Great-circle_distance">Wikipedia</a>.
        /// <para>
        /// The arguments are in radians, and the result is in radians.
        /// </para>
        /// </summary>
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

        /// <summary>
        /// Converts a distance in the units of the radius to degrees (360 degrees are
        /// in a circle). A spherical earth model is assumed.
        /// </summary>
        public static double Dist2Degrees(double dist, double radius)
        {
            return ToDegrees(Dist2Radians(dist, radius));
        }

        /// <summary>
        /// Converts <paramref name="degrees"/> (1/360th of circumference of a circle) into a
        /// distance as measured by the units of the radius.  A spherical earth model
        /// is assumed.
        /// </summary>
        public static double Degrees2Dist(double degrees, double radius)
        {
            return Radians2Dist(ToRadians(degrees), radius);
        }

        /// <summary>
        /// Converts a distance in the units of <paramref name="radius"/> (e.g. kilometers)
        /// to radians (multiples of the radius). A spherical earth model is assumed.
        /// </summary>
        public static double Dist2Radians(double dist, double radius)
        {
            return dist / radius;
        }

        /// <summary>
        /// Converts <paramref name="radians"/> (multiples of the <paramref name="radius"/>) to
        /// distance in the units of the radius (e.g. kilometers).
        /// </summary>
        public static double Radians2Dist(double radians, double radius)
        {
            return radians * radius;
        }

        /// <summary>
        /// Same as Java's <c>Math.toRadians(double)</c> but 3x faster (multiply vs. divide).
        /// See CompareRadiansSnippet.java in tests.
        /// </summary>
        public static double ToRadians(double degrees)
        {
            return degrees * DegreesToRadians;
        }

        /// <summary>
        /// Same as Java's <c>Math.toDegrees(double)</c> but 3x faster (multiply vs. divide).
        /// See CompareRadiansSnippet.java in tests.
        /// </summary>
        public static double ToDegrees(double radians)
        {
            return radians * RadiansToDegrees;
        }
    }
}
