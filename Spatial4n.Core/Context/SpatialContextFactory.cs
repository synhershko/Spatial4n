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
using System.Collections.Generic;
using System.Reflection;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Spatial4n.Core.Context
{
    /// <summary>
    /// Factory for a SpatialContext.
    /// </summary>
    public class SpatialContextFactory
    {
        protected Dictionary<String, String> args;

        protected DistanceUnits units;
        protected DistanceCalculator calculator;
        protected Rectangle worldBounds;

        /// <summary>
        /// The factory class is lookuped up via "spatialContextFactory" in args
        /// then falling back to a Java system property (with initial caps). If neither are specified
        /// then {@link SimpleSpatialContextFactory} is chosen.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static SpatialContext MakeSpatialContext(Dictionary<String, String> args)
        {
            SpatialContextFactory instance;
            String cname = null;
            if (!args.TryGetValue("spatialContextFactory", out cname) || cname == null)
                //if (!Configuration.GetValue("SpatialContextFactory", out cname) || cname == null)
                instance = new SpatialContextFactory();
            else
            {
                Type t = Type.GetType(cname);
                instance = (SpatialContextFactory) Activator.CreateInstance(t);
            }
            instance.Init(args);
            return instance.NewSpatialContext();
        }

        protected void Init(Dictionary<String, String> args)
        {
            this.args = args;
            InitUnits();
            InitCalculator();
            InitWorldBounds();
        }

        protected void InitUnits()
        {
            String unitsStr;
            if (!args.TryGetValue("units", out unitsStr) || unitsStr == null)
                units = DistanceUnits.KILOMETERS;
            else
                units = DistanceUnits.FindDistanceUnit(unitsStr);
        }

        protected void InitCalculator()
        {
            String calcStr;
            if (!args.TryGetValue("distCalculator", out calcStr) || calcStr == null)
                return;
            if (calcStr.Equals("haversine", StringComparison.InvariantCultureIgnoreCase))
            {
                calculator = new GeodesicSphereDistCalc.Haversine(units.EarthRadius());
            }
            else if (calcStr.Equals("lawOfCosines", StringComparison.InvariantCultureIgnoreCase))
            {
                calculator = new GeodesicSphereDistCalc.LawOfCosines(units.EarthRadius());
            }
            else if (calcStr.Equals("vincentySphere", StringComparison.InvariantCultureIgnoreCase))
            {
                calculator = new GeodesicSphereDistCalc.Vincenty(units.EarthRadius());
            }
            else if (calcStr.Equals("cartesian", StringComparison.InvariantCultureIgnoreCase))
            {
                calculator = new CartesianDistCalc();
            }
            else if (calcStr.Equals("cartesian^2", StringComparison.InvariantCultureIgnoreCase))
            {
                calculator = new CartesianDistCalc(true);
            }
            else
            {
                throw new Exception("Unknown calculator: " + calcStr);
            }
        }

        protected void InitWorldBounds()
        {
            String worldBoundsStr;
            if (!args.TryGetValue("worldBounds", out worldBoundsStr) || worldBoundsStr == null)
                return;

            //kinda ugly we do this just to read a rectangle.  TODO refactor
            var simpleCtx = new SpatialContext(units, calculator, null);
            worldBounds = (Rectangle)simpleCtx.ReadShape(worldBoundsStr);
        }

        /** Subclasses should simply construct the instance from the initialized configuration. */
        protected SpatialContext NewSpatialContext()
        {
            return new SpatialContext(units, calculator, worldBounds);
        }
    }
}
