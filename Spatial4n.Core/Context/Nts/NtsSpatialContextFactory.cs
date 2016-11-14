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

using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using Spatial4n.Core.Io.Nts;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Spatial4n.Core.Context.Nts
{
    public class NtsSpatialContextFactory : SpatialContextFactory
    {
        protected static readonly PrecisionModel defaultPrecisionModel = new PrecisionModel();//floating

        //These 3 are NTS defaults for new GeometryFactory()
        public PrecisionModel precisionModel = defaultPrecisionModel;
        public int srid = 0;
        public ICoordinateSequenceFactory coordinateSequenceFactory = CoordinateArraySequenceFactory.Instance;

        //ignored if geo=false
        public DatelineRule datelineRule = DatelineRule.Width180;

        public ValidationRule validationRule = ValidationRule.Error;
        public bool autoIndex = false;
        public bool allowMultiOverlap = false;//ignored if geo=false

        //kinda advanced options:
        public bool useNtsPoint = true;
        public bool useNtsLineString = true;

        public NtsSpatialContextFactory()
        {
            base.wktShapeParserClass = typeof(NtsWktShapeParser);
            base.binaryCodecClass = typeof(NtsBinaryCodec);
        }

        protected override void Init(IDictionary<string, string> args)
        {
            base.Init(args);

            InitField("datelineRule");
            InitField("validationRule");
            InitField("autoIndex");
            InitField("allowMultiOverlap");
            InitField("useNtsPoint");
            InitField("useNtsLineString");

            string scaleStr;
            string modelStr;
            args.TryGetValue("precisionScale", out scaleStr);
            args.TryGetValue("precisionModel", out modelStr);

            if (scaleStr != null)
            {
                if (modelStr != null && !modelStr.Equals("fixed"))
                    throw new ApplicationException("Since precisionScale was specified; precisionModel must be 'fixed' but got: " + modelStr);
                precisionModel = new PrecisionModel(double.Parse(scaleStr, CultureInfo.InvariantCulture));
            }
            else if (modelStr != null)
            {
                if (modelStr.Equals("floating"))
                {
                    precisionModel = new PrecisionModel(PrecisionModels.Floating);
                }
                else if (modelStr.Equals("floating_single"))
                {
                    precisionModel = new PrecisionModel(PrecisionModels.FloatingSingle);
                }
                else if (modelStr.Equals("fixed"))
                {
                    throw new ApplicationException("For fixed model, must specifiy 'precisionScale'");
                }
                else
                {
                    throw new ApplicationException("Unknown precisionModel: " + modelStr);
                }
            }
        }

        public virtual GeometryFactory GeometryFactory
        {
            get
            {
                if (precisionModel == null || coordinateSequenceFactory == null)
                    throw new InvalidOperationException("precision model or coord seq factory can't be null");
                return new GeometryFactory(precisionModel, srid, coordinateSequenceFactory);
            }
        }

        protected internal override SpatialContext NewSpatialContext()
        {
            return new NtsSpatialContext(this);
        }
    }
}
