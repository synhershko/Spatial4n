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
	public interface IShape
	{
		/// <summary>
		/// Describe the relationship between the two objects.  For example
		///		this is WITHIN other
		///		this CONTAINS other
		///		this is DISJOINT other
		///		this INTERSECTS other
		/// 
		/// The context object is optional -- it may include spatial reference.
		/// </summary>
		/// <param name="other"></param>
		/// <param name="ctx"></param>
		/// <returns></returns>
		SpatialRelation Relate(IShape other, SpatialContext ctx);

		/// <summary>
		/// Get the bounding box for this Shape
		/// </summary>
		/// <returns></returns>
		IRectangle GetBoundingBox();

		/// <summary>
		/// 
		/// </summary>
		/// <returns>true if the shape has area.  This will be false for points and lines</returns>
		bool HasArea();

		IPoint GetCenter();
	}
}
