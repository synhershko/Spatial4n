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
	public interface DistanceCalculator
	{
		/// <summary>
		/// The distance between <code>from</code> and <code>to</code>.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		double Distance(Point @from, Point to);

		/// <summary>
		/// The distance between <code>from</code> and <code>Point(toX,toY)</code>.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="toX"></param>
		/// <param name="toY"></param>
		/// <returns></returns>
		double Distance(Point @from, double toX, double toY);

		/// <summary>
		/// Calculates where a destination point is given an origin (<code>from</code>)
		/// distance, and bearing (given in degrees -- 0-360).
		/// </summary>
		/// <param name="from"></param>
		/// <param name="distance"></param>
		/// <param name="bearingDEG"></param>
		/// <param name="ctx"></param>
		/// <returns></returns>
		Point PointOnBearing(Point from, double distance, double bearingDEG, SpatialContext ctx);

		/// <summary>
		/// Converts a distance (in units of the sphere's radius, e.g. km) to degrees
		/// (0-360). A spherical earth model is assumed for geospatial.  This is not
		/// implemented for non-geospatial.
		/// </summary>
		/// <param name="distance"></param>
		/// <returns></returns>
		double DistanceToDegrees(double distance);

		/// <summary>
		/// Converts distance-degrees (0-360, e.g. as a length around the sphere) to
		/// distance (in units of the sphere's radius, e.g. km). This is the opposite
		/// of {@link #distanceToDegrees(double)}.
		/// </summary>
		/// <param name="degrees"></param>
		/// <returns></returns>
		double DegreesToDistance(double degrees);

		/// <summary>
		/// Calculates the bounding box of a circle, as specified by its center point
		/// and distance.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="distance"></param>
		/// <param name="ctx"></param>
		/// <returns></returns>
		Rectangle CalcBoxByDistFromPt(Point from, double distance, SpatialContext ctx);

		/// <summary>
		/// The <code>Y</code> coordinate of the horizontal axis (e.g. left-right line)
		/// of a circle.  The horizontal axis of a circle passes through its furthest
		/// left-most and right-most edges. On a 2D plane, this result is always
		/// <code>from.getY()</code> but, perhaps surprisingly, on a sphere it is going
		/// to be slightly different.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="distance"></param>
		/// <param name="ctx"></param>
		/// <returns></returns>
		double CalcBoxByDistFromPt_yHorizAxisDEG(Point from, double distance, SpatialContext ctx);

		double Area(Rectangle rect);

		double Area(Circle circle);
	}
}
