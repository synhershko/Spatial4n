using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Io.Nts
{
    /// <summary>
    /// Writes shapes in WKB, if it isn't otherwise supported by the superclass.
    /// </summary>
    public class NtsBinaryCodec : BinaryCodec
    {
        protected readonly bool useFloat;//instead of double

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

        protected override ShapeType TypeForShape(Shape s)
        {
            ShapeType type = base.TypeForShape(s);
            if (type == 0)
            {
                type = TYPE_GEOM;//handles everything
            }
            return type;
        }

        protected override Shape ReadShapeByTypeIfSupported(/*DataInput*/BinaryReader dataInput, /*byte*/ShapeType type)
        {
            if (type != TYPE_GEOM)
                return base.ReadShapeByTypeIfSupported(dataInput, type);
            return ReadNtsGeom(dataInput);
        }

        protected override bool WriteShapeByTypeIfSupported(/*DataOutput*/BinaryWriter dataOutput, Shape s, /*byte*/ShapeType type)
        {
            if (type != TYPE_GEOM)
                return base.WriteShapeByTypeIfSupported(dataOutput, s, type);
            WriteNtsGeom(dataOutput, s);
            return true;

        }

        private class InstreamAnonymousHelper : Stream
        {
            bool first = true;

            //public override int Read(byte[] buffer, int offset, int count)
            //{
            //    Read(buffer, 0, buffer.Length);
            //}

            public override void Read(byte[] buf)
            {
                if (first)
                {//we don't write JTS's leading BOM so synthesize reading it
                    if (buf.Length != 1)
                        throw new InvalidOperationException("Expected initial read of one byte, not: " + buf.Length);
                    buf[0] = WKBConstants.wkbXDR;//0
                    first = false;
                }
                else
                {
                    //TODO for performance, specialize for common array lengths: 1, 4, 8
                    dataInput.ReadFully(buf);
                }
            }
        }

        private class WriteStreamAnonymousHelper : Stream
        {
            private readonly BinaryWriter dataOutput;
            public WriteStreamAnonymousHelper(BinaryWriter dataOutput)
            {
                this.dataOutput = dataOutput;
            }

            bool first = true;

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (first)
                {
                    first = false;
                    //skip byte order mark
                    if (count != 1 || buffer[0] != WKBConstants.wkbXDR)//the default
                        throw new InvalidOperationException("Unexpected WKB byte order mark");
                    return;
                }
                dataOutput.Write(buffer, 0, count);
            }
        }

        public Shape ReadNtsGeom(/*DataInput*/BinaryReader dataInput)
        {
            NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
            WKBReader reader = new WKBReader(ctx.GetGeometryFactory());
            try
            {
                InStream inStream = new InstreamAnonymousHelper();
                //      InStream inStream = new InStream() {//a strange NTS abstraction
                //        bool first = true;
                //    @Override
                //        public void read(byte[] buf) throws IOException
                //    {
                //          if (first) {//we don't write JTS's leading BOM so synthesize reading it
                //            if (buf.length != 1)
                //                throw new IllegalStateException("Expected initial read of one byte, not: " + buf.length);
                //            buf[0] = WKBConstants.wkbXDR;//0
                //            first = false;
                //        } else {
                //            //TODO for performance, specialize for common array lengths: 1, 4, 8
                //            dataInput.readFully(buf);
                //        }
                //    }
                //};
                Geometry geom = reader.Read(inStream);
                //false: don't check for dateline-180 cross or multi-polygon overlaps; this won't happen
                // once it gets written, and we're reading it now
                return ctx.MakeShape(geom, false, false);
            }
            catch (GeoAPI.IO.ParseException ex)
            {
                throw new InvalidShapeException("error reading WKT", ex);
            }
        }

        public void WriteNtsGeom(/*DataOutput*/BinaryWriter dataOutput, Shape s)
        {
            NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
            Geometry geom = ctx.GetGeometryFrom(s);//might even translate it
                                                   //    new WKBWriter().Write(geom, new OutStream()
                                                   //{//a strange JTS abstraction
                                                   //    boolean first = true;
                                                   //    @Override
                                                   //      public void write(byte[] buf, int len) throws IOException
                                                   //{
                                                   //        if (first) {
                                                   //        first = false;
                                                   //        //skip byte order mark
                                                   //        if (len != 1 || buf[0] != WKBConstants.wkbXDR)//the default
                                                   //            throw new IllegalStateException("Unexpected WKB byte order mark");
                                                   //        return;
                                                   //    }
                                                   //    dataOutput.write(buf, 0, len);
                                                   //}
                                                   //    });
        }
    }
}
