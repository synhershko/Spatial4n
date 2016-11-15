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
    /// A rectangle aligned with the axis (i.e. it is not at an angle).
    /// <para>
    /// In geospatial contexts, it may cross the international date line (-180
    /// longitude) if <see cref="CrossesDateLine"/> however it cannot pass the poles
    /// although it may span the globe.  It spans the globe if the X coordinate
    /// (Longitude) goes from -180 to 180 as seen from <see cref="MinX"/> and <see cref="MaxX"/>.
    /// </para>
    /// </summary>
    public interface IRectangle : IShape
    {
        /// <summary>
        /// Expert: Resets the state of this shape given the arguments. This is a
        /// performance feature to avoid excessive Shape object allocation as well as
        /// some argument error checking. Mutable shapes is error-prone so use with
        /// care.
        /// </summary>
        void Reset(double minX, double maxX, double minY, double maxY);

        /// <summary>
        /// The width. In geospatial contexts, this is generally in degrees longitude
        /// and is aware of the international dateline.  It will always be >= 0.
        /// </summary>
        double Width { get; }

        /// <summary>
        /// The height. In geospatial contexts, this is in degrees latitude. It will
        /// always be >= 0.
        /// </summary>
        double Height { get; }

        /// <summary>The left edge of the X coordinate.</summary>
        double MinX { get; }

        /// <summary>The bottom edge of the Y coordinate.</summary>
        double MinY { get; }

        /// <summary>The right edge of the X coordinate.</summary>
        double MaxX { get; }

        /// <summary>The top edge of the Y coordinate.</summary>
        double MaxY { get; }

        /// <summary>
        /// Only meaningful for geospatial contexts.
        /// </summary>
        bool CrossesDateLine { get; }

        /// <summary>
        /// A specialization of <see cref="IShape.Relate(IShape)"/>
        /// for a vertical line.
        /// </summary>
        SpatialRelation RelateYRange(double minY, double maxY);

        /// <summary>
        /// A specialization of <see cref="IShape.Relate(IShape)"/>
        /// for a horizontal line.
        /// </summary>
        SpatialRelation RelateXRange(double minX, double maxX);
    }
}
