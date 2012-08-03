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

namespace Spatial4n.Core.Shapes.Impl
{
	/// <summary>
	/// A circle as it exists on the surface of a sphere.
	/// </summary>
	public class GeoCircle : CircleImpl
	{
		private readonly double distDEG;// [0 TO 180]
		private readonly GeoCircle inverseCircle;//when distance reaches > 1/2 way around the world, cache the inverse.
		private readonly double horizAxisY;//see getYAxis

		public GeoCircle(Point p, double dist, SpatialContext ctx)
			: base(p, dist, ctx)
		{
			Debug.Assert(ctx.IsGeo());

			//In the direction of latitude (N,S), distance is the same number of degrees.
			distDEG = ctx.GetDistCalc().DistanceToDegrees(distRadius);

			if (distDEG > 90)
			{
				//--spans more than half the globe
				Debug.Assert(enclosingBox.GetWidth() == 360);
				double backDistDEG = 180 - distDEG;
				if (backDistDEG >= 0)
				{
					double backDistance = ctx.GetDistCalc().DegreesToDistance(backDistDEG);
					//shrink inverseCircle as small as possible to avoid accidental overlap
					backDistance -= Ulp(backDistance);
					Point backPoint = ctx.MakePoint(GetCenter().GetX() + 180, GetCenter().GetY() + 180);
					inverseCircle = new GeoCircle(backPoint, backDistance, ctx);
				}
				else
					inverseCircle = null;//whole globe
				horizAxisY = GetCenter().GetY();//although probably not used
			}
			else
			{
				inverseCircle = null;
				double _horizAxisY = ctx.GetDistCalc().CalcBoxByDistFromPt_yHorizAxisDEG(GetCenter(), dist, ctx);
				//some rare numeric conditioning cases can cause this to be barely beyond the box
				if (_horizAxisY > enclosingBox.GetMaxY())
				{
					horizAxisY = enclosingBox.GetMaxY();
				}
				else if (_horizAxisY < enclosingBox.GetMinY())
				{
					horizAxisY = enclosingBox.GetMinY();
				}
				else
				{
					horizAxisY = _horizAxisY;
				}
				//Debug.Assert(enclosingBox.Relate_yRange(horizAxis, horizAxis, ctx).Intersects());
			}
		}

		protected override double GetYAxis()
		{
			return horizAxisY;
		}

		/// <summary>
		/// Called after bounding box is intersected.
		/// </summary>
		/// <param name="r"></param>
		/// <param name="bboxSect">INTERSECTS or CONTAINS from enclosingBox's intersection</param>
		/// <param name="ctx"></param>
		/// <returns>DISJOINT, CONTAINS, or INTERSECTS (not WITHIN)</returns>
		protected override SpatialRelation RelateRectanglePhase2(Rectangle r, SpatialRelation bboxSect, SpatialContext ctx)
		{
			//Rectangle wraps around the world longitudinally creating a solid band; there are no corners to test intersection
			if (r.GetWidth() == 360)
			{
				return SpatialRelation.INTERSECTS;
			}

			if (inverseCircle != null)
			{
				return inverseCircle.Relate(r, ctx).Inverse();
			}

			//if a pole is wrapped, we have a separate algorithm
			if (enclosingBox.GetWidth() == 360)
			{
				return RelateRectangleCircleWrapsPole(r, ctx);
			}

			//This is an optimization path for when there are no dateline or pole issues.
			if (!enclosingBox.GetCrossesDateLine() && !r.GetCrossesDateLine())
			{
				return base.RelateRectanglePhase2(r, bboxSect, ctx);
			}

			//do quick check to see if all corners are within this circle for CONTAINS
			int cornersIntersect = NumCornersIntersect(r);
			if (cornersIntersect == 4)
			{
				//ensure r's x axis is within c's.  If it doesn't, r sneaks around the globe to touch the other side (intersect).
				SpatialRelation xIntersect = r.RelateXRange(enclosingBox.GetMinX(), enclosingBox.GetMaxX(), ctx);
				if (xIntersect == SpatialRelation.WITHIN)
					return SpatialRelation.CONTAINS;
				return SpatialRelation.INTERSECTS;
			}

			//INTERSECT or DISJOINT ?
			if (cornersIntersect > 0)
				return SpatialRelation.INTERSECTS;

			//Now we check if one of the axis of the circle intersect with r.  If so we have
			// intersection.

			/* x axis intersects  */
			if (r.RelateYRange(GetYAxis(), GetYAxis(), ctx).Intersects() // at y vertical
				  && r.RelateXRange(enclosingBox.GetMinX(), enclosingBox.GetMaxX(), ctx).Intersects())
				return SpatialRelation.INTERSECTS;

			/* y axis intersects */
			if (r.RelateXRange(GetXAxis(), GetXAxis(), ctx).Intersects())
			{ // at x horizontal
				double yTop = GetCenter().GetY() + distDEG;
				Debug.Assert(yTop <= 90);
				double yBot = GetCenter().GetY() - distDEG;
				Debug.Assert(yBot >= -90);
				if (r.RelateYRange(yBot, yTop, ctx).Intersects())//back bottom
					return SpatialRelation.INTERSECTS;
			}

			return SpatialRelation.DISJOINT;

		}

