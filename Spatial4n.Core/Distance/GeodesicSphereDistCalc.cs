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
using System.Diagnostics;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Util;

namespace Spatial4n.Core.Distance
{
	/// <summary>
	/// A base class for a Distance Calculator that assumes a spherical earth model. 
	/// </summary>
	public abstract class GeodesicSphereDistCalc : AbstractDistanceCalculator
	{
		protected readonly double radius;

		protected GeodesicSphereDistCalc(double radius)
		{
			this.radius = radius;
		}

		public override double Distance(IPoint @from, double toX, double toY)
		{
			return DistanceLatLonRAD(DistanceUtils.ToRadians(from.GetY()), DistanceUtils.ToRadians(from.GetX()),
				DistanceUtils.ToRadians(toY), DistanceUtils.ToRadians(toX)) * radius;
		}

		public override IPoint PointOnBearing(IPoint @from, double dist, double bearingDEG, SpatialContext ctx)
		{
			//TODO avoid unnecessary double[] intermediate object
			if (dist == 0)
				return from;
			double[] latLon = DistanceUtils.PointOnBearingRAD(
				DistanceUtils.ToRadians(from.GetY()), DistanceUtils.ToRadians(from.GetX()),
				DistanceUtils.Dist2Radians(dist, ctx.GetUnits().EarthRadius()),
				DistanceUtils.ToRadians(bearingDEG), null);
			return ctx.MakePoint(MathHelper.ToDegrees(latLon[1]), MathHelper.ToDegrees(latLon[0]));

		}

		public override double DistanceToDegrees(double distance)
		{
			return DistanceUtils.Dist2Degrees(distance, radius);
		}

		public override double DegreesToDistance(double degrees)
		{
			return DistanceUtils.Radians2Dist(DistanceUtils.ToRadians(degrees), radius);

		}

		public override IRectangle CalcBoxByDistFromPt(IPoint @from, double distance, SpatialContext ctx)
		{
			Debug.Assert(radius == ctx.GetUnits().EarthRadius());
			if (distance == 0)
				return from.GetBoundingBox();
			return DistanceUtils.CalcBoxByDistFromPtDEG(from.GetY(), from.GetX(), distance, ctx);
		}

		public override double CalcBoxByDistFromPtHorizAxis(IPoint @from, double distance, SpatialContext ctx)
		{
			return DistanceUtils.CalcBoxByDistFromPtHorizAxisDEG(from.GetY(), from.GetX(), distance, radius);
		}

		public override bool Equals(object o)
		{
			if (this == o) return true;
			var that = o as GeodesicSphereDistCalc;
			return that != null && radius.Equals(that.radius);
		}

		public override int GetHashCode()
		{
			long temp = radius != +0.0d ? BitConverter.DoubleToInt64Bits(radius) : 0L;
			return (int)(temp ^ ((uint)temp >> 32));
		}

		protected abstract double DistanceLatLonRAD(double lat1, double lon1, double lat2, double lon2);

		public class Haversine : GeodesicSphereDistCalc
		{

			public Haversine(double radius)
				: base(radius)
			{
			}

			protected override double DistanceLatLonRAD(double lat1, double lon1, double lat2, double lon2)
			{
				return DistanceUtils.DistHaversineRAD(lat1, lon1, lat2, lon2);
			}

		}

		public class LawOfCosines : GeodesicSphereDistCalc
		{
			public LawOfCosines(double radius)
				: base(radius)
			{
			}

			protected override double DistanceLatLonRAD(double lat1, double lon1, double lat2, double lon2)
			{
				return DistanceUtils.DistLawOfCosinesRAD(lat1, lon1, lat2, lon2);
			}

		}

		public class Vincenty : GeodesicSphereDistCalc
		{
			public Vincenty(double radius)
				: base(radius)
			{
			}

			protected override double DistanceLatLonRAD(double lat1, double lon1, double lat2, double lon2)
			{
				return DistanceUtils.DistVincentyRAD(lat1, lon1, lat2, lon2);
			}
		}

	}
}
