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
        //Note: Historically, the code here originated from the defunct NtsShapeReadWriter.

        public NtsWKTReaderShapeParser(NtsSpatialContext ctx, NtsSpatialContextFactory factory)
            : base(ctx, factory)
        {
        }

        public override IShape ParseIfSupported(string wktString)
        {
            return ParseIfSupported(wktString, new WKTReader(m_ctx.GeometryFactory));
        }

        /**
         * Reads WKT from the {@code str} via NTS's {@link com.vividsolutions.jts.io.WKTReader}.
         * @param str
         * @param reader <pre>new WKTReader(ctx.getGeometryFactory()))</pre>
         * @return Non-Null
         */
        protected IShape ParseIfSupported(string str, WKTReader reader)
        {
            try
            {
                IGeometry geom = reader.Read(str);

                //Normalizes & verifies coordinates
                CheckCoordinates(geom);

                if (geom is NetTopologySuite.Geometries.Point)
                {
                    NetTopologySuite.Geometries.Point ptGeom = (NetTopologySuite.Geometries.Point)geom;
                    if (m_ctx.UseNtsPoint)
                        return new NtsPoint(ptGeom, m_ctx);
                    else
                        return m_ctx.MakePoint(ptGeom.X, ptGeom.Y);
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
                if (outerInstance.m_ctx.IsGeo && outerInstance.m_ctx.IsNormWrapLongitude)
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
                outerInstance.m_ctx.VerifyX(x);
                outerInstance.m_ctx.VerifyY(y);
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
            // note: NTS WKTReader has already normalized coords with the JTS PrecisionModel.
            geom.Apply(new CoordinateSequenceFilterAnonymousHelper(this));
        }
    }
}
