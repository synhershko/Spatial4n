using System;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Util;
using Xunit;

namespace Spatial4n.Tests.util
{
	public class TestGeohashUtils
	{
		SpatialContext ctx = SpatialContext.GEO_KM;

		/**
		 * Pass condition: lat=42.6, lng=-5.6 should be encoded as "ezs42e44yx96",
		 * lat=57.64911 lng=10.40744 should be encoded as "u4pruydqqvj8"
		 */
		[Fact]
		public void TestEncode()
		{
			String hash = GeohashUtils.EncodeLatLon(42.6, -5.6);
			Assert.Equal("ezs42e44yx96", hash);

			hash = GeohashUtils.EncodeLatLon(57.64911, 10.40744);
			Assert.Equal("u4pruydqqvj8", hash);
		}

		/**
		 * Pass condition: lat=52.3738007, lng=4.8909347 should be encoded and then
		 * decoded within 0.00001 of the original value
		 */
		[Fact]
		public void TestDecodePreciseLongitudeLatitude()
		{
			String hash = GeohashUtils.EncodeLatLon(52.3738007, 4.8909347);

			Point point = GeohashUtils.Decode(hash, ctx);

			CustomAssert.EqualWithDelta(52.3738007, point.GetY(), 0.00001D);
			CustomAssert.EqualWithDelta(4.8909347, point.GetX(), 0.00001D);
		}

		/**
		 * Pass condition: lat=84.6, lng=10.5 should be encoded and then decoded
		 * within 0.00001 of the original value
		 */
		[Fact]
		public void TestDecodeImpreciseLongitudeLatitude()
		{
			String hash = GeohashUtils.EncodeLatLon(84.6, 10.5);

			Point point = GeohashUtils.Decode(hash, ctx);

			CustomAssert.EqualWithDelta(84.6, point.GetY(), 0.00001D);
			CustomAssert.EqualWithDelta(10.5, point.GetX(), 0.00001D);
		}

		/*
		 * see https://issues.apache.org/jira/browse/LUCENE-1815 for details
		 */
		[Fact]
		public void TestDecodeEncode()
		{
			String geoHash = "u173zq37x014";
			Assert.Equal(geoHash, GeohashUtils.EncodeLatLon(52.3738007, 4.8909347));
			Point point = GeohashUtils.Decode(geoHash, ctx);
			CustomAssert.EqualWithDelta(52.37380061d, point.GetY(), 0.000001d);
			CustomAssert.EqualWithDelta(4.8909343d, point.GetX(), 0.000001d);

			Assert.Equal(geoHash, GeohashUtils.EncodeLatLon(point.GetY(), point.GetX()));

			geoHash = "u173";
			point = GeohashUtils.Decode("u173", ctx);
			geoHash = GeohashUtils.EncodeLatLon(point.GetY(), point.GetX());
			Point point2 = GeohashUtils.Decode(geoHash, ctx);
			CustomAssert.EqualWithDelta(point.GetY(), point2.GetY(), 0.000001d);
			CustomAssert.EqualWithDelta(point.GetX(), point2.GetX(), 0.000001d);
		}

		/** see the table at http://en.wikipedia.org/wiki/Geohash */
		[Fact]
		public void TestHashLenToWidth()
		{
			double[] box = GeohashUtils.LookupDegreesSizeForHashLen(3);
			Assert.Equal(1.40625, box[0], precision: 5);
			Assert.Equal(1.40625, box[1], precision: 5);
		}
	}
}
