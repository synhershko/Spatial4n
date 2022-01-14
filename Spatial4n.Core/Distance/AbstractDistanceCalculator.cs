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
    public abstract class AbstractDistanceCalculator : IDistanceCalculator
    {
        public virtual double Distance(IPoint @from, IPoint to)
        {
            return Distance(from, to.X, to.Y);
        }

        public virtual bool Within(IPoint from, double toX, double toY, double distance)
        {
            return Distance(from, toX, toY) <= distance;
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        // spatial4n specific - need to implement abstract members here to satisfy the IDistanceCalculator contract
        public abstract double Distance(IPoint from, double toX, double toY);
        public abstract IPoint PointOnBearing(IPoint from, double distDEG, double bearingDEG, SpatialContext ctx, IPoint? reuse);
        public abstract IRectangle CalcBoxByDistFromPt(IPoint from, double distDEG, SpatialContext ctx, IRectangle? reuse);
        public abstract double CalcBoxByDistFromPt_yHorizAxisDEG(IPoint from, double distDEG, SpatialContext ctx);
        public abstract double Area(IRectangle rect);
        public abstract double Area(ICircle circle);
    }
}
