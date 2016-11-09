using Spatial4n.Core.Context;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Io
{
    /// <summary>
    /// A binary shape format. It is <em>not</em> designed to be a published standard, unlike Well Known
    /// Binary (WKB). The initial release is simple but it could get more optimized to use fewer bytes or
    /// to write & read pre-computed index structures.
    /// <para>
    /// Immutable and thread-safe.
    /// </para>
    /// </summary>
    public class BinaryCodec
    {
        //type 0; reserved for unkonwn/generic; see readCollection
        //protected static readonly byte
        //    TYPE_POINT = 1,
        //    TYPE_RECT = 2,
        //    TYPE_CIRCLE = 3,
        //    TYPE_COLL = 4,
        //    TYPE_GEOM = 5;
        protected enum ShapeType
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

        public Shape ReadShape(/*DataInput*/BinaryReader dataInput)
        {
            byte type = dataInput.ReadByte();
            Shape s = ReadShapeByTypeIfSupported(dataInput, (ShapeType)type);
            if (s == null)
                throw new ArgumentException("Unsupported shape byte " + type);
            return s;
        }

        public void WriteShape(/*DataOutput*/BinaryWriter dataOutput, Shape s)
        {
            bool written = WriteShapeByTypeIfSupported(dataOutput, s);
            if (!written)
                throw new ArgumentException("Unsupported shape " + s.GetType());
        }

        protected Shape ReadShapeByTypeIfSupported(/*DataInput*/BinaryReader dataInput, /*byte*/ShapeType type)
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

        /** Note: writes the type byte even if not supported */
        protected bool WriteShapeByTypeIfSupported(/*DataOutput*/BinaryWriter dataOutput, Shape s)
        {
            ShapeType type = TypeForShape(s);
            dataOutput.Write((byte)type);
            return WriteShapeByTypeIfSupported(dataOutput, s, type);
            //dataOutput.position(dataOutput.position() - 1);//reset putting type
        }

        protected bool WriteShapeByTypeIfSupported(/*DataOutput*/BinaryWriter dataOutput, Shape s, /*byte*/ShapeType type)
        {
            switch (type)
            {
                case ShapeType.TYPE_POINT: WritePoint(dataOutput, (Point)s); break;
                case ShapeType.TYPE_RECT: WriteRect(dataOutput, (Rectangle)s); break;
                case ShapeType.TYPE_CIRCLE: WriteCircle(dataOutput, (Circle)s); break;
                case ShapeType.TYPE_COLL: WriteCollection(dataOutput, (ShapeCollection)s); break;
                default:
                    return false;
            }
            return true;
        }

        protected virtual /*byte*/ShapeType TypeForShape(Shape s)
        {
            if (s is Point)
            {
                return ShapeType.TYPE_POINT;
            }
            else if (s is Rectangle)
            {
                return ShapeType.TYPE_RECT;
            }
            else if (s is Circle)
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

        protected virtual double ReadDim(/*DataInput*/BinaryReader dataInput)
        {
            return dataInput.ReadDouble();
        }

        protected virtual void WriteDim(/*DataOutput*/BinaryWriter dataOutput, double v)
        {
            dataOutput.Write(v);
        }

        public Point ReadPoint(/*DataInput*/BinaryReader dataInput)
        {
            return ctx.MakePoint(ReadDim(dataInput), ReadDim(dataInput));
        }

        public void WritePoint(/*DataOutput*/BinaryWriter dataOutput, Point pt)
        {
            WriteDim(dataOutput, pt.GetX());
            WriteDim(dataOutput, pt.GetY());
        }

        public Rectangle ReadRect(/*DataInput*/BinaryReader dataInput)
        {
            return ctx.MakeRectangle(ReadDim(dataInput), ReadDim(dataInput), ReadDim(dataInput), ReadDim(dataInput));
        }

        public void WriteRect(/*DataOutput*/BinaryWriter dataOutput, Rectangle r)
        {
            WriteDim(dataOutput, r.GetMinX());
            WriteDim(dataOutput, r.GetMaxX());
            WriteDim(dataOutput, r.GetMinY());
            WriteDim(dataOutput, r.GetMaxY());
        }

        public Circle readCircle(/*DataInput*/BinaryReader dataInput)
        {
            return ctx.MakeCircle(ReadPoint(dataInput), ReadDim(dataInput));
        }

        public void writeCircle(/*DataOutput*/BinaryWriter dataOutput, Circle c)
        {
            WritePoint(dataOutput, c.GetCenter());
            WriteDim(dataOutput, c.GetRadius());
        }

        public ShapeCollection readCollection(/*DataInput*/BinaryReader dataInput)
        {
            byte type = dataInput.ReadByte();
            int size = dataInput.ReadInt32();
            List<Shape> shapes = new List<Shape>(size);
            for (int i = 0; i < size; i++)
            {
                if (type == 0)
                {
                    shapes.Add(ReadShape(dataInput));
                }
                else
                {
                    Shape s = ReadShapeByTypeIfSupported(dataInput, (ShapeType)type);
                    if (s == null)
                        throw new InvalidShapeException("Unsupported shape byte " + type);
                    shapes.Add(s);
                }
            }
            return ctx.MakeCollection(shapes);
        }

        public void writeCollection(/*DataOutput*/BinaryWriter dataOutput, ShapeCollection col)
        {
            byte type = (byte)0;//TODO add type to ShapeCollection
            dataOutput.Write(type);
            dataOutput.Write(col.Count);
            for (int i = 0; i < col.Count) ; i++) {
                Shape s = col[i];
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
