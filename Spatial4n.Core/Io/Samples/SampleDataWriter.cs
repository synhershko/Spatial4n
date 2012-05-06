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
using System.IO;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Spatial4n.Core.Io.Samples
{
	public class SampleDataWriter
	{
		protected readonly TextWriter _out;
		protected readonly SpatialContext ctx;
		protected readonly bool bbox;
		protected readonly int maxLength;

		public SampleDataWriter(string filePath, SpatialContext ctx, bool bbox, int maxLength)
		{
			this.ctx = ctx;
			this.bbox = bbox;
			this.maxLength = maxLength;

			_out = new StringWriter(/*new FileStream(filePath, FileMode.Create)*/);

			_out.Write("#id");
			_out.Write('\t');
			_out.Write("name");
			_out.Write('\t');
			_out.Write("shape");
			_out.Write('\t');
			_out.WriteLine();
			_out.Flush();
		}

		//protected String ToString(String name, Shape shape)
		//{
		//    String v = ctx.ToString(shape);
		//    if (maxLength > 0 && v.Length > maxLength)
		//    {
		//        Geometry g = ((JtsSpatialContext)ctx).getGeometryFrom(shape);

		//        long last = v.length();
		//        Envelope env = g.getEnvelopeInternal();
		//        double mins = Math.min(env.getWidth(), env.getHeight());
		//        double div = 1000;
		//        while (v.length() > maxLength)
		//        {
		//            double tolerance = mins / div;
		//            System._out.println(name + " :: Simplifying long geometry: WKT.length=" + v.length() + " tolerance=" + tolerance);
		//            Geometry simple = TopologyPreservingSimplifier.simplify(g, tolerance);
		//            v = simple.toText();
		//            if (v.length() == last)
		//            {
		//                System._out.println(name + " :: Can not simplify geometry smaller then max. " + last);
		//                break;
		//            }
		//            last = v.length();
		//            div *= .70;
		//        }
		//    }
		//    return v;
		//}

		public void Write(String id, String name, double x, double y)
		{
			this.Write(id, name, ctx.MakePoint(x, y));
		}

		public void Write(String id, String name, Shape shape)
		{
			String geo = string.Empty;//ToString(name, bbox ? shape.GetBoundingBox() : shape);
			_out.Write(id);
			_out.Write('\t');
			_out.Write(name);
			_out.Write('\t');
			_out.Write(geo);
			_out.Write('\t');
			_out.WriteLine();
			_out.Flush();
		}

		public void Close()
		{
			_out.Close();
		}
	}
}
