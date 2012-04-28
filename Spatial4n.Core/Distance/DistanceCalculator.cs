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
		double Distance(Point @from, Point to);
		double Distance(Point @from, double toX, double toY);

		Point PointOnBearing(Point @from, double dist, double bearingDEG, SpatialContext ctx);

		/// <summary>
		/// Converts a distance to radians (multiples of the radius). A spherical
		/// earth model is assumed for geospatial, and non-geospatial is the identity function.
		/// </summary>
		/// <param name="distance"></param>
		/// <returns></returns>
		double DistanceToDegrees(double distance);

		double DegreesToDistance(double degrees);

		//public Point pointOnBearing(Point from, double angle);

		Rectangle CalcBoxByDistFromPt(Point from, double distance, SpatialContext ctx);

		double CalcBoxByDistFromPtHorizAxis(Point from, double distance, SpatialContext ctx);
	}
}
