using Spatial4n.Core.Context;
using Spatial4n.Core.Exceptions;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Io
{
    /// <summary>
    /// Reads & writes a shape from a given string in the old format.
    /// <list type="bullet">
    ///     <item>
    ///     Point: X Y
    ///     1.23 4.56
    ///     </item>
    ///     <item>
    ///     Rect: XMin YMin XMax YMax
    ///     1.23 4.56 7.87 4.56
    ///     </item>
    ///     <item>
    ///     {CIRCLE} '(' {POINT} {DISTANCE} ')'
    ///     CIRCLE is "CIRCLE" or "Circle" (no other case), and POINT is "X Y" order pair of doubles, or
    ///     "Y,X" (lat,lon) pair of doubles, and DISTANCE is "d=RADIUS" or "distance=RADIUS" where RADIUS
    ///     is a double that is the distance radius in degrees.
    ///     </item>
    /// </list>
    /// </summary>
    [Obsolete]
    public class LegacyShapeReadWriterFormat
    {
        private LegacyShapeReadWriterFormat()
        {
        }

        /**
         * Writes a shape to a String, in a format that can be read by
         * {@link #readShapeOrNull(String, com.spatial4j.core.context.SpatialContext)}.
         * @param shape Not null.
         * @return Not null.
         */
        public static string WriteShape(Shape shape)
        {
            return WriteShape(shape, /*makeNumberFormat(6)*/ "0.000000");
        }

        /** Overloaded to provide a number format. */
        public static string WriteShape(Shape shape, /*NumberFormat nf*/ string numberFormat) // TODO: Add culture overload?
        {
            if (shape is Point)
            {
                Point point = (Point)shape;
                //return nf.format(point.GetX()) + " " + nf.format(point.GetY());
                return point.GetX().ToString(numberFormat) + " " + point.GetY().ToString(numberFormat);
            }
            else if (shape is Rectangle)
            {
                Rectangle rect = (Rectangle)shape;
                //return
                //    nf.format(rect.GetMinX()) + " " +
                //        nf.format(rect.GetMinY()) + " " +
                //        nf.format(rect.GetMaxX()) + " " +
                //        nf.format(rect.GetMaxY());
                return rect.GetMinX().ToString(numberFormat) + " " +
                    rect.GetMinY().ToString(numberFormat) + " " +
                    rect.GetMaxX().ToString(numberFormat) + " " +
                    rect.GetMaxY().ToString(numberFormat);
            }
            else if (shape is Circle)
            {
                Circle c = (Circle)shape;
                //return "Circle(" +
                //    nf.format(c.GetCenter().GetX()) + " " +
                //    nf.format(c.GetCenter().GetY()) + " " +
                //    "d=" + nf.format(c.GetRadius()) +
                //    ")";
                return "Circle(" +
                    c.GetCenter().GetX().ToString(numberFormat) + " " +
                    c.GetCenter().GetY().ToString(numberFormat) + " " +
                    "d=" + c.GetRadius().ToString(numberFormat) +
                    ")";
            }
            return shape.ToString();
        }

        ///**
        // * A convenience method to create a suitable NumberFormat for writing numbers.
        // */
        //public static NumberFormat makeNumberFormat(int fractionDigits)
        //{
        //    NumberFormat nf = NumberFormat.getInstance(Locale.ROOT);//not thread-safe
        //    nf.setGroupingUsed(false);
        //    nf.setMaximumFractionDigits(fractionDigits);
        //    nf.setMinimumFractionDigits(fractionDigits);
        //    return nf;
        //}

        /** Reads the shape specification as defined in the class javadocs. If the first character is
         * a letter but it doesn't complete out "Circle" or "CIRCLE" then this method returns null,
         * offering the caller the opportunity to potentially try additional parsing.
         * If the first character is not a letter then it's assumed to be a point or rectangle. If that
         * doesn't work out then an {@link com.spatial4j.core.exception.InvalidShapeException} is thrown.
         */
        public static Shape ReadShapeOrNull(string str, SpatialContext ctx)
        {
            if (str == null || str.Length == 0)
            {
                throw new InvalidShapeException(str);
            }

            string[] tokens;
            int nextToken = 0;

            if (char.IsLetter(str[0]))
            {
                if (str.StartsWith("Circle(", StringComparison.Ordinal) || str.StartsWith("CIRCLE(", StringComparison.Ordinal))
                {
                    int idx = str.LastIndexOf(')');
                    if (idx > 0)
                    {
                        int circleLength = "Circle(".Length;
                        string body = str.Substring(circleLength, idx - circleLength);
                        //StringTokenizer st = new StringTokenizer(body, " ");
                        //String token = st.nextToken();
                        tokens = body.Split(' ');
                        nextToken = 0;
                        string token = tokens[nextToken];
                        Point pt;
                        if (token.IndexOf(',') != -1)
                        {
                            pt = ReadLatCommaLonPoint(token, ctx);
                        }
                        else
                        {
                            double x = double.Parse(token, CultureInfo.InvariantCulture);
                            double y = double.Parse(token = tokens[++nextToken], CultureInfo.InvariantCulture);
                            pt = ctx.MakePoint(x, y);
                        }
                        double? d = null;

                        //string arg = st.nextToken();
                        string arg = tokens[++nextToken];
                        idx = arg.IndexOf('=');
                        if (idx > 0)
                        {
                            string k = arg.Substring(0, idx - 0);
                            if (k.Equals("d", StringComparison.Ordinal) || k.Equals("distance", StringComparison.Ordinal))
                            {
                                d = double.Parse(arg.Substring(idx + 1), CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                throw new InvalidShapeException("unknown arg: " + k + " :: " + str);
                            }
                        }
                        else
                        {
                            d = double.Parse(arg, CultureInfo.InvariantCulture);
                        }
                        //if (st.hasMoreTokens())
                        if (nextToken < tokens.Length - 1)
                        {
                            throw new InvalidShapeException("Extra arguments: " + tokens[++nextToken] /*st.nextToken()*/ + " :: " + str);
                        }
                        if (d == null)
                        {
                            throw new InvalidShapeException("Missing Distance: " + str);
                        }
                        //NOTE: we are assuming the units of 'd' is the same as that of the spatial context.
                        return ctx.MakeCircle(pt, d.Value);
                    }
                }
                return null;//caller has opportunity to try other parsing
            }

            if (str.IndexOf(',') != -1)
                return ReadLatCommaLonPoint(str, ctx);
            //StringTokenizer st = new StringTokenizer(str, " ");
            tokens = str.Split(' ');
            nextToken = 0;
            //double p0 = Double.parseDouble(st.nextToken());
            //double p1 = Double.parseDouble(st.nextToken());
            double p0 = double.Parse(tokens[nextToken], CultureInfo.InvariantCulture);
            double p1 = double.Parse(tokens[++nextToken], CultureInfo.InvariantCulture);
            //if (st.hasMoreTokens()) 
            if (nextToken < tokens.Length - 1)
            {
                //double p2 = Double.parseDouble(st.nextToken());
                //double p3 = Double.parseDouble(st.nextToken());
                double p2 = double.Parse(tokens[++nextToken], CultureInfo.InvariantCulture);
                double p3 = double.Parse(tokens[++nextToken], CultureInfo.InvariantCulture);
                //if (st.hasMoreTokens())
                if (nextToken < tokens.Length - 1)
                    throw new InvalidShapeException("Only 4 numbers supported (rect) but found more: " + str);
                return ctx.MakeRectangle(p0, p2, p1, p3);
            }
            return ctx.MakePoint(p0, p1);
        }

        /** Reads geospatial latitude then a comma then longitude. */
        private static Point ReadLatCommaLonPoint(string value, SpatialContext ctx)
        {
            double[]
            latLon = ParseUtils.ParseLatitudeLongitude(value);
            return ctx.MakePoint(latLon[1], latLon[0]);
        }
    }
}
