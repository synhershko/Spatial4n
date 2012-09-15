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
	public interface Rectangle :Shape
	{
		double GetWidth();
		double GetHeight();

		double GetMinX();
		double GetMinY();
		double GetMaxX();
		double GetMaxY();

		/// <summary>
		/// Only meaningful for geospatial contexts.
		/// </summary>
		/// <returns></returns>
		bool GetCrossesDateLine();

        /// <summary>
        /// Expert: Resets the state of this shape given the arguments. This is a
        /// performance feature to avoid excessive Shape object allocation as well as
        /// some argument error checking. Mutable shapes is error-prone so use with
        /// care.
        /// </summary>
        /// <param name="minX"></param>
        /// <param name="maxX"></param>
        /// <param name="minY"></param>
        /// <param name="maxY"></param>
        void Reset(double minX, double maxX, double minY, double maxY);

		/* There is no axis line shape, and this is more efficient then creating a flat Rectangle for intersect(). */
		SpatialRelation RelateYRange(double minY, double maxY);
		SpatialRelation RelateXRange(double minX, double maxX);
	}
}
