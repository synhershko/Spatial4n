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
using Spatial4n.Core.Distance;
using System;
using System.Diagnostics;

namespace Spatial4n.Core.Shapes.Impl
{
    /// <summary>
    /// A circle as it exists on the surface of a sphere.
    /// </summary>
    public class GeoCircle : Circle
    {
        private GeoCircle inverseCircle; //when distance reaches > 1/2 way around the world, cache the inverse.
        private double horizAxisY; //see getYAxis

        public GeoCircle(IPoint p, double radiusDEG, SpatialContext ctx)
            : base(p, radiusDEG, ctx)
        {
            Debug.Assert(ctx.IsGeo);
            Init();
        }

        public override void Reset(double x, double y, double radiusDEG)
        {
            base.Reset(x, y, radiusDEG);
            Init();
        }

        private void Init()
        {
            if (radiusDEG > 90)
            {
                //--spans more than half the globe
                Debug.Assert(enclosingBox.Width == 360);
                double backDistDEG = 180 - radiusDEG;
                if (backDistDEG > 0)
                {
                    double backRadius = 180 - radiusDEG;
                    double backX = DistanceUtils.NormLonDEG(Center.X + 180);
                    double backY = DistanceUtils.NormLatDEG(Center.Y + 180);
                    //Shrink inverseCircle as small as possible to avoid accidental overlap.
                    // Note that this is tricky business to come up with a value small enough
                    // but not too small or else numerical conditioning issues become a problem.
                    backRadius -= Math.Max(Ulp(Math.Abs(backY) + backRadius), Ulp(Math.Abs(backX) + backRadius));
                    if (inverseCircle != null)
                    {
                        inverseCircle.Reset(backX, backY, backRadius);
                    }
                    else
                    {
                        inverseCircle = new GeoCircle(ctx.MakePoint(backX, backY), backRadius, ctx);
                    }
                }
                else
                {
                    inverseCircle = null; //whole globe
                }
                horizAxisY = Center.Y; //although probably not used
            }
            else
            {
                inverseCircle = null;
                double _horizAxisY = ctx.DistCalc.CalcBoxByDistFromPt_yHorizAxisDEG(Center, radiusDEG, ctx);
                //some rare numeric conditioning cases can cause this to be barely beyond the box
                if (_horizAxisY > enclosingBox.MaxY)
                {
                    horizAxisY = enclosingBox.MaxY;
                }
                else if (_horizAxisY < enclosingBox.MinY)
                {
                    horizAxisY = enclosingBox.MinY;
                }
                else
                {
                    horizAxisY = _horizAxisY;
                }
                //Debug.Assert(enclosingBox.Relate_yRange(horizAxis, horizAxis, ctx).Intersects());
            }
        }

        protected override double YAxis
		{
			get { return horizAxisY; }
		}

