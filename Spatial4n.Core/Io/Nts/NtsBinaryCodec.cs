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
using NetTopologySuite.IO;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;
using System.IO;

namespace Spatial4n.Core.Io.Nts
{
    /// <summary>
    /// Writes shapes in WKB, if it isn't otherwise supported by the superclass.
    /// </summary>
    public class NtsBinaryCodec : BinaryCodec
    {
        protected readonly bool useFloat;//instead of double
        private const int wkbXDR = 0;
        public NtsBinaryCodec(NtsSpatialContext ctx, NtsSpatialContextFactory factory)
                  : base(ctx, factory)
        {
            //note: ctx.geometryFactory hasn't been set yet
            useFloat = (factory.precisionModel.PrecisionModelType == PrecisionModels.FloatingSingle);
        }

        protected override double ReadDim(/*DataInput*/BinaryReader dataInput)
        {
            if (useFloat)
                return dataInput.ReadSingle();
            return base.ReadDim(dataInput);
        }

        protected override void WriteDim(/*DataOutput*/BinaryWriter dataOutput, double v)
        {
            if (useFloat)
                dataOutput.Write((float)v);
            else
                base.WriteDim(dataOutput, v);
        }

        protected override ShapeType TypeForShape(IShape s)
        {
            ShapeType type = base.TypeForShape(s);
            if (type == 0)
            {
                type = ShapeType.TYPE_GEOM;//handles everything
            }
            return type;
        }

        protected override IShape ReadShapeByTypeIfSupported(/*DataInput*/BinaryReader dataInput, /*byte*/ShapeType type)
        {
            if (type != ShapeType.TYPE_GEOM)
                return base.ReadShapeByTypeIfSupported(dataInput, type);
            return ReadNtsGeom(dataInput);
        }

        protected override bool WriteShapeByTypeIfSupported(/*DataOutput*/BinaryWriter dataOutput, IShape s, /*byte*/ShapeType type)
        {
            if (type != ShapeType.TYPE_GEOM)
                return base.WriteShapeByTypeIfSupported(dataOutput, s, type);
            WriteNtsGeom(dataOutput, s);
            return true;

        }


        /// <summary>
        /// Spatial4n specific class. The primary purpose of this class is
        /// to ensure the inner stream does not get disposed prematurely.
        /// </summary>
        private class InputStreamAnonymousHelper : Stream
        {
            private readonly BinaryReader dataInput;

            public InputStreamAnonymousHelper(BinaryReader dataInput)
            {
                this.dataInput = dataInput;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return Read(buffer, offset, count);
            }

            public override bool CanRead
            {
                get
                {
                    return dataInput.BaseStream.CanRead;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return dataInput.BaseStream.CanSeek;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return dataInput.BaseStream.CanWrite;
                }
            }

            public override long Length
            {
                get
                {
                    return dataInput.BaseStream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return dataInput.BaseStream.Position;
                }

                set
                {
                    dataInput.BaseStream.Position = value;
                }
            }

            public override void Flush()
            {
                dataInput.BaseStream.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return dataInput.BaseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                dataInput.BaseStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                dataInput.BaseStream.Write(buffer, offset, count);
            }
        }

        public IShape ReadNtsGeom(/*DataInput*/BinaryReader dataInput)
        {
            NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
            WKBReader reader = new WKBReader(ctx.GeometryFactory);
            try
            {
                Stream inStream = new InputStreamAnonymousHelper(dataInput);
                IGeometry geom = reader.Read(inStream);
                //false: don't check for dateline-180 cross or multi-polygon overlaps; this won't happen
                // once it gets written, and we're reading it now
                return ctx.MakeShape(geom, false, false);

            }
            catch (GeoAPI.IO.ParseException ex)
            {
                throw new InvalidShapeException("error reading WKT", ex);
            }
        }

        /// <summary>
        /// Spatial4n specific class. The primary purpose of this class is
        /// to ensure the inner stream does not get disposed prematurely.
        /// </summary>
        private class OutputStreamAnonymousHelper : Stream
        {
            private readonly BinaryWriter dataOutput;
            public OutputStreamAnonymousHelper(BinaryWriter dataOutput)
            {
                this.dataOutput = dataOutput;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                dataOutput.BaseStream.Write(buffer, offset, count);
            }

            public override bool CanRead
            {
                get
                {
                    return dataOutput.BaseStream.CanRead;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return dataOutput.BaseStream.CanSeek;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return dataOutput.BaseStream.CanWrite;
                }
            }

            public override long Length
            {
                get
                {
                    return dataOutput.BaseStream.Length;
                }
            }

            public override long Position
            {
                get
                {
                    return dataOutput.BaseStream.Position;
                }

                set
                {
                    dataOutput.BaseStream.Position = value;
                }
            }

            public override void Flush()
            {
                dataOutput.BaseStream.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return dataOutput.BaseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                dataOutput.BaseStream.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return dataOutput.BaseStream.Read(buffer, offset, count);
            }
        }

        public void WriteNtsGeom(/*DataOutput*/BinaryWriter dataOutput, IShape s)
        {
            NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
            IGeometry geom = ctx.GetGeometryFrom(s);//might even translate it
            new WKBWriter().Write(geom, new OutputStreamAnonymousHelper(dataOutput));
        }
    }
}
