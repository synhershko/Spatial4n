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
using Spatial4n.Core.Exceptions;

namespace Spatial4n.Core.Context
{
	/// <summary>
	/// Utility methods related to parsing shapes.
	/// </summary>
	public static class ParseUtils
	{
		/// <summary>
		/// Given a string containing <i>dimension</i> values encoded in it, separated by commas, return a String array of length <i>dimension</i>
		/// containing the values.
		/// </summary>
		/// <param name="_out">A preallocated array.  Must be size dimension.  If it is not it will be resized.</param>
		/// <param name="externalVal">The value to parse</param>
		/// <param name="dimension">The expected number of values for the point</param>
		/// <returns>An array of the values that make up the point (aka vector)</returns>
		public static String[] ParsePoint(String[] _out, String externalVal, int dimension)
		{
			//TODO: Should we support sparse vectors?
			if (_out == null || _out.Length != dimension) _out = new String[dimension];
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
		/// Given a string containing <i>dimension</i> values encoded in it, separated by commas, return a double array of length <i>dimension</i>
		/// containing the values.
		/// </summary>
		/// <param name="out">A preallocated array.  Must be size dimension.  If it is not it will be resized.</param>
		/// <param name="externalVal">The value to parse</param>
		/// <param name="dimension">The expected number of values for the point</param>
		/// <returns>An array of the values that make up the point (aka vector)</returns>
		public static double[] ParsePointDouble(double[] @out, String externalVal, int dimension)
		{
			if (@out == null || @out.Length != dimension) @out = new double[dimension];
			int idx = externalVal.IndexOf(',');
			int end = idx;
			int start = 0;
			int i = 0;
			if (idx == -1 && dimension == 1 && externalVal.Length > 0)
			{
				//we have a single point, dimension better be 1
				@out[0] = Double.Parse(externalVal.Trim());
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
					@out[i] = Double.Parse(externalVal.Substring(start, (end - start)));
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

		public static double[] ParseLatitudeLongitude(String latLonStr)
		{
			return ParseLatitudeLongitude(null, latLonStr);
		}

		/// <summary>
		/// extract (by calling {@link #parsePoint(String[], String, int)} and validate the latitude and longitude contained
		/// in the String by making sure the latitude is between 90 & -90 and longitude is between -180 and 180.<p/>
		/// The latitude is assumed to be the first part of the string and the longitude the second part.
		/// </summary>
		/// <param name="latLon">A preallocated array to hold the result</param>
		/// <param name="latLonStr">The string to parse.  Latitude is the first value, longitude is the second.</param>
		/// <returns>The lat long</returns>
		public static double[] ParseLatitudeLongitude(double[] latLon, String latLonStr)
		{
			if (latLon == null)
			{
				latLon = new double[2];
			}
			double[] toks = ParsePointDouble(null, latLonStr, 2);

			if (toks[0] < -90.0 || toks[0] > 90.0)
			{
				throw new InvalidShapeException(
						"Invalid latitude: latitudes are range -90 to 90: provided lat: ["
								+ toks[0] + "]");
			}
			latLon[0] = toks[0];


			if (toks[1] < -180.0 || toks[1] > 180.0)
			{

				throw new InvalidShapeException(
						"Invalid longitude: longitudes are range -180 to 180: provided lon: ["
								+ toks[1] + "]");
			}
			latLon[1] = toks[1];

			return latLon;
		}

	}
}
