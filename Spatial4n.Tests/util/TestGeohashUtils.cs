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
using Spatial4n.Core.Util;
using Xunit;

namespace Spatial4n.Tests.util
{
    public class TestGeohashUtils
    {
        readonly SpatialContext ctx = SpatialContext.GEO;

        /**
         * Pass condition: lat=42.6, lng=-5.6 should be encoded as "ezs42e44yx96",
         * lat=57.64911 lng=10.40744 should be encoded as "u4pruydqqvj8"
         */
        [Fact]
        public virtual void TestEncode()
        {
            string hash = GeohashUtils.EncodeLatLon(42.6, -5.6);
            Assert.Equal("ezs42e44yx96", hash);

            hash = GeohashUtils.EncodeLatLon(57.64911, 10.40744);
            Assert.Equal("u4pruydqqvj8", hash);
        }

        /**
         * Pass condition: lat=52.3738007, lng=4.8909347 should be encoded and then
         * decoded within 0.00001 of the original value
         */
        [Fact]
        public virtual void TestDecodePreciseLongitudeLatitude()
        {
            string hash = GeohashUtils.EncodeLatLon(52.3738007, 4.8909347);

            IPoint point = GeohashUtils.Decode(hash, ctx);

            CustomAssert.EqualWithDelta(52.3738007, point.Y, 0.00001D);
            CustomAssert.EqualWithDelta(4.8909347, point.X, 0.00001D);
        }

        /**
         * Pass condition: lat=84.6, lng=10.5 should be encoded and then decoded
         * within 0.00001 of the original value
         */
        [Fact]
        public virtual void TestDecodeImpreciseLongitudeLatitude()
        {
            string hash = GeohashUtils.EncodeLatLon(84.6, 10.5);

            IPoint point = GeohashUtils.Decode(hash, ctx);

            CustomAssert.EqualWithDelta(84.6, point.Y, 0.00001D);
            CustomAssert.EqualWithDelta(10.5, point.X, 0.00001D);
        }

        /*
         * see https://issues.apache.org/jira/browse/LUCENE-1815 for details
         */
        [Fact]
        public virtual void TestDecodeEncode()
        {
            string geoHash = "u173zq37x014";
            Assert.Equal(geoHash, GeohashUtils.EncodeLatLon(52.3738007, 4.8909347));
            IPoint point = GeohashUtils.Decode(geoHash, ctx);
            CustomAssert.EqualWithDelta(52.37380061d, point.Y, 0.000001d);
            CustomAssert.EqualWithDelta(4.8909343d, point.X, 0.000001d);

            Assert.Equal(geoHash, GeohashUtils.EncodeLatLon(point.Y, point.X));

            geoHash = "u173";
            point = GeohashUtils.Decode("u173", ctx);
            geoHash = GeohashUtils.EncodeLatLon(point.Y, point.X);
            IPoint point2 = GeohashUtils.Decode(geoHash, ctx);
            CustomAssert.EqualWithDelta(point.Y, point2.Y, 0.000001d);
            CustomAssert.EqualWithDelta(point.X, point2.X, 0.000001d);
        }

        /** see the table at http://en.wikipedia.org/wiki/Geohash */
        [Fact]
        public virtual void TestHashLenToWidth()
        {
            //test odd & even len
            double[] boxOdd = GeohashUtils.LookupDegreesSizeForHashLen(3);
            CustomAssert.EqualWithDelta(1.40625, boxOdd[0], 0.0001);
            CustomAssert.EqualWithDelta(1.40625, boxOdd[1], 0.0001);
            double[] boxEven = GeohashUtils.LookupDegreesSizeForHashLen(4);
            CustomAssert.EqualWithDelta(0.1757, boxEven[0], 0.0001);
            CustomAssert.EqualWithDelta(0.3515, boxEven[1], 0.0001);
        }

        /** see the table at http://en.wikipedia.org/wiki/Geohash */
        [Fact]
        public virtual void TestLookupHashLenForWidthHeight()
        {
            Assert.Equal(1, GeohashUtils.LookupHashLenForWidthHeight(999, 999));

            Assert.Equal(1, GeohashUtils.LookupHashLenForWidthHeight(999, 46));
            Assert.Equal(1, GeohashUtils.LookupHashLenForWidthHeight(46, 999));

            Assert.Equal(2, GeohashUtils.LookupHashLenForWidthHeight(44, 999));
            Assert.Equal(2, GeohashUtils.LookupHashLenForWidthHeight(999, 44));
            Assert.Equal(2, GeohashUtils.LookupHashLenForWidthHeight(999, 5.7));
            Assert.Equal(2, GeohashUtils.LookupHashLenForWidthHeight(11.3, 999));

            Assert.Equal(3, GeohashUtils.LookupHashLenForWidthHeight(999, 5.5));
            Assert.Equal(3, GeohashUtils.LookupHashLenForWidthHeight(11.1, 999));

            Assert.Equal(GeohashUtils.MAX_PRECISION, GeohashUtils.LookupHashLenForWidthHeight(10e-20, 10e-20));
        }
    }
}