		private SpatialRelation RelateRectangleCircleWrapsPole(Rectangle r, SpatialContext ctx)
		{
			//This method handles the case where the circle wraps ONE pole, but not both.  For both,
			// there is the inverseCircle case handled before now.  The only exception is for the case where
			// the circle covers the entire globe, and we'll check that first.
			if (distDEG == 180)//whole globe
				return SpatialRelation.CONTAINS;

			//Check if r is within the pole wrap region:
			double yTop = GetCenter().GetY() + distDEG;
			if (yTop > 90)
			{
				double yTopOverlap = yTop - 90;
				Debug.Assert(yTopOverlap <= 90);
				if (r.GetMinY() >= 90 - yTopOverlap)
					return SpatialRelation.CONTAINS;
			}
			else
			{
				double yBot = point.GetY() - distDEG;
				if (yBot < -90)
				{
					double yBotOverlap = -90 - yBot;
					Debug.Assert(yBotOverlap <= 90);
					if (r.GetMaxY() <= -90 + yBotOverlap)
						return SpatialRelation.CONTAINS;
				}
				else
				{
					//This point is probably not reachable ??
					Debug.Assert(yTop == 90 || yBot == -90);//we simply touch a pole
					//continue
				}
			}

			//If there are no corners to check intersection because r wraps completely...
			if (r.GetWidth() == 360)
				return SpatialRelation.INTERSECTS;

			//Check corners:
			int cornersIntersect = NumCornersIntersect(r);
			// (It might be possible to reduce contains() calls within nCI() to exactly two, but this intersection
			//  code is complicated enough as it is.)
			if (cornersIntersect == 4)
			{//all
				double backX = ctx.NormX(GetCenter().GetX() + 180);
				if (r.RelateXRange(backX, backX, ctx).Intersects())
					return SpatialRelation.INTERSECTS;
				else
					return SpatialRelation.CONTAINS;
			}
			else if (cornersIntersect == 0)
			{//none
				double frontX = GetCenter().GetX();
				if (r.RelateXRange(frontX, frontX, ctx).Intersects())
					return SpatialRelation.INTERSECTS;
				else
					return SpatialRelation.DISJOINT;
			}
			else//partial
				return SpatialRelation.INTERSECTS;
		}

		/** Returns either 0 for none, 1 for some, or 4 for all. */
		private int NumCornersIntersect(Rectangle r)
		{
			//We play some logic games to avoid calling contains() which can be expensive.
			// for partial, we exit early with 1 and ignore bool.
			bool b = (Contains(r.GetMinX(), r.GetMinY()));
			if (Contains(r.GetMinX(), r.GetMaxY()))
			{
				if (!b)
					return 1;//partial
			}
			else
			{
				if (b)
					return 1;//partial
			}
			if (Contains(r.GetMaxX(), r.GetMinY()))
			{
				if (!b)
					return 1;//partial
			}
			else
			{
				if (b)
					return 1;//partial
			}
			if (Contains(r.GetMaxX(), r.GetMaxY()))
			{
				if (!b)
					return 1;//partial
			}
			else
			{
				if (b)
					return 1;//partial
			}
			return b ? 4 : 0;
		}

		public override string ToString()
		{
			//I'm deliberately making this look basic and not fully detailed with class name & misc fields.
			//Add distance in degrees, which is easier to recognize, and earth radius agnostic.
			String dStr = String.Format("{0:0.00}", distRadius);
			if (ctx.IsGeo())
			{
				double distDEG = ctx.GetDistCalc().DistanceToDegrees(distRadius);
				dStr += String.Format("={0:0.0}\u00B0", distDEG);
			}
			return "Circle(" + point + ",d=" + dStr + ')';
		}

		private static double Ulp(double value)
		{
			if (Double.IsNaN(value)) return Double.NaN;
			if (Double.IsInfinity(value)) return Double.PositiveInfinity;
			if (value == +0.0d || value == -0.0d) return Double.MinValue;
			if (value == Double.MaxValue) return Math.Pow(2, 971);


			long bits = BitConverter.DoubleToInt64Bits(value);
			double nextValue = BitConverter.Int64BitsToDouble(bits + 1);
			return nextValue - value;
		}
	}
}
