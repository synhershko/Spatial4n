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
using Spatial4n.Core.Shapes;

namespace Spatial4n.Core.Distance
{
    public interface IDistanceCalculator
    {
        /// <summary>
        /// The distance between <paramref name="from"/> and <paramref name="to"/>.
        /// </summary>
        double Distance(IPoint from, IPoint to);

        /// <summary>
        /// The distance between <paramref name="from"/> and <c>Point(toX,toY)</c>.
        /// </summary>
        double Distance(IPoint from, double toX, double toY);

        /// <summary>
        /// Returns true if the distance between from and to is &lt;= distance.
        /// </summary>
        bool Within(IPoint from, double toX, double toY, double distance);

        /// <summary>
        /// Calculates where a destination point is given an origin (<c>from</c>)
        /// distance, and bearing (given in degrees -- 0-360).  If reuse is given, then
        /// this method may <see cref="IPoint.Reset(double, double)"/> it and return it.
        /// </summary>
        IPoint PointOnBearing(IPoint from, double distDEG, double bearingDEG, SpatialContext ctx, IPoint reuse);

        /// <summary>
        /// Calculates the bounding box of a circle, as specified by its center point
        /// and distance.
        /// </summary>
        IRectangle CalcBoxByDistFromPt(IPoint from, double distDEG, SpatialContext ctx, IRectangle? reuse);

        /// <summary>
        /// The <c>Y</c> coordinate of the horizontal axis of a circle that has maximum width. On a
        /// 2D plane, this result is always <c>from.Y</c> but, perhaps surprisingly, on a sphere
        /// it is going to be slightly different.
        /// </summary>
        double CalcBoxByDistFromPt_yHorizAxisDEG(IPoint from, double distDEG, SpatialContext ctx);

        double Area(IRectangle rect);

        double Area(ICircle circle);
    }
}
