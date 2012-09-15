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
using GeoAPI.Geometries;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Impl;
using Spatial4n.Core.Shapes.Nts;

namespace Spatial4n.Core.Io
{
	public class NtsShapeReadWriter : ShapeReadWriter
	{
		private const byte TYPE_POINT = 0;
		private const byte TYPE_BBOX = 1;
		private const byte TYPE_GEOM = 2;

        private bool normalizeGeomCoords = true;//TODO make configurable

		public NtsShapeReadWriter(NtsSpatialContext ctx)
			: base(ctx)
		{
		}

		private class ShapeReaderWriterCoordinateSequenceFilter : ICoordinateSequenceFilter
		{
			private readonly NtsSpatialContext _ctx;
		    private readonly bool _normalizeGeomCoords;
		    private bool changed = false;

			public ShapeReaderWriterCoordinateSequenceFilter(NtsSpatialContext ctx, bool normalizeGeomCoords)
			{
			    _ctx = ctx;
			    _normalizeGeomCoords = normalizeGeomCoords;
			}

		    public void Filter(ICoordinateSequence seq, int i)
			{
			    double x = seq.GetX(i);
			    double y = seq.GetY(i);

			    if (_ctx.IsGeo() && _normalizeGeomCoords)
			    {
			        double xNorm = DistanceUtils.NormLonDEG(x);
			        if (x != xNorm)
			        {
			            changed = true;
			            seq.SetOrdinate(i, Ordinate.X, xNorm);
			        }

			        double yNorm = DistanceUtils.NormLatDEG(y);
			        if (y != yNorm)
			        {
			            changed = true;
			            seq.SetOrdinate(i, Ordinate.Y, yNorm);
			        }
			    }
			    else
			    {
			        _ctx.VerifyX(x);
			        _ctx.VerifyY(y);
			    }
			}

		    public bool Done
			{
				get { return false; }
			}

			public bool GeometryChanged
			{
				get { return changed; }
			}
		}

		private void CheckCoordinates(IGeometry geom)
		{
            geom.Apply(new ShapeReaderWriterCoordinateSequenceFilter((NtsSpatialContext)Ctx, normalizeGeomCoords));
		}

		/// Reads the standard shape format + WKT.
		public override Shape ReadShape(String str)
		{
			var shape = base.ReadStandardShape(str);
			if (shape == null)
			{
				try
				{
					var reader = new WKTReader(((NtsSpatialContext)Ctx).GetGeometryFactory());
					var geom = reader.Read(str);

					//Normalize coordinates to geo boundary
					CheckCoordinates(geom);

					var ntsPoint = geom as NetTopologySuite.Geometries.Point;
					if (ntsPoint != null)
					{
						return new NtsPoint(ntsPoint, Ctx);
					}
					else if (geom.IsRectangle)
					{
						bool crossesDateline = false;
						if (Ctx.IsGeo())
						{
							//Polygon points are supposed to be counter-clockwise order. If JTS says it is clockwise, then
							// it's actually a dateline crossing rectangle.
							crossesDateline = !CGAlgorithms.IsCCW(geom.Coordinates);
						}
						Envelope env = geom.EnvelopeInternal;
						if (crossesDateline)
							return new RectangleImpl(env.MaxX, env.MinX, env.MinY, env.MaxY, Ctx);
						else
							return new RectangleImpl(env.MinX, env.MaxX, env.MinY, env.MaxY, Ctx);
					}
					return new NtsGeometry(geom, (NtsSpatialContext) Ctx, true);
				}
				catch (NetTopologySuite.IO.ParseException ex)
				{
					throw new InvalidShapeException("error reading WKT", ex);
				}
			}
			return shape;
		}

		public override String WriteShape(Shape shape)
		{
			var jtsGeom = shape as NtsGeometry;
			if (jtsGeom != null)
			{
				return jtsGeom.GetGeom().AsText();
			}
			return base.WriteShape(shape);
		}

		/**
   * Reads a shape from a byte array, using an internal format written by
   * {@link #writeShapeToBytes(com.spatial4j.core.shape.Shape)}.
   */
		public Shape ReadShapeFromBytes(byte[] array, int offset, int length)
		{
			using (var stream = new MemoryStream(array, offset, length, false))
			using (var bytes = new BinaryReader(stream))
			{

				var type = bytes.ReadByte();
				if (type == TYPE_POINT)
				{
					return new NtsPoint(((NtsSpatialContext)Ctx).GetGeometryFactory().CreatePoint(new Coordinate(bytes.ReadDouble(), bytes.ReadDouble())), Ctx);
				}

				if (type == TYPE_BBOX)
				{
					return new RectangleImpl(
						bytes.ReadDouble(), bytes.ReadDouble(),
						bytes.ReadDouble(), bytes.ReadDouble(), Ctx);
				}

				if (type == TYPE_GEOM)
				{
					var reader = new WKBReader(((NtsSpatialContext)Ctx).GetGeometryFactory());
					try
					{
						IGeometry geom = reader.Read(stream);

						CheckCoordinates(geom);
						return new NtsGeometry(geom, (NtsSpatialContext)Ctx, true);
					}
					catch (ParseException ex)
					{
						throw new InvalidShapeException("error reading WKT", ex);
					}
					catch (IOException ex)
					{
						throw new InvalidShapeException("error reading WKT", ex);
					}
				}

				throw new InvalidShapeException("shape not handled: " + type);
			}

		}

		// Writes shapes in an internal format readable by {@link #readShapeFromBytes(byte[], int, int)}.
		public byte[] WriteShapeToBytes(Shape shape)
		{
			var p = shape as Shapes.Point;
			if (p != null)
			{
				using (var stream = new MemoryStream(1 + (2 * 8)))
				using (var bytes = new BinaryWriter(stream))
				{
					bytes.Write(TYPE_POINT);
					bytes.Write(p.GetX());
					bytes.Write(p.GetY());
					return stream.ToArray();
				}
			}

			var rect = shape as Rectangle;
			if (rect != null)
			{

				using (var stream = new MemoryStream(1 + (4 * 8)))
				using (var bytes = new BinaryWriter(stream))
				{
					bytes.Write(TYPE_BBOX);
					bytes.Write(rect.GetMinX());
					bytes.Write(rect.GetMaxX());
					bytes.Write(rect.GetMinY());
					bytes.Write(rect.GetMaxY());
					return stream.ToArray();
				}
			}

			var ntsShape = shape as NtsGeometry;
			if (ntsShape != null)
			{
				var writer = new WKBWriter();
				byte[] bb = writer.Write(ntsShape.GetGeom());
				using (var stream = new MemoryStream(1 + bb.Length))
				using (var bytes = new BinaryWriter(stream))
				{
					bytes.Write(TYPE_GEOM);
					bytes.Write(bb);
					return stream.ToArray();
				}
			}

			throw new ArgumentException("unsuported shape:" + shape);
		}

	}
}
