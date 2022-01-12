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

namespace Spatial4n.Core.Shapes
{
    /// <summary>
    /// The set of spatial relationships. Naming is consistent with OGC spec conventions as seen in SQL/MM and others.
    /// <para>
    /// No equality case.  If two <see cref="IShape"/> instances are equal then the result might be <see cref="Contains"/> (preferred) or <see cref="Within"/>.
    /// Client logic may have to be aware of this edge condition; Spatial4n testing certainly does.
    /// </para>
    /// <para></para>
    /// The <see cref="Contains"/> and <see cref="Within"/> wording here is inconsistent with OGC; these here map to OGC
    /// "COVERS" and "COVERED BY", respectively. The distinction is in the boundaries; in Spatial4n
    /// there is no boundary distinction -- boundaries are part of the shape as if it was an "interior",
    /// with respect to OGC's terminology.
    /// </summary>
    public enum SpatialRelation
    {
        //see http://docs.geotools.org/latest/userguide/library/jts/dim9.html#preparedgeometry

        /// <summary>
        /// Used in .NET for mimicking the ability to set an enum to null in Java.
        /// Set to zero explicitly to ensure it will be the default value for an 
        /// uninitialized SpatialRelation variable.
        /// </summary>
        None = 0,
        [Obsolete("Use None instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        NOT_SET = 0,

        /// <summary>
        /// The shape is within the target geometry. It's the converse of <see cref="Contains"/>.
        /// Boundaries of shapes count too.  OGC specs refer to this relation as "COVERED BY";
        /// <see cref="Within"/> is differentiated thereby not including boundaries.
        /// </summary>
        Within = 1,
        [Obsolete("Use Within instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), CLSCompliant(false)]
        WITHIN = 1,

        /// <summary>
        /// The shape contains the target geometry. It's the converse of <see cref="Within"/>.
        /// Boundaries of shapes count too.  OGC specs refer to this relation as "COVERS";
        /// <see cref="Contains"/> is differentiated thereby not including boundaries.
        /// </summary>
        Contains = 2,
        [Obsolete("Use Contains instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), CLSCompliant(false)]
        CONTAINS = 2,

        /// <summary>
        /// The shape shares no point in common with the target shape.
        /// </summary>
        Disjoint = 3,
        [Obsolete("Use Disjoint instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), CLSCompliant(false)]
        DISJOINT = 3,

        /// <summary>
        /// The shape shares some points/overlap with the target shape, and the relation is
        /// not more specifically <see cref="Within"/> or <see cref="Contains"/>.
        /// </summary>
        Intersects = 4,
        [Obsolete("Use Intersects instead. This const will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never), CLSCompliant(false)]
        INTERSECTS = 4,
        //Don't have these: TOUCHES, CROSSES, OVERLAPS, nor distinction between CONTAINS/COVERS
    }

    /// <summary>
    /// Extensions to <see cref="SpatialRelation"/>.
    /// </summary>
    public static class SpatialRelationExtensions
    {
        /// <summary>
        /// Given the result of <c>shapeA.Relate(shapeB)</c>, transposing that
        /// result should yield the result of <c>shapeB.Relate(shapeA)</c>. There
        /// is a corner case is when the shapes are equal, in which case actually
        /// flipping the Relate() call will result in the same value -- either <see cref="SpatialRelation.Contains"/>
        /// or <see cref="SpatialRelation.Within"/>; this method can't possible check for that so the caller might
        /// have to.
        /// </summary>
        public static SpatialRelation Transpose(this SpatialRelation relation)
        {
            return relation switch
            {
                SpatialRelation.Contains => SpatialRelation.Within,
                SpatialRelation.Within => SpatialRelation.Contains,
                _ => relation,
            };
        }

        /// <summary>
        /// If you were to call <c>aShape.Relate(bShape)</c> and <c>aShape.Relate(cShape)</c>, you
        /// could call this to merge the intersect results as if <c>bShape &amp; cShape</c> were
        /// combined into <see cref="ShapeCollection"/>.
        /// </summary>
        public static SpatialRelation Combine(this SpatialRelation relation, SpatialRelation other)
        {
            // You can think of this algorithm as a state transition / automata.
            // 1. The answer must be the same no matter what the order is.
            // 2. If any INTERSECTS, then the result is INTERSECTS (done).
            // 3. A DISJOINT + WITHIN == INTERSECTS (done).
            // 4. A DISJOINT + CONTAINS == CONTAINS.
            // 5. A CONTAINS + WITHIN == INTERSECTS (done). (weird scenario)
            // 6. X + X == X.

            if (other == relation)
                return relation;
            if (relation == SpatialRelation.Disjoint && other == SpatialRelation.Contains
                || relation == SpatialRelation.Contains && other == SpatialRelation.Disjoint)
                return SpatialRelation.Contains;
            return SpatialRelation.Intersects;
        }

        /// <summary>
        /// Not <see cref="SpatialRelation.Disjoint"/>, i.e. there is some sort of intersection.
        /// </summary>
        public static bool Intersects(this SpatialRelation relation)
        {
            return relation != SpatialRelation.Disjoint;
        }

        /// <summary>
        /// If <c>aShape.Relate(bShape)</c> is r, then <c>r.Inverse()</c>
        /// is <c>Inverse(aShape).Relate(bShape)</c> whereas
        /// <c>Inverse(shape)</c> is theoretically the opposite area covered by a
        /// shape, i.e. everywhere but where the shape is.
        /// <para/>
        /// Note that it's not commutative!  <c>SpatialRelation.Within.Inverse().Inverse() !=
        /// SpatialRelation.Within</c>.
        /// </summary>
        public static SpatialRelation Inverse(this SpatialRelation relation)
        {
            return relation switch
            {
                SpatialRelation.Disjoint => SpatialRelation.Contains,
                SpatialRelation.Contains => SpatialRelation.Disjoint,
                SpatialRelation.Within => SpatialRelation.Intersects,//not commutative!
                _ => SpatialRelation.Intersects,
            };
        }
    }

    [Obsolete("Use SpatialRelationExtensions instead. This will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class SpatialRelationComparators
    {

        [Obsolete("Use SpatialRelationExtensions.Transpose() instead. This will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static SpatialRelation Transpose(SpatialRelation sr) => sr.Transpose();

        [Obsolete("Use SpatialRelationExtensions.Combine() instead. This will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static SpatialRelation Combine(SpatialRelation @this, SpatialRelation other) => @this.Combine(other);

        [Obsolete("Use SpatialRelationExtensions.Intersects() instead. This will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static bool Intersects(SpatialRelation @this) => @this.Intersects();

        [Obsolete("Use SpatialRelationExtensions.Inverse() instead. This will be removed in 0.5.0."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static SpatialRelation Inverse(SpatialRelation @this) => @this.Inverse();
    }
}
