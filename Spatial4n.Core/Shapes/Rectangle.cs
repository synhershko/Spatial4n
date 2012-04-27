﻿/*
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
	public interface IRectangle :IShape
	{
		double GetWidth();
		double GetHeight();

		double GetMinX();
		double GetMinY();
		double GetMaxX();
		double GetMaxY();

		/** If {@link #hasArea()} then this returns the area, otherwise it returns 0. */
		double GetArea();
		/** Only meaningful for geospatial contexts. */
		bool GetCrossesDateLine();

		/* There is no axis line shape, and this is more efficient then creating a flat Rectangle for intersect(). */
		SpatialRelation Relate_yRange(double minY, double maxY, SpatialContext ctx);
		SpatialRelation Relate_xRange(double minX, double maxX, SpatialContext ctx);
	}
}
