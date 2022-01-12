/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
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
using System.Diagnostics;
using System.Text;

namespace Spatial4n.Core.Util
{
    /// <summary>
    ///  Utilities for encoding and decoding <a href="http://en.wikipedia.org/wiki/Geohash">geohashes</a>.
    ///  <para/>
    ///  This class isn't used by any other part of Spatial4n; it's included largely for convenience of
    ///  software using Spatial4n. There are other open-source libraries that have more comprehensive
    ///  geohash utilities but providing this one avoids an additional dependency for what's a small
    ///  amount of code.  <c>If you're using Spatial4n just for this class, consider alternatives.</c>
    ///  <para/>
    ///  This code originally came from <a href="https://issues.apache.org/jira/browse/LUCENE-1512">
    ///  Apache Lucene, LUCENE-1512</a>.
    /// </summary>
    public static class GeohashUtils
    {
        private static readonly char[] Base32 = {
                                                     '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                                                     'b', 'c', 'd', 'e', 'f', 'g', 'h', 'j', 'k', 'm',
                                                     'n', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x',
                                                     'y', 'z'
                                                 }; //note: this is sorted

        private static readonly int[] Base32Index = LoadBase32Index(); //sparse array of indexes from '0' to 'z'

        private static int[] LoadBase32Index() // Spatial4n: Avoid static constructors
        {
            int[] result = new int[Base32[Base32.Length - 1] - Base32[0] + 1];
            Debug.Assert(result.Length < 100);//reasonable length
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = -500;
            }

            for (int i = 0; i < Base32.Length; i++)
            {
                result[Base32[i] - Base32[0]] = i;
            }

            return result;
        }

        public const int MaxPrecision = 24; //DWS: I forget what level results in needless more precision but it's about this

