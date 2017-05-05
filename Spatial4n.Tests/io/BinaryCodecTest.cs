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
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.IO;
using Spatial4n.Core.Shapes;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Spatial4n.Core.IO
{
    public class BinaryCodecTest
    {
        protected readonly Random random = new Random(RandomSeed.Seed());

        internal readonly SpatialContext ctx;
        private BinaryCodec binaryCodec;

        protected BinaryCodecTest(SpatialContext ctx)
        {
            this.ctx = ctx;
            binaryCodec = ctx.BinaryCodec;//stateless
        }

        public BinaryCodecTest()
            : this(SpatialContext.GEO)
        {
        }

        //This test uses WKT to specify the shapes because the Nts based subclass tests will test
        // using floats instead of doubles, and WKT is normalized whereas ctx.makeXXX is not.

        [Fact]
        public virtual void TestPoint()
        {
            AssertRoundTrip(Wkt("POINT(-10 80.3)"));
        }

        [Fact]
        public virtual void TestRect()
        {
            AssertRoundTrip(Wkt("ENVELOPE(-10, 180, 42.3, 0)"));
        }

        [Fact]
        public virtual void TestCircle()
        {
            AssertRoundTrip(Wkt("BUFFER(POINT(-10 30), 5.2)"));
        }

        [Fact]
        public virtual void TestCollection()
        {
            ShapeCollection s = ctx.MakeCollection(
                (new IShape[]
                {
                RandomShape(),
                RandomShape(),
                RandomShape()
                }).ToList()
            );
            AssertRoundTrip(s);
        }

        protected virtual IShape Wkt(string wkt)
        {
            try
            {
                return ctx.ReadShapeFromWkt(wkt);
            }
            catch (ParseException e)
            {
                throw new Exception(e.Message, e);
            }
        }

        protected virtual IShape RandomShape()
        {
            switch (random.Next(2))
            {//inclusive
                case 0: return Wkt("POINT(-10 80.3)");
                case 1: return Wkt("ENVELOPE(-10, 180, 42.3, 0)");
                case 2: return Wkt("BUFFER(POINT(-10 30), 5.2)");
                default: throw new Exception();
            }
        }

        protected virtual void AssertRoundTrip(IShape shape)
        {
            try
            {
                MemoryStream baos = new MemoryStream();
                binaryCodec.WriteShape(new BinaryWriter(baos), shape);
                MemoryStream bais = new MemoryStream(baos.ToArray());
                Assert.Equal(shape, binaryCodec.ReadShape(new BinaryReader(bais)));
            }
            catch (IOException e)
            {
                throw new Exception(e.Message, e);
            }
        }
    }
}
