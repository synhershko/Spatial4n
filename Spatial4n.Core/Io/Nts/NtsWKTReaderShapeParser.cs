using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;
using Spatial4n.Core.Shapes.Nts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Io.Nts
{
    public class NtsWKTReaderShapeParser : NtsWktShapeParser
    {
        //Note: Historically, the code here originated from the defunct JtsShapeReadWriter.

        public NtsWKTReaderShapeParser(NtsSpatialContext ctx, NtsSpatialContextFactory factory)
            : base(ctx, factory)
        {
        }

        public override Shape ParseIfSupported(string wktString)
        {
            return ParseIfSupported(wktString, new WKTReader(ctx.GetGeometryFactory()));
        }

        /**
         * Reads WKT from the {@code str} via JTS's {@link com.vividsolutions.jts.io.WKTReader}.
         * @param str
         * @param reader <pre>new WKTReader(ctx.getGeometryFactory()))</pre>
         * @return Non-Null
         */
        protected Shape ParseIfSupported(string str, WKTReader reader)
        {
            try
            {
                IGeometry geom = reader.Read(str);

                //Normalizes & verifies coordinates
                CheckCoordinates(geom);

                if (geom is NetTopologySuite.Geometries.Point)
                {
                    NetTopologySuite.Geometries.Point ptGeom = (NetTopologySuite.Geometries.Point)geom;
                    if (ctx.UseNtsPoint)
                        return new NtsPoint(ptGeom, ctx);
                    else
                        return ctx.MakePoint(ptGeom.X, ptGeom.Y);
                }
                else if (geom.IsRectangle)
                {
                    return base.MakeRectFromPoly(geom);
                }
                else
                {
                    return base.MakeShapeFromGeometry(geom);
                }
            }
            catch (InvalidShapeException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new InvalidShapeException("error reading WKT: " + e.ToString(), e);
            }
        }

        private class CoordinateSequenceFilterAnonymousHelper : ICoordinateSequenceFilter
        {
            private readonly NtsWKTReaderShapeParser outerInstance;

            public CoordinateSequenceFilterAnonymousHelper(NtsWKTReaderShapeParser outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            private bool changed = false;

            public void Filter(ICoordinateSequence seq, int i)
            {
                double x = seq.GetX(i);
                double y = seq.GetY(i);

                //Note: we don't simply call ctx.normX & normY because
                //  those methods use the precisionModel, but WKTReader already
                //  used the precisionModel. It's be nice to turn that off somehow but alas.
                if (outerInstance.ctx.IsGeo() && outerInstance.ctx.IsNormWrapLongitude())
                {
                    double xNorm = DistanceUtils.NormLonDEG(x);
                    if (x.CompareTo(xNorm) != 0)
                    {//handles NaN
                        changed = true;
                        seq.SetOrdinate(i, Ordinate.X, xNorm);
                    }
                    //          double yNorm = DistanceUtils.normLatDEG(y);
                    //          if (y != yNorm) {
                    //            changed = true;
                    //            seq.setOrdinate(i,CoordinateSequence.Y,yNorm);
                    //          }
                }
                outerInstance.ctx.VerifyX(x);
                outerInstance.ctx.VerifyY(y);
            }

            public bool Done
            {
                get
                {
                    return false;
                }
            }

            public bool GeometryChanged
            {
                get
                {
                    return changed;
                }
            }


        }

        protected virtual void CheckCoordinates(IGeometry geom)
        {
            // note: JTS WKTReader has already normalized coords with the JTS PrecisionModel.
            geom.Apply(new CoordinateSequenceFilterAnonymousHelper(this));
            //        geom.Apply(new CoordinateSequenceFilter() {
            //      boolean changed = false;
            //        @Override
            //      public void filter(CoordinateSequence seq, int i)
            //    {
            //        double x = seq.getX(i);
            //        double y = seq.getY(i);

            //        //Note: we don't simply call ctx.normX & normY because
            //        //  those methods use the precisionModel, but WKTReader already
            //        //  used the precisionModel. It's be nice to turn that off somehow but alas.
            //        if (ctx.isGeo() && ctx.isNormWrapLongitude())
            //        {
            //            double xNorm = DistanceUtils.normLonDEG(x);
            //            if (Double.compare(x, xNorm) != 0)
            //            {//handles NaN
            //                changed = true;
            //                seq.setOrdinate(i, CoordinateSequence.X, xNorm);
            //            }
            //            //          double yNorm = DistanceUtils.normLatDEG(y);
            //            //          if (y != yNorm) {
            //            //            changed = true;
            //            //            seq.setOrdinate(i,CoordinateSequence.Y,yNorm);
            //            //          }
            //        }
            //        ctx.verifyX(x);
            //        ctx.verifyY(y);
            //    }

            //    @Override
            //      public boolean isDone() { return false; }

            //    @Override
            //      public boolean isGeometryChanged() { return changed; }
            //});
        }
    }
}
