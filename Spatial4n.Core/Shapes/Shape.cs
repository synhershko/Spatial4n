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

namespace Spatial4n.Core.Shapes
{
    /// <summary>
    /// The base interface defining a geometric shape. Shape instances should be
    /// instantiated via one of the create* methods on a <see cref="SpatialContext"/> or
    /// by reading WKT which calls those methods; they should <c>not</c> be
    /// created directly.
    /// <para>
    /// Shapes are generally immutable and thread-safe. If a particular shape has a
    /// <c>Reset(...)</c> method then its use means the shape is actually
    /// mutable. Mutating shape state is considered expert and should be done with care.
    /// </para>
    /// </summary>
    public interface IShape
    {
        /// <summary>
        /// Describe the relationship between the two objects.  For example
        /// <list type="bullet">
        ///   <item>this is <see cref="SpatialRelation.Within"/> other</item>
        ///   <item>this <see cref="SpatialRelation.Contains"/> other</item>
        ///   <item>this is <see cref="SpatialRelation.Disjoint"/> other</item>
        ///   <item>this <see cref="SpatialRelation.Intersects"/> other</item>
        /// </list>
        /// Note that a <see cref="IShape"/> implementation may choose to return <see cref="SpatialRelation.Intersects"/> when the
        /// true answer is <see cref="SpatialRelation.Within"/> or <see cref="SpatialRelation.Contains"/> for performance reasons. If a shape does
        /// this then it <i>must</i> document when it does.  Ideally the shape will not
        /// do this approximation in all circumstances, just sometimes.
        /// <p />
        /// If the shapes are equal then the result is <see cref="SpatialRelation.Contains"/> (preferred) or <see cref="SpatialRelation.Within"/>.
        /// </summary>
        SpatialRelation Relate(IShape other);

        /// <summary>
        /// Get the bounding box for this <see cref="IShape"/>. This means the shape is within the
        /// bounding box and that it touches each side of the rectangle.
        /// <p/>
        /// Postcondition: <c>this.BoundingBox.Relate(this) == SpatialRelation.Contains</c>
        /// </summary>
        IRectangle BoundingBox { get; }

        /// <summary>
        /// Does the shape have area?  This will be false for points and lines. It will
        /// also be false for shapes that normally have area but are constructed in a
        /// degenerate case as to not have area (e.g. a circle with 0 radius or
        /// rectangle with no height or no width).
        /// </summary>
        /// <returns></returns>
        bool HasArea { get; }

        /// <summary>
        /// Calculates the area of the shape, in square-degrees. If ctx is null then
        /// simple Euclidean calculations will be used.  This figure can be an
        /// estimate.
        /// </summary>
        double GetArea(SpatialContext? ctx);

        /// <summary>
        /// Returns the center point of this shape. This is usually the same as
        /// <c>BoundingBox.Center</c> but it doesn't have to be.
        /// <para/>
        /// Postcondition: <c>this.Relate(this.Center) == SpatialContext.Contains</c>
        /// </summary>
        IPoint Center { get; }

        /// <summary>
        /// Returns a buffered version of this shape.  The buffer is usually a
        /// rounded-corner buffer, although some shapes might buffer differently. This
        /// is an optional operation.
        /// </summary>
        /// <returns>Not null, and the returned shape should contain the current shape.</returns>
        IShape GetBuffered(double distance, SpatialContext ctx);

        /// <summary>
        /// Shapes can be "empty", which is to say it exists nowhere. The underlying coordinates are
        /// typically NaN.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// The sub-classes of <see cref="IShape"/> generally implement the
        /// same contract for <see cref="object.Equals(object)"/> and <see cref="object.GetHashCode()"/>
        /// amongst the same sub-interface type.  This means, for example, that multiple
        /// Point implementations of different classes are equal if they share the same x
        /// &amp; y.
        /// </summary>
        bool Equals(object other);
    }
}
