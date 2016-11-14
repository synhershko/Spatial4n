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

namespace Spatial4n.Core.Shapes
{
    /// <summary>
    /// The set of spatial relationships.  Naming is consistent with OGC spec conventions as seen in SQL/MM and others.
    /// <para>
    /// No equality case.  If two Shape instances are equal then the result might be CONTAINS (preferred) or WITHIN.  
    /// Client logic may have to be aware of this edge condition; Spatial4n testing certainly does.
    /// </para>
    /// <para></para>
    /// The "CONTAINS" and "WITHIN" wording here is inconsistent with OGC; these here map to OGC
    /// "COVERS" and "COVERED BY", respectively. The distinction is in the boundaries; in Spatial4n
    /// there is no boundary distinction -- boundaries are part of the shape as if it was an "interior",
    /// with respect to OGC's terminology.
    /// </summary>
    public enum SpatialRelation
    {
        //see http://docs.geotools.org/latest/userguide/library/jts/dim9.html#preparedgeometry

        NULL_VALUE, // TODO: Remove???

        /// <summary>
        /// The shape is within the target geometry. It's the converse of <see cref="CONTAINS"/>.
        /// Boundaries of shapes count too.  OGC specs refer to this relation as "COVERED BY";
        /// WITHIN is differentiated there by not including boundaries.
        /// </summary>
		WITHIN,

        /// <summary>
        /// The shape contains the target geometry. It's the converse of <see cref="WITHIN"/>.
        /// Boundaries of shapes count too.  OGC specs refer to this relation as "COVERS";
        /// CONTAINS is differentiated there by not including boundaries.
        /// </summary>
		CONTAINS,

        /// <summary>
        /// The shape shares no point in common with the target shape.
        /// </summary>
		DISJOINT,

        /// <summary>
        /// The shape shares some points/overlap with the target shape, and the relation is
        /// not more specifically <see cref="WITHIN"/> or <see cref="CONTAINS"/>.
        /// </summary>
		INTERSECTS,
        //Don't have these: TOUCHES, CROSSES, OVERLAPS, nor distinction between CONTAINS/COVERS
    }

    public static class SpatialRelationComparators
    {
        public static SpatialRelation Transpose(this SpatialRelation sr)
        {
            switch (sr)
            {
                case SpatialRelation.CONTAINS: return SpatialRelation.WITHIN;
                case SpatialRelation.WITHIN: return SpatialRelation.CONTAINS;
                default: return sr;
            }
        }

        /// <summary>
        /// If you were to call aShape.relate(bShape) and aShape.relate(cShape), you could call
        /// this to merge the intersect results as if bShape & cShape were combined into {@link MultShape}.
        /// </summary>
        /// <param name="this"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static SpatialRelation Combine(this SpatialRelation @this, SpatialRelation other)
        {
            // You can think of this algorithm as a state transition / automata.
            // 1. The answer must be the same no matter what the order is.
            // 2. If any INTERSECTS, then the result is INTERSECTS (done).
            // 3. A DISJOINT + WITHIN == INTERSECTS (done).
            // 4. A DISJOINT + CONTAINS == CONTAINS.
            // 5. A CONTAINS + WITHIN == INTERSECTS (done). (weird scenario)
            // 6. X + X == X.

            if (other == @this)
                return @this;
            if (@this == SpatialRelation.DISJOINT && other == SpatialRelation.CONTAINS
                || @this == SpatialRelation.CONTAINS && other == SpatialRelation.DISJOINT)
                return SpatialRelation.CONTAINS;
            return SpatialRelation.INTERSECTS;
        }

        public static bool Intersects(this SpatialRelation @this)
        {
            return @this != SpatialRelation.DISJOINT;
        }

        /** Not commutative!  WITHIN.inverse().inverse() != WITHIN. */
        public static SpatialRelation Inverse(this SpatialRelation @this)
        {
            switch (@this)
            {
                case SpatialRelation.DISJOINT: return SpatialRelation.CONTAINS;
                case SpatialRelation.CONTAINS: return SpatialRelation.DISJOINT;
                case SpatialRelation.WITHIN: return SpatialRelation.INTERSECTS;//not commutative!
            }
            return SpatialRelation.INTERSECTS;
        }
    }
}