	    /// <summary>
	    /// Called after bounding box is intersected.
	    /// </summary>
	    /// <param name="r"></param>
	    /// <param name="bboxSect">INTERSECTS or CONTAINS from enclosingBox's intersection</param>
	    /// <returns>DISJOINT, CONTAINS, or INTERSECTS (not WITHIN)</returns>
	    protected override SpatialRelation RelateRectanglePhase2(IRectangle r, SpatialRelation bboxSect)
		{
			if (inverseCircle != null)
			{
				return inverseCircle.Relate(r).Inverse();
			}

			//if a pole is wrapped, we have a separate algorithm
			if (enclosingBox.Width == 360)
			{
				return RelateRectangleCircleWrapsPole(r, ctx);
			}

			//This is an optimization path for when there are no dateline or pole issues.
			if (!enclosingBox.CrossesDateLine && !r.CrossesDateLine)
			{
				return base.RelateRectanglePhase2(r, bboxSect);
			}

            //Rectangle wraps around the world longitudinally creating a solid band; there are no corners to test intersection
            if (r.Width == 360)
            {
                return SpatialRelation.INTERSECTS;
            }

            //do quick check to see if all corners are within this circle for CONTAINS
            int cornersIntersect = NumCornersIntersect(r);
			if (cornersIntersect == 4)
			{
				//ensure r's x axis is within c's.  If it doesn't, r sneaks around the globe to touch the other side (intersect).
				SpatialRelation xIntersect = r.RelateXRange(enclosingBox.MinX, enclosingBox.MaxX);
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
			if (r.RelateYRange(YAxis, YAxis).Intersects() // at y vertical
				  && r.RelateXRange(enclosingBox.MinX, enclosingBox.MaxX).Intersects())
				return SpatialRelation.INTERSECTS;

			/* y axis intersects */
			if (r.RelateXRange(XAxis, XAxis).Intersects())
			{ // at x horizontal
				double yTop = Center.Y + radiusDEG;
				Debug.Assert(yTop <= 90);
				double yBot = Center.Y - radiusDEG;
				Debug.Assert(yBot >= -90);
				if (r.RelateYRange(yBot, yTop).Intersects())//back bottom
					return SpatialRelation.INTERSECTS;
			}

			return SpatialRelation.DISJOINT;

		}

		private SpatialRelation RelateRectangleCircleWrapsPole(IRectangle r, SpatialContext ctx)
		{
			//This method handles the case where the circle wraps ONE pole, but not both.  For both,
			// there is the inverseCircle case handled before now.  The only exception is for the case where
			// the circle covers the entire globe, and we'll check that first.
			if (radiusDEG == 180)//whole globe
				return SpatialRelation.CONTAINS;

			//Check if r is within the pole wrap region:
			double yTop = Center.Y + radiusDEG;
			if (yTop > 90)
			{
				double yTopOverlap = yTop - 90;
				Debug.Assert(yTopOverlap <= 90, "yTopOverlap: " + yTopOverlap);
				if (r.MinY >= 90 - yTopOverlap)
					return SpatialRelation.CONTAINS;
			}
			else
			{
				double yBot = point.Y - radiusDEG;
				if (yBot < -90)
				{
					double yBotOverlap = -90 - yBot;
					Debug.Assert(yBotOverlap <= 90);
					if (r.MaxY <= -90 + yBotOverlap)
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
			if (r.Width == 360)
				return SpatialRelation.INTERSECTS;

			//Check corners:
			int cornersIntersect = NumCornersIntersect(r);
			// (It might be possible to reduce contains() calls within nCI() to exactly two, but this intersection
			//  code is complicated enough as it is.)
            double frontX = Center.X;
			if (cornersIntersect == 4)
			{//all
                double backX = frontX <= 0 ? frontX + 180 : frontX - 180;
                if (r.RelateXRange(backX, backX).Intersects())
					return SpatialRelation.INTERSECTS;
				else
					return SpatialRelation.CONTAINS;
			}
			else if (cornersIntersect == 0)
			{//none
				if (r.RelateXRange(frontX, frontX).Intersects())
					return SpatialRelation.INTERSECTS;
				else
					return SpatialRelation.DISJOINT;
			}
			else//partial
				return SpatialRelation.INTERSECTS;
		}

		/** Returns either 0 for none, 1 for some, or 4 for all. */
		private int NumCornersIntersect(IRectangle r)
		{
			//We play some logic games to avoid calling contains() which can be expensive.
			// for partial, we exit early with 1 and ignore bool.
			bool b = (Contains(r.MinX, r.MinY));
			if (Contains(r.MinX, r.MaxY))
			{
				if (!b)
					return 1;//partial
			}
			else
			{
				if (b)
					return 1;//partial
			}
			if (Contains(r.MaxX, r.MinY))
			{
				if (!b)
					return 1;//partial
			}
			else
			{
				if (b)
					return 1;//partial
			}
			if (Contains(r.MaxX, r.MaxY))
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
            double distKm = DistanceUtils.Degrees2Dist(radiusDEG, DistanceUtils.EARTH_MEAN_RADIUS_KM);
			string dStr = string.Format("{0:0.0}\u00B0 {1:0.00}km", radiusDEG, distKm);
			return "Circle(" + point + ", d=" + dStr + ')';
		}

		private static double Ulp(double value)
		{
			if (double.IsNaN(value)) return double.NaN;
			if (double.IsInfinity(value)) return double.PositiveInfinity;
			if (value == +0.0d || value == -0.0d) return double.MinValue;
			if (value == double.MaxValue) return Math.Pow(2, 971);


			long bits = BitConverter.DoubleToInt64Bits(value);
			double nextValue = BitConverter.Int64BitsToDouble(bits + 1);
			return nextValue - value;
		}
	}
}
