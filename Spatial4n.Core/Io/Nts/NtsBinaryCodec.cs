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

        protected override ShapeType TypeForShape(Shape s)
        {
            ShapeType type = base.TypeForShape(s);
            if (type == 0)
            {
                type = ShapeType.TYPE_GEOM;//handles everything
            }
            return type;
        }

        protected override Shape ReadShapeByTypeIfSupported(/*DataInput*/BinaryReader dataInput, /*byte*/ShapeType type)
        {
            if (type != ShapeType.TYPE_GEOM)
                return base.ReadShapeByTypeIfSupported(dataInput, type);
            return ReadNtsGeom(dataInput);
        }

        protected override bool WriteShapeByTypeIfSupported(/*DataOutput*/BinaryWriter dataOutput, Shape s, /*byte*/ShapeType type)
        {
            if (type != ShapeType.TYPE_GEOM)
                return base.WriteShapeByTypeIfSupported(dataOutput, s, type);
            WriteNtsGeom(dataOutput, s);
            return true;

        }

        private class InputStreamAnonymousHelper : Stream
        {
            private readonly BinaryReader dataInput;
            bool first = true;

            public InputStreamAnonymousHelper(BinaryReader dataInput)
            {
                this.dataInput = dataInput;
            }


            public override int Read(byte[] buffer, int offset, int count)
            {
                return Read(buffer, offset, count);
            }

            //public override int Read(byte[] buffer, int offset, int count)
            //{
            //    if (first)
            //    {//we don't write NTS's leading BOM so synthesize reading it
            //        if (count != 1)
            //            throw new InvalidOperationException("Expected initial read of one byte, not: " + count);
            //        buffer[0] = wkbXDR; //WKBConstants.wkbXDR;//0
            //        first = false;
            //        return 1;
            //    }
            //    else
            //    {
            //        //TODO for performance, specialize for common array lengths: 1, 4, 8
            //        //dataInput.ReadFully(buf);
            //        //dataInput.Read()
            //        //buf = ReadFully(dataInput.BaseStream);


            //        return dataInput.BaseStream.Read(buffer, offset, count);
            //    }
            //}

            //public override int Read(byte[] buf)
            //{

            //    return 
            //}

            //private static byte[] ReadFully(Stream input)
            //{
            //    //input.Seek(0, SeekOrigin.Begin);
            //    if (input is MemoryStream)
            //    {
            //        return ((MemoryStream)input).ToArray();
            //    }

            //    using (var ms = new MemoryStream())
            //    {
            //        input.CopyTo(ms);
            //        return ms.ToArray();
            //    }
            //}

            #region Unmodified Stream Overrides
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

            //public override int Read(byte[] buffer, int offset, int count)
            //{
            //    throw new NotImplementedException();
            //}

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

            #endregion
        }

        public Shape ReadNtsGeom(/*DataInput*/BinaryReader dataInput)
        {
            NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
            WKBReader reader = new WKBReader(ctx.GetGeometryFactory());
            try
            {
                //Stream inStream = dataInput.BaseStream;
                Stream inStream = new InputStreamAnonymousHelper(dataInput);

                //Stream inStream = new InstreamAnonymousHelper();
                //////      InStream inStream = new InStream() {//a strange NTS abstraction
                //////        bool first = true;
                //////    @Override
                //////        public void read(byte[] buf) throws IOException
                //////    {
                //////          if (first) {//we don't write NTS's leading BOM so synthesize reading it
                //////            if (buf.length != 1)
                //////                throw new IllegalStateException("Expected initial read of one byte, not: " + buf.length);
                //////            buf[0] = WKBConstants.wkbXDR;//0
                //////            first = false;
                //////        } else {
                //////            //TODO for performance, specialize for common array lengths: 1, 4, 8
                //////            dataInput.readFully(buf);
                //////        }
                //////    }
                //////};
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

        private class OutputStreamAnonymousHelper : Stream
        {
            private readonly BinaryWriter dataOutput;
            public OutputStreamAnonymousHelper(BinaryWriter dataOutput)
            {
                this.dataOutput = dataOutput;
            }

            bool first = true;

            public override void Write(byte[] buffer, int offset, int count)
            {
                dataOutput.BaseStream.Write(buffer, offset, count);
            }

            //public override void Write(byte[] buffer, int offset, int count)
            //{
            //    if (first)
            //    {
            //        first = false;
            //        //skip byte order mark
            //        if (count != 1 || buffer[0] != /*WKBConstants.*/wkbXDR)//the default
            //            throw new InvalidOperationException("Unexpected WKB byte order mark");
            //        return;
            //    }
            //    dataOutput.Write(buffer, offset, count);
            //}

            #region Unmodified Stream Overrides

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

            #endregion
        }

        public void WriteNtsGeom(/*DataOutput*/BinaryWriter dataOutput, Shape s)
        {
            NtsSpatialContext ctx = (NtsSpatialContext)base.ctx;
            IGeometry geom = ctx.GetGeometryFrom(s);//might even translate it
            //var writer = new WKBWriter();
            //writer.Write(geom, dataOutput.BaseStream);
            new WKBWriter().Write(geom, new OutputStreamAnonymousHelper(dataOutput));

    //        new WKBWriter().Write(geom, new OutStream()
    //                                               {//a strange NTS abstraction
    //                                                   boolean first = true;
    //        @Override
    //                                                     public void write(byte[] buf, int len) throws IOException
    //    {
    //                                                       if (first) {
    //            first = false;
    //            //skip byte order mark
    //            if (len != 1 || buf[0] != WKBConstants.wkbXDR)//the default
    //                throw new IllegalStateException("Unexpected WKB byte order mark");
    //            return;
    //        }
    //        dataOutput.write(buf, 0, len);
    //    }
    //});
        }
    }
}
