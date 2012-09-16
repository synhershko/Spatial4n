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
        protected Dictionary<String, String> Args;
		protected bool geo = true;
        protected DistanceCalculator Calculator;
        protected Rectangle WorldBounds;

		protected SpatialContextFactory()
		{
		}

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
            String cname;
            //if (!Configuration.GetValue("SpatialContextFactory", out cname) || cname == null)
            if (!args.TryGetValue("spatialContextFactory", out cname) || cname == null)
            {
                instance = new SpatialContextFactory();
                instance.Init(args);
                return instance.NewSpatialContext();
            }
            else
            {
                Type t = Type.GetType(cname);
                instance = (SpatialContextFactory)Activator.CreateInstance(t);
                
                //See if the specified type has subclassed the "NewSpatialContext" method and if so call it to do the setup
                var subClassedMethod = t.GetMethod("NewSpatialContext", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (subClassedMethod != null)
                    return (SpatialContext)subClassedMethod.Invoke(instance, new object[] { });

                //Otherwise fallback to the default behaviour
                instance.Init(args);
                return instance.NewSpatialContext();
            }
        }

        protected void Init(Dictionary<String, String> args)
        {
            Args = args;
            InitUnits();
            InitCalculator();
            InitWorldBounds();
        }

        protected void InitUnits()
        {
			string geoStr;
			if (Args.TryGetValue("geo", out geoStr) && geoStr != null)
				bool.TryParse(geoStr, out geo);
        }

        protected void InitCalculator()
        {
            String calcStr;
            if (!Args.TryGetValue("distCalculator", out calcStr) || calcStr == null)
                return;
            if (calcStr.Equals("haversine", StringComparison.InvariantCultureIgnoreCase))
            {
                Calculator = new GeodesicSphereDistCalc.Haversine();
            }
            else if (calcStr.Equals("lawOfCosines", StringComparison.InvariantCultureIgnoreCase))
            {
                Calculator = new GeodesicSphereDistCalc.LawOfCosines();
            }
            else if (calcStr.Equals("vincentySphere", StringComparison.InvariantCultureIgnoreCase))
            {
                Calculator = new GeodesicSphereDistCalc.Vincenty();
            }
            else if (calcStr.Equals("cartesian", StringComparison.InvariantCultureIgnoreCase))
            {
                Calculator = new CartesianDistCalc();
            }
            else if (calcStr.Equals("cartesian^2", StringComparison.InvariantCultureIgnoreCase))
            {
                Calculator = new CartesianDistCalc(true);
            }
            else
            {
                throw new Exception("Unknown calculator: " + calcStr);
            }
        }

        protected void InitWorldBounds()
        {
            String worldBoundsStr;
            if (!Args.TryGetValue("worldBounds", out worldBoundsStr) || worldBoundsStr == null)
                return;

            //kinda ugly we do this just to read a rectangle.  TODO refactor
            var simpleCtx = new SpatialContext(geo, Calculator, null);
            WorldBounds = (Rectangle)simpleCtx.ReadShape(worldBoundsStr);
        }

        /** Subclasses should simply construct the instance from the initialized configuration. */
        protected virtual SpatialContext NewSpatialContext()
        {
            return new SpatialContext(geo, Calculator, WorldBounds);
        }
    }
}