        [Obsolete("Use MaxPrecision instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public const int MAX_PRECISION = 24; //DWS: I forget what level results in needless more precision but it's about this

        private static readonly int[] Bits = { 16, 8, 4, 2, 1 };

        /// <summary>
        /// Encodes the given latitude and longitude into a geohash
        /// </summary>
        /// <param name="latitude">Latitude to encode</param>
        /// <param name="longitude">Longitude to encode</param>
        /// <returns>Geohash encoding of the longitude and latitude</returns>
        public static string EncodeLatLon(double latitude, double longitude)
        {
            return EncodeLatLon(latitude, longitude, 12);
        }

        /// <summary>
        /// Encodes the given latitude and longitude into a geohash
        /// </summary>
        /// <param name="latitude">Latitude to encode</param>
        /// <param name="longitude">Longitude to encode</param>
        /// <param name="precision"></param>
        /// <returns>Geohash encoding of the longitude and latitude</returns>
        public static string EncodeLatLon(double latitude, double longitude, int precision)
        {
            double[] latInterval = { -90.0, 90.0 };
            double[] lonInterval = { -180.0, 180.0 };

            var geohash = new StringBuilder();
            bool isEven = true;
            int bit = 0, ch = 0;

            while (geohash.Length < precision)
            {
                double mid; // spatial4n: Remove unnecessary assignment
                if (isEven)
                {
                    mid = (lonInterval[0] + lonInterval[1]) / 2;
                    if (longitude > mid)
                    {
                        ch |= Bits[bit];
                        lonInterval[0] = mid;
                    }
                    else
                    {
                        lonInterval[1] = mid;
                    }
                }
                else
                {
                    mid = (latInterval[0] + latInterval[1]) / 2;
                    if (latitude > mid)
                    {
                        ch |= Bits[bit];
                        latInterval[0] = mid;
                    }
                    else
                    {
                        latInterval[1] = mid;
                    }
                }

                isEven = !isEven;

                if (bit < 4)
                {
                    bit++;
                }
                else
                {
                    geohash.Append(Base32[ch]);
                    bit = 0;
                    ch = 0;
                }
            }

            return geohash.ToString();
        }

        /// <summary>
        /// Decodes the given geohash into a latitude and longitude
        /// </summary>
        /// <param name="geohash">Geohash to deocde</param>
        /// <param name="ctx"></param>
        /// <returns>Array with the latitude at index 0, and longitude at index 1</returns>
        public static IPoint Decode(string geohash, SpatialContext ctx)
        {
            IRectangle rect = DecodeBoundary(geohash, ctx);
            double latitude = (rect.MinY + rect.MaxY) / 2D;
            double longitude = (rect.MinX + rect.MaxX) / 2D;
            return ctx.MakePoint(longitude, latitude);
        }

        /// <summary>
        /// Returns min-max lat, min-max lon.
        /// </summary>
        public static IRectangle DecodeBoundary(string geohash, SpatialContext ctx)
        {
            double minY = -90, maxY = 90, minX = -180, maxX = 180;
            bool isEven = true;

            for (int i = 0; i < geohash.Length; i++)
            {
                char c = geohash[i];
                if (c >= 'A' && c <= 'Z')
                    c = Convert.ToChar(c - Convert.ToChar('A' - 'a'));

                int cd = Base32Index[c - Base32[0]]; //TODO check successful?

                foreach (var mask in Bits)
                {
                    if (isEven)
                    {
                        if ((cd & mask) != 0)
                        {
                            minX = (minX + maxX) / 2D;
                        }
                        else
                        {
                            maxX = (minX + maxX) / 2D;
                        }
                    }
                    else
                    {
                        if ((cd & mask) != 0)
                        {
                            minY = (minY + maxY) / 2D;
                        }
                        else
                        {
                            maxY = (minY + maxY) / 2D;
                        }
                    }
                    isEven = !isEven;
                }
            }
            return ctx.MakeRectangle(minX, maxX, minY, maxY);
        }

        /// <summary>Array of geohashes 1 level below the baseGeohash. Sorted.</summary>
        public static string[] GetSubGeohashes(string baseGeohash)
        {
            var hashes = new string[Base32.Length];
            for (int i = 0; i < Base32.Length; i++)
            {//note: already sorted
                char c = Base32[i];
                hashes[i] = baseGeohash + c;
            }
            return hashes;
        }

        public static double[] LookupDegreesSizeForHashLen(int hashLen)
        {
            return new double[] { hashLenToLatHeight[hashLen], hashLenToLonWidth[hashLen] };
        }

        /// <summary>
        /// Return the shortest geohash length that will have a width &amp; height >= specified arguments.
        /// </summary>
        public static int LookupHashLenForWidthHeight(double lonErr, double latErr)
        {
            //loop through hash length arrays from beginning till we find one.
            for (int len = 1; len < MaxPrecision; len++)
            {
                double latHeight = hashLenToLatHeight[len];
                double lonWidth = hashLenToLonWidth[len];
                if (latHeight < latErr && lonWidth < lonErr)
                    return len;
            }
            return MaxPrecision;
        }

        /** See the table at http://en.wikipedia.org/wiki/Geohash */
        private static readonly double[] hashLenToLatHeight = LoadHashLenToLatHeight();
        private static readonly double[] hashLenToLonWidth = LoadHashLenToLonWidth();

        private static double[] LoadHashLenToLatHeight() // Spatial4n: Avoid static constructors
        {
            double[] result = new double[MaxPrecision + 1];
            result[0] = 90 * 2;
            bool even = false;
            for (int i = 1; i <= MaxPrecision; i++)
            {
                result[i] = result[i - 1] / (even ? 8 : 4);
                even = !even;
            }
            return result;
        }

        private static double[] LoadHashLenToLonWidth() // Spatial4n: Avoid static constructors
        {
            double[] result = new double[MaxPrecision + 1];
            result[0] = 180 * 2;
            bool even = false;
            for (int i = 1; i <= MaxPrecision; i++)
            {
                result[i] = result[i - 1] / (even ? 4 : 8);
                even = !even;
            }
            return result;
        }
    }
}