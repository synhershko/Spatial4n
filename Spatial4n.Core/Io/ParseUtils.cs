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

using Spatial4n.Core.Exceptions;
using System;
using System.Globalization;

namespace Spatial4n.Core.IO
{
    /// <summary>
    /// Utility methods related to parsing a series of numbers.
    /// <para>
    /// This code came from <see cref="Distance.DistanceUtils"/>, which came from
    /// <a href="https://issues.apache.org/jira/browse/LUCENE-773">Apache
    /// Lucene, LUCENE-773</a>, which in turn came from "LocalLucene".
    /// </para>
    /// </summary>
    [Obsolete("Not useful; see https://github.com/spatial4j/spatial4j/issues/19"), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ParseUtils
    {
        /// <summary>
        /// Given a string containing <c>dimension</c> values encoded in it, separated by commas, return a string array of length <c>dimension</c>
        /// containing the values.
        /// </summary>
        /// <param name="_out">A preallocated array.  Must be size dimension.  If it is not it will be resized.</param>
        /// <param name="externalVal">The value to parse</param>
        /// <param name="dimension">The expected number of values for the point</param>
        /// <returns>An array of the values that make up the point (aka vector)</returns>
        /// <exception cref="InvalidShapeException">If the dimension specified does not match the number of values in the <paramref name="externalVal"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="externalVal"/> is <c>null</c>.</exception>
        public static string[] ParsePoint(string[] _out, string externalVal, int dimension)
        {
            if (externalVal is null)
                throw new ArgumentNullException(nameof(externalVal)); // spatial4n specific - use ArgumentNullException instead of NullReferenceException

            //TODO: Should we support sparse vectors?
            if (_out == null || _out.Length != dimension) _out = new string[dimension];
            int idx = externalVal.IndexOf(',');
            int end = idx;
            int start = 0;
            int i = 0;
            if (idx == -1 && dimension == 1 && externalVal.Length > 0)
            {
                //we have a single point, dimension better be 1
                _out[0] = externalVal.Trim();
                i = 1;
            }
            else if (idx > 0)
            {
                //if it is zero, that is an error
                //Parse out a comma separated list of point values, as in: 73.5,89.2,7773.4
                for (; i < dimension; i++)
                {
                    while (start < end && externalVal[start] == ' ') start++;
                    while (end > start && externalVal[end - 1] == ' ') end--;
                    if (start == end)
                    {
                        break;
                    }
                    //Substring in .NET is (startPosn, length), But in Java it's (startPosn, endPosn)
                    //see http://docs.oracle.com/javase/1.4.2/docs/api/java/lang/String.html#substring(int, int)
                    _out[i] = externalVal.Substring(start, (end - start));
                    start = idx + 1;
                    end = externalVal.IndexOf(',', start);
                    idx = end;
                    if (end == -1)
                    {
                        end = externalVal.Length;
                    }
                }
            }
            if (i != dimension)
            {
                throw new InvalidShapeException("incompatible dimension (" + dimension +
                                                ") and values (" + externalVal + ").  Only " + i + " values specified");
            }
            return _out;
        }

        /// <summary>
        /// Given a string containing <c>dimension</c> values encoded in it, separated by commas, return a double array of length <c>dimension</c>
        /// containing the values.
        /// </summary>
        /// <param name="out">A preallocated array.  Must be size dimension.  If it is not it will be resized.</param>
        /// <param name="externalVal">The value to parse</param>
        /// <param name="dimension">The expected number of values for the point</param>
        /// <returns>An array of the values that make up the point (aka vector)</returns>
        /// <exception cref="InvalidShapeException">If the dimension specified does not match the number of values in the <paramref name="externalVal"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="externalVal"/> is <c>null</c>.</exception>
        public static double[] ParsePointDouble(double[]? @out, string externalVal, int dimension)
        {
            if (externalVal is null)
                throw new ArgumentNullException(nameof(externalVal)); // spatial4n specific - use ArgumentNullException instead of NullReferenceException

            if (@out == null || @out.Length != dimension) @out = new double[dimension];
            int idx = externalVal.IndexOf(',');
            int end = idx;
            int start = 0;
            int i = 0;
            if (idx == -1 && dimension == 1 && externalVal.Length > 0)
            {
                //we have a single point, dimension better be 1
                @out[0] = double.Parse(externalVal.Trim(), CultureInfo.InvariantCulture);
                i = 1;
            }
            else if (idx > 0)
            {
                //if it is zero, that is an error
                //Parse out a comma separated list of point values, as in: 73.5,89.2,7773.4
                for (; i < dimension; i++)
                {
                    //TODO: abstract common code with other parsePoint
                    while (start < end && externalVal[start] == ' ') start++;
                    while (end > start && externalVal[end - 1] == ' ') end--;
                    if (start == end)
                    {
                        break;
                    }
                    //Substring in .NET is (startPosn, length), But in Java it's (startPosn, endPosn)
                    //see http://docs.oracle.com/javase/1.4.2/docs/api/java/lang/String.html#substring(int, int)
                    @out[i] = double.Parse(externalVal.Substring(start, (end - start)), CultureInfo.InvariantCulture);
                    start = idx + 1;
                    end = externalVal.IndexOf(',', start);
                    idx = end;
                    if (end == -1)
                    {
                        end = externalVal.Length;
                    }
                }
            }
            if (i != dimension)
            {
                throw new InvalidShapeException("incompatible dimension (" + dimension +
                                                ") and values (" + externalVal + ").  Only " + i + " values specified");
            }
            return @out;
        }

        /// <summary>
        /// Extract (by calling <see cref="ParsePoint(string[], string, int)"/> and validate the latitude and longitude contained
        /// in the string by making sure the latitude is between 90 &amp; -90 and longitude is between -180 and 180.<p/>
        /// The latitude is assumed to be the first part of the string and the longitude the second part.
        /// </summary>
        /// <param name="latLonStr">The string to parse.  Latitude is the first value, longitude is the second.</param>
        /// <returns>The lat long</returns>
        /// <exception cref="InvalidShapeException">if there was an error parsing</exception>
        /// <exception cref="ArgumentNullException"><paramref name="latLonStr"/> is <c>null</c>.</exception>
        public static double[] ParseLatitudeLongitude(string latLonStr)
        {
            return ParseLatitudeLongitude(null, latLonStr);
        }

        /// <summary>
        /// A variation of <see cref="ParseLatitudeLongitude(string)"/> that re-uses an output array.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="latLonStr"/> is <c>null</c>.</exception>
        public static double[] ParseLatitudeLongitude(double[]? outLatLon, string latLonStr)
        {
            outLatLon = ParsePointDouble(outLatLon, latLonStr, 2);

            if (outLatLon[0] < -90.0 || outLatLon[0] > 90.0)
            {
                throw new InvalidShapeException(
                        "Invalid latitude: latitudes are range -90 to 90: provided lat: ["
                                + outLatLon[0] + "]");
            }


            if (outLatLon[1] < -180.0 || outLatLon[1] > 180.0)
            {
                throw new InvalidShapeException(
                        "Invalid longitude: longitudes are range -180 to 180: provided lon: ["
                                + outLatLon[1] + "]");
            }

            return outLatLon;
        }
    }
}
