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
using Spatial4n.Core.Io;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Spatial4n.Tests.io
{
    public class WktCustomShapeParserTest : WktShapeParserTest
    {
        internal class CustomShape : Point
        {

            internal readonly string name;

            /**
             * A simple constructor without normalization / validation.
             */
            public CustomShape(string name, SpatialContext ctx)
                        : base(0, 0, ctx)
            {
                this.name = name;
            }
        }

        public WktCustomShapeParserTest()
                : base(MakeCtx())
        {
        }

        private static SpatialContext MakeCtx()
        {
            SpatialContextFactory factory = new SpatialContextFactory();
            factory.wktShapeParserClass = typeof(MyWKTShapeParser);
            return factory.NewSpatialContext();
        }

        [Fact]
        public virtual void TestCustomShape()
        {
            Assert.Equal("customShape", ((CustomShape)ctx.ReadShapeFromWkt("customShape()")).name);
            Assert.Equal("custom3d", ((CustomShape)ctx.ReadShapeFromWkt("custom3d ()")).name);//number supported
        }

        [Fact]
        public virtual void TestNextSubShapeString()
        {

            WktShapeParser.State state = ctx.WktShapeParser.NewState("OUTER(INNER(3, 5))");
            state.offset = 0;

            Assert.Equal("OUTER(INNER(3, 5))", state.NextSubShapeString());
            Assert.Equal("OUTER(INNER(3, 5))".Length, state.offset);

            state.offset = "OUTER(".Length;
            Assert.Equal("INNER(3, 5)", state.NextSubShapeString());
            Assert.Equal("OUTER(INNER(3, 5)".Length, state.offset);

            state.offset = "OUTER(INNER(".Length;
            Assert.Equal("3", state.NextSubShapeString());
            Assert.Equal("OUTER(INNER(3".Length, state.offset);
        }

        public class MyWKTShapeParser : WktShapeParser
        {
            public MyWKTShapeParser(SpatialContext ctx, SpatialContextFactory factory)
                        : base(ctx, factory)
            {
            }

            protected internal override State NewState(string wkt)
            {
                //First few lines compile, despite newState() being protected. Just proving extensibility.
                WktShapeParser other = null;
                if (false)
                    other.NewState(wkt);

                return new State(this, wkt);
            }

            protected internal override IShape ParseShapeByType(State state, string shapeType)
            {
                IShape result = base.ParseShapeByType(state, shapeType);
                if (result == null && shapeType.Contains("custom"))
                {
                    state.NextExpect('(');
                    state.NextExpect(')');
                    return new CustomShape(shapeType, m_ctx);
                }
                return result;
            }
        }
    }
}
