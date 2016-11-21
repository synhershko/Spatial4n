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

#if FEATURE_NTS

using GeoAPI.Geometries;
using NetTopologySuite.Utilities;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Shapes;
using Xunit;

namespace Spatial4n.Core.IO
{
    public class NtsBinaryCodecTest : BinaryCodecTest
    {

        // TODO: Wtf is this?
        //      @ParametersFactory
        //public static Iterable<Object[]> parameters()
        //      {
        //          //try floats
        //          NtsSpatialContextFactory factory = new NtsSpatialContextFactory();
        //          factory.precisionModel = new PrecisionModel(PrecisionModel.FLOATING_SINGLE);

        //          return Arrays.asList($$(
        //              $(NtsSpatialContext.GEO),//doubles
        //      $(factory.newSpatialContext())//floats
        //          ));
        //      }

        public NtsBinaryCodecTest()
            : base(NtsSpatialContext.GEO)
        {
        }

        public NtsBinaryCodecTest(NtsSpatialContext ctx)
            : base(ctx)
        {
        }

        [Fact]
        public virtual void TestPoly()
        {
            NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
            ctx.MakeShape(RandomGeometry(random.Next(3, 20)), false, false);
        }

        protected override IShape RandomShape()
        {
            if (random.Next(3) == 0)
            {
                NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
                return ctx.MakeShape(RandomGeometry(random.Next(3, 20)), false, false);
            }
            else
            {
                return base.RandomShape();
            }
        }

        private IGeometry RandomGeometry(int points)
        {
            //a circle
            NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
            GeometricShapeFactory gsf = new GeometricShapeFactory(ctx.GeometryFactory);
            gsf.Centre = (new Coordinate(0, 0));
            gsf.Size = (180);//diameter
            gsf.NumPoints = (points);
            return gsf.CreateCircle();
        }
    }
}

#endif