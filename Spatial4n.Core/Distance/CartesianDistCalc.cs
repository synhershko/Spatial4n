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
	public class CartesianDistCalc : AbstractDistanceCalculator
	{
		private readonly bool squared;

		public CartesianDistCalc()
		{
			squared = false;
		}

		public CartesianDistCalc(bool squared)
		{
			this.squared = squared;
		}

		public override double Distance(IPoint @from, double toX, double toY)
		{
			double result = 0;

			double v = from.GetX() - toX;
			result += (v * v);

			v = from.GetY() - toY;
			result += (v * v);

			if (squared)
				return result;

			return Math.Sqrt(result);
		}

		public override IPoint PointOnBearing(IPoint @from, double dist, double bearingDEG, SpatialContext ctx)
		{
			if (dist == 0)
				return from;
			double bearingRAD = MathHelper.ToDegrees(bearingDEG);
			double x = Math.Sin(bearingRAD) * dist;
			double y = Math.Cos(bearingRAD) * dist;
			return ctx.MakePoint(from.GetX() + x, from.GetY() + y);
		}

		public override double DistanceToDegrees(double distance)
		{
			throw new InvalidOperationException("no geo!");
		}

		public override double DegreesToDistance(double degrees)
		{
			throw new InvalidOperationException("no geo!");
		}

		public override IRectangle CalcBoxByDistFromPt(IPoint @from, double distance, SpatialContext ctx)
		{
			return ctx.MakeRect(from.GetX() - distance, from.GetX() + distance, from.GetY() - distance, from.GetY() + distance);
		}

		public override double CalcBoxByDistFromPtHorizAxis(IPoint @from, double distance, SpatialContext ctx)
		{
			return from.GetY();
		}

		public override bool Equals(object o)
		{
			if (this == o) return true;

			var that = o as CartesianDistCalc;
			if (that == null) return false;
			return squared == that.squared;
		}

		public override int GetHashCode()
		{
			return (squared ? 1 : 0);
		}
	}
}
