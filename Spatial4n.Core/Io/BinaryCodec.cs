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
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.IO;

namespace Spatial4n.Core.Io
{
    /// <summary>
    /// A binary shape format. It is <c>not</c> designed to be a published standard, unlike Well Known
    /// Binary (WKB). The initial release is simple but it could get more optimized to use fewer bytes or
    /// to write & read pre-computed index structures.
    /// <para>
    /// Immutable and thread-safe.
    /// </para>
    /// </summary>
    public class BinaryCodec
    {
        //type 0; reserved for unkonwn/generic; see readCollection
        protected enum ShapeType : byte
        {
            TYPE_POINT = 1,
            TYPE_RECT = 2,
            TYPE_CIRCLE = 3,
            TYPE_COLL = 4,
            TYPE_GEOM = 5
        }


        //TODO support BufferedLineString

        protected readonly SpatialContext ctx;

        //This constructor is mandated by SpatialContextFactory
        public BinaryCodec(SpatialContext ctx, SpatialContextFactory factory)
        {
            this.ctx = ctx;
        }

        public virtual IShape ReadShape(BinaryReader dataInput)
        {
            byte type = dataInput.ReadByte();
            IShape s = ReadShapeByTypeIfSupported(dataInput, (ShapeType)type);
            if (s == null)
                throw new ArgumentException("Unsupported shape byte " + type);
            return s;
        }

        public virtual void WriteShape(BinaryWriter dataOutput, IShape s)
        {
            bool written = WriteShapeByTypeIfSupported(dataOutput, s);
            if (!written)
                throw new ArgumentException("Unsupported shape " + s.GetType());
        }

        protected virtual IShape ReadShapeByTypeIfSupported(BinaryReader dataInput, ShapeType type)
        {
            switch (type)
            {
                case ShapeType.TYPE_POINT: return ReadPoint(dataInput);
                case ShapeType.TYPE_RECT: return ReadRect(dataInput);
                case ShapeType.TYPE_CIRCLE: return ReadCircle(dataInput);
                case ShapeType.TYPE_COLL: return ReadCollection(dataInput);
                default: return null;
            }
        }

        /// <summary>
        /// Note: writes the type byte even if not supported
        /// </summary>
        protected virtual bool WriteShapeByTypeIfSupported(BinaryWriter dataOutput, IShape s)
        {
            ShapeType type = TypeForShape(s);
            dataOutput.Write((byte)type);
            return WriteShapeByTypeIfSupported(dataOutput, s, type);
            //dataOutput.position(dataOutput.position() - 1);//reset putting type
        }

        protected virtual bool WriteShapeByTypeIfSupported(BinaryWriter dataOutput, IShape s, ShapeType type)
        {
            switch (type)
            {
                case ShapeType.TYPE_POINT: WritePoint(dataOutput, (IPoint)s); break;
                case ShapeType.TYPE_RECT: WriteRect(dataOutput, (IRectangle)s); break;
                case ShapeType.TYPE_CIRCLE: WriteCircle(dataOutput, (ICircle)s); break;
                case ShapeType.TYPE_COLL: WriteCollection(dataOutput, (ShapeCollection)s); break;
                default:
                    return false;
            }
            return true;
        }

        protected virtual ShapeType TypeForShape(IShape s)
        {
            if (s is IPoint)
            {
                return ShapeType.TYPE_POINT;
            }
            else if (s is IRectangle)
            {
                return ShapeType.TYPE_RECT;
            }
            else if (s is ICircle)
            {
                return ShapeType.TYPE_CIRCLE;
            }
            else if (s is ShapeCollection)
            {
                return ShapeType.TYPE_COLL;
            }
            else
            {
                return 0;
            }
        }

        protected virtual double ReadDim(BinaryReader dataInput)
        {
            return dataInput.ReadDouble();
        }

        protected virtual void WriteDim(BinaryWriter dataOutput, double v)
        {
            dataOutput.Write(v);
        }

        public virtual IPoint ReadPoint(BinaryReader dataInput)
        {
            return ctx.MakePoint(ReadDim(dataInput), ReadDim(dataInput));
        }

        public virtual void WritePoint(BinaryWriter dataOutput, IPoint pt)
        {
            WriteDim(dataOutput, pt.X);
            WriteDim(dataOutput, pt.Y);
        }

        public virtual IRectangle ReadRect(BinaryReader dataInput)
        {
            return ctx.MakeRectangle(ReadDim(dataInput), ReadDim(dataInput), ReadDim(dataInput), ReadDim(dataInput));
        }

        public virtual void WriteRect(BinaryWriter dataOutput, IRectangle r)
        {
            WriteDim(dataOutput, r.MinX);
            WriteDim(dataOutput, r.MaxX);
            WriteDim(dataOutput, r.MinY);
            WriteDim(dataOutput, r.MaxY);
        }

        public virtual ICircle ReadCircle(BinaryReader dataInput)
        {
            return ctx.MakeCircle(ReadPoint(dataInput), ReadDim(dataInput));
        }

        public void WriteCircle(BinaryWriter dataOutput, ICircle c)
        {
            WritePoint(dataOutput, c.Center);
            WriteDim(dataOutput, c.Radius);
        }

        public virtual ShapeCollection ReadCollection(BinaryReader dataInput)
        {
            byte type = dataInput.ReadByte();
            int size = dataInput.ReadInt32();
            List<IShape> shapes = new List<IShape>(size);
            for (int i = 0; i < size; i++)
            {
                if (type == 0)
                {
                    shapes.Add(ReadShape(dataInput));
                }
                else
                {
                    IShape s = ReadShapeByTypeIfSupported(dataInput, (ShapeType)type);
                    if (s == null)
                        throw new InvalidShapeException("Unsupported shape byte " + type);
                    shapes.Add(s);
                }
            }
            return ctx.MakeCollection(shapes);
        }

        public virtual void WriteCollection(BinaryWriter dataOutput, ShapeCollection col)
        {
            byte type = (byte)0;//TODO add type to ShapeCollection
            dataOutput.Write(type);
            dataOutput.Write(col.Count);
            for (int i = 0; i < col.Count; i++)
            {
                IShape s = col[i];
                if (type == 0)
                {
                    WriteShape(dataOutput, s);
                }
                else
                {
                    bool written = WriteShapeByTypeIfSupported(dataOutput, s, (ShapeType)type);
                    if (!written)
                        throw new ArgumentException("Unsupported shape type " + s.GetType());
                }
            }
        }
    }
}
