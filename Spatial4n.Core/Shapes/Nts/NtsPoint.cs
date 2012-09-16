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
using GeoAPI.Geometries;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes.Impl;

namespace Spatial4n.Core.Shapes.Nts
{
	/// <summary>
	/// Wraps a {@link com.vividsolutions.jts.geom.Point}.
	/// </summary>
	public class NtsPoint : Point
	{
		private readonly IPoint pointGeom;
	    private readonly SpatialContext ctx;

	    /// <summary>
	    /// A simple constructor without normalization / validation.
	    /// </summary>
	    /// <param name="pointGeom"></param>
	    /// <param name="ctx"> </param>
	    public NtsPoint(IPoint pointGeom, SpatialContext ctx)
	    {
	        this.pointGeom = pointGeom;
	        this.ctx = ctx;
	    }

	    public IPoint GetGeom()
		{
			return pointGeom;
		}

		public Point GetCenter()
		{
			return this;
		}

		public bool HasArea()
		{
			return false;
		}

		public double GetArea(SpatialContext ctx)
		{
			return 0;
		}

		public Rectangle GetBoundingBox()
		{
            return ctx.MakeRectangle(this, this);
		}

		public SpatialRelation Relate(Shape other)
		{
			// ** NOTE ** the overall order of logic is kept consistent here with simple.PointImpl.
			if (other is Point)
				return this.Equals(other) ? SpatialRelation.INTERSECTS : SpatialRelation.DISJOINT;
			return other.Relate(this).Transpose();
		}

		public double GetX()
		{
			return pointGeom.X;
		}

		public double GetY()
		{
			return pointGeom.Y;
		}

	    public void Reset(double x, double y)
	    {
            var cSeq = pointGeom.CoordinateSequence;
            cSeq.SetOrdinate(0, Ordinate.X, x);
            cSeq.SetOrdinate(0, Ordinate.Y, y);
	    }


	    public override String ToString()
		{
			return string.Format("Pt(x={0:0.0},y={1:0.0})", GetX(), GetY());
		}

		public override bool Equals(Object o)
		{
			return PointImpl.Equals(this, o);
		}

		public override int GetHashCode()
		{
			return PointImpl.GetHashCode(this);
		}
	}

}
