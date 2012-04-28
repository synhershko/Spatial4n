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
using System.Collections.ObjectModel;
using System.Linq;
using Spatial4n.Core.Context;

namespace Spatial4n.Core.Shapes
{
    public class MultiShape : Shape
    {
        private readonly Collection<Shape> geoms;
        private readonly Rectangle bbox;

        public MultiShape(Collection<Shape> geoms, SpatialContext ctx)
        {
            this.geoms = geoms;
            double minX = Double.MaxValue;
            double minY = Double.MaxValue;
            double maxX = Double.MaxValue;
            double maxY = Double.MaxValue;
            foreach (var geom in geoms)
            {
                Rectangle r = geom.GetBoundingBox();
                minX = Math.Min(minX, r.GetMinX());
                minY = Math.Min(minY, r.GetMinY());
                maxX = Math.Max(maxX, r.GetMaxX());
                maxY = Math.Max(maxY, r.GetMaxY());
            }
            this.bbox = ctx.MakeRect(minX, maxX, minY, maxY);
        }

        public SpatialRelation Relate(Shape other, SpatialContext ctx)
        {
            bool allOutside = true;
            bool allContains = true;
            foreach (var geom in geoms)
            {
                SpatialRelation sect = geom.Relate(other, ctx);
                if (sect != SpatialRelation.DISJOINT)
                    allOutside = false;
                if (sect != SpatialRelation.CONTAINS)
                    allContains = false;
                if (!allContains && !allOutside)
                    return SpatialRelation.INTERSECTS; //short circuit
            }
            if (allOutside)
                return SpatialRelation.DISJOINT;
            if (allContains)
                return SpatialRelation.CONTAINS;
            return SpatialRelation.INTERSECTS;
        }

        public Rectangle GetBoundingBox()
        {
            return bbox;
        }

        public bool HasArea()
        {
            return geoms.Any(geom => geom.HasArea());
        }

        public Point GetCenter()
        {
            return bbox.GetCenter();
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;
            
            var that = o as MultiShape;
            if (that == null || (geoms != null ? !geoms.Equals(that.geoms) : that.geoms != null)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return geoms != null ? geoms.GetHashCode() : 0;
        }
    }
}
