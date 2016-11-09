using GeoAPI.IO;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Spatial4n.Core.Io
{
    public class WktShapeParser
    {
        //TODO support SRID:  "SRID=4326;pointPOINT(1,2)

        //TODO should reference proposed ShapeFactory instead of ctx, which is a point of indirection that
        // might optionally do data validation & normalization
        protected readonly SpatialContext ctx;

        /** This constructor is required by {@link com.spatial4j.core.context.SpatialContextFactory#makeWktShapeParser(com.spatial4j.core.context.SpatialContext)}. */
        public WktShapeParser(SpatialContext ctx, SpatialContextFactory factory)
        {
            this.ctx = ctx;
        }

        public virtual SpatialContext Ctx
        {
            get { return ctx; }
        }

        /**
         * Parses the wktString, returning the defined Shape.
         *
         * @return Non-null Shape defined in the String
         * @throws ParseException Thrown if there is an error in the Shape definition
         */
        public virtual Shape Parse(string wktString)
        {
            Shape shape = ParseIfSupported(wktString);//sets rawString & offset
            if (shape != null)
                return shape;
            string shortenedString = (wktString.Length <= 128 ? wktString : wktString.Substring(0, (128 - 3) - 0) + "...");
            throw new ParseException("Unknown Shape definition [" + shortenedString + "]"/*, 0*/);
        }

        /**
         * Parses the wktString, returning the defined Shape. If it can't because the
         * shape name is unknown or an empty or blank string was passed, then it returns null.
         * If the WKT starts with a supported shape but contains an inner unsupported shape then
         * it will result in a {@link ParseException}.
         *
         * @param wktString non-null, can be empty or have surrounding whitespace
         * @return Shape, null if unknown / unsupported shape.
         * @throws ParseException Thrown if there is an error in the Shape definition
         */
        public virtual Shape ParseIfSupported(string wktString)
        {
            State state = NewState(wktString);
            state.NextIfWhitespace();//leading
            if (state.Eof)
                return null;
            //shape types must start with a letter
            if (!char.IsLetter(state.rawString[state.offset]))
                return null;
            string shapeType = state.NextWord();
            Shape result = null;
            try
            {
                result = ParseShapeByType(state, shapeType);
            }
            catch (ParseException e)
            {
                throw e;
            }
            catch (Exception e)
            {//most likely InvalidShapeException
                ParseException pe = new ParseException(e.ToString()/*, state.offset*/);
                //pe.initCause(e);
                throw pe;
            }
            if (result != null && !state.Eof)
                throw new ParseException("end of shape expected"/*, state.offset*/);
            return result;
        }

        /** (internal) Creates a new State with the given String. It's only called by
         * {@link #parseIfSupported(String)}. This is an extension point for subclassing. */
        protected virtual State NewState(string wktString)
        {
            //NOTE: if we wanted to re-use old States to reduce object allocation, we might do that
            // here. But in the scheme of things, it doesn't seem worth the bother as it complicates the
            // thread-safety story of the API for too little of a gain.
            return new State(wktString);
        }

        /**
         * (internal) Parses the remainder of a shape definition following the shape's name
         * given as {@code shapeType} already consumed via
         * {@link State#nextWord()}. If
         * it's able to parse the shape, {@link WktShapeParser.State#offset}
         * should be advanced beyond
         * it (e.g. to the ',' or ')' or EOF in general). The default implementation
         * checks the name against some predefined names and calls corresponding
         * parse methods to handle the rest. Overriding this method is an
         * excellent extension point for additional shape types. Or, use this class by delegation to this
         * method.
         * <p />
         * When writing a parse method that reacts to a specific shape type, remember to handle the
         * dimension and EMPTY token via
         * {@link com.spatial4j.core.io.WktShapeParser.State#nextIfEmptyAndSkipZM()}.
         *
         * @param state
         * @param shapeType Non-Null string; could have mixed case. The first character is a letter.
         * @return The shape or null if not supported / unknown.
         */
        protected virtual Shape ParseShapeByType(State state, string shapeType)
        {
            Debug.Assert(char.IsLetter(shapeType[0]), "Shape must start with letter: " + shapeType);

            if (shapeType.Equals("POINT", StringComparison.OrdinalIgnoreCase))
            {
                return ParsePointShape(state);
            }
            else if (shapeType.Equals("MULTIPOINT", StringComparison.OrdinalIgnoreCase))
            {
                return ParseMultiPointShape(state);
            }
            else if (shapeType.Equals("ENVELOPE", StringComparison.OrdinalIgnoreCase))
            {
                return ParseEnvelopeShape(state);
            }
            else if (shapeType.Equals("GEOMETRYCOLLECTION", StringComparison.OrdinalIgnoreCase))
            {
                return ParseGeometryCollectionShape(state);
            }
            else if (shapeType.Equals("LINESTRING", StringComparison.OrdinalIgnoreCase))
            {
                return ParseLineStringShape(state);
            }
            else if (shapeType.Equals("MULTILINESTRING", StringComparison.OrdinalIgnoreCase))
            {
                return ParseMultiLineStringShape(state);
            }
            //extension
            if (shapeType.Equals("BUFFER", StringComparison.OrdinalIgnoreCase))
            {
                return ParseBufferShape(state);
            }

            // HEY! Update class Javadocs if add more shapes
            return null;
        }

        /**
         * Parses the BUFFER operation applied to a parsed shape.
         * <pre>
         *   '(' shape ',' number ')'
         * </pre>
         * Whereas 'number' is the distance to buffer the shape by.
         */
        protected virtual Shape ParseBufferShape(State state)
        {
            state.NextExpect('(');
            Shape shape = Shape(state);
            state.NextExpect(',');
            double distance = NormDist(state.NextDouble());
            state.NextExpect(')');
            return shape.GetBuffered(distance, ctx);
        }

        /** Called to normalize a value that isn't X or Y. X & Y or normalized via
         * {@link com.spatial4j.core.context.SpatialContext#normX(double)} & normY.
         */
        protected virtual double NormDist(double v)
        {//TODO should this be added to ctx?
            return v;
        }

        /**
         * Parses a POINT shape from the raw string.
         * <pre>
         *   '(' coordinate ')'
         * </pre>
         *
         * @see #point(WktShapeParser.State)
         */
        protected virtual Shape ParsePointShape(State state)
        {
            if (state.NextIfEmptyAndSkipZM())
                return ctx.MakePoint(double.NaN, double.NaN);
            state.NextExpect('(');
            Point coordinate = Point(state);
            state.NextExpect(')');
            return coordinate;
        }

        /**
         * Parses a MULTIPOINT shape from the raw string -- a collection of points.
         * <pre>
         *   '(' coordinate (',' coordinate )* ')'
         * </pre>
         * Furthermore, coordinate can optionally be wrapped in parenthesis.
         *
         * @see #point(WktShapeParser.State)
         */
        protected virtual Shape ParseMultiPointShape(State state)
        {
            if (state.NextIfEmptyAndSkipZM())
                return ctx.MakeCollection(new List<Point>());
            List<Point> shapes = new List<Point>();
            state.NextExpect('(');
            do
            {
                bool openParen = state.NextIf('(');
                Point coordinate = Point(state);
                if (openParen)
                    state.NextExpect(')');
                shapes.Add(coordinate);
            } while (state.NextIf(','));
            state.NextExpect(')');
            return ctx.MakeCollection(shapes);
        }

        /**
         * Parses an ENVELOPE (aka Rectangle) shape from the raw string. The values are normalized.
         * <p />
         * Source: OGC "Catalogue Services Specification", the "CQL" (Common Query Language) sub-spec.
         * <em>Note the inconsistent order of the min & max values between x & y!</em>
         * <pre>
         *   '(' x1 ',' x2 ',' y2 ',' y1 ')'
         * </pre>
         */
        protected virtual Shape ParseEnvelopeShape(State state)
        {
            //FYI no dimension or EMPTY
            state.NextExpect('(');
            double x1 = state.NextDouble();
            state.NextExpect(',');
            double x2 = state.NextDouble();
            state.NextExpect(',');
            double y2 = state.NextDouble();
            state.NextExpect(',');
            double y1 = state.NextDouble();
            state.NextExpect(')');
            return ctx.MakeRectangle(ctx.NormX(x1), ctx.NormX(x2), ctx.NormY(y1), ctx.NormY(y2));
        }

        /**
         * Parses a LINESTRING shape from the raw string -- an ordered sequence of points.
         * <pre>
         *   coordinateSequence
         * </pre>
         *
         * @see #pointList(WktShapeParser.State)
         */
        protected virtual Shape ParseLineStringShape(State state)
        {
            if (state.NextIfEmptyAndSkipZM())
                return ctx.MakeLineString(new List<Point>());
            List<Point> points = PointList(state);
            return ctx.MakeLineString(points);
        }

        /**
         * Parses a MULTILINESTRING shape from the raw string -- a collection of line strings.
         * <pre>
         *   '(' coordinateSequence (',' coordinateSequence )* ')'
         * </pre>
         *
         * @see #parseLineStringShape(com.spatial4j.core.io.WktShapeParser.State)
         */
        protected virtual Shape ParseMultiLineStringShape(State state)
        {
            if (state.NextIfEmptyAndSkipZM())
                return ctx.MakeCollection(new List<Shape>());
            List<Shape> shapes = new List<Shape>();
            state.NextExpect('(');
            do
            {
                shapes.Add(ParseLineStringShape(state));
            } while (state.NextIf(','));
            state.NextExpect(')');
            return ctx.MakeCollection(shapes);
        }

        /**
         * Parses a GEOMETRYCOLLECTION shape from the raw string.
         * <pre>
         *   '(' shape (',' shape )* ')'
         * </pre>
         */
        protected virtual Shape ParseGeometryCollectionShape(State state)
        {
            if (state.NextIfEmptyAndSkipZM())
                return ctx.MakeCollection(new List<Shape>());
            List<Shape> shapes = new List<Shape>();
            state.NextExpect('(');
            do
            {
                shapes.Add(Shape(state));
            } while (state.NextIf(','));
            state.NextExpect(')');
            return ctx.MakeCollection(shapes);
        }

        /** Reads a shape from the current position, starting with the name of the shape. It
         * calls {@link #parseShapeByType(com.spatial4j.core.io.WktShapeParser.State, String)}
         * and throws an exception if the shape wasn't supported. */
        protected virtual Shape Shape(State state)
        {
            string type = state.NextWord();
            Shape shape = ParseShapeByType(state, type);
            if (shape == null)
                throw new ParseException("Shape of type " + type + " is unknown"/*, state.offset*/);
            return shape;
        }

        /**
         * Reads a list of Points (AKA CoordinateSequence) from the current position.
         * <pre>
         *   '(' coordinate (',' coordinate )* ')'
         * </pre>
         *
         * @see #point(WktShapeParser.State)
         */
        protected virtual List<Point> PointList(State state)
        {
            List<Point> sequence = new List<Point>();
            state.NextExpect('(');
            do
            {
                sequence.Add(Point(state));
            } while (state.NextIf(','));
            state.NextExpect(')');
            return sequence;
        }

        /**
         * Reads a raw Point (AKA Coordinate) from the current position. Only the first 2 numbers are
         * used.  The values are normalized.
         * <pre>
         *   number number number*
         * </pre>
         */
        protected virtual Point Point(State state)
        {
            double x = state.NextDouble();
            double y = state.NextDouble();
            state.SkipNextDoubles();
            return ctx.MakePoint(ctx.NormX(x), ctx.NormY(y));
        }

        /** The parse state. */
        public class State
        {
            /** Set in {@link #parseIfSupported(String)}. */
            public string rawString;
            /** Offset of the next char in {@link #rawString} to be read. */
            public int offset;
            /** Dimensionality specifier (e.g. 'Z', or 'M') following a shape type name. */
            public string dimension;

            public State(string rawString)
            {
                this.rawString = rawString;
            }

            public virtual SpatialContext Ctx
            {
                get { return ctx; }
            }

            public virtual WktShapeParser Parser
            {
                get { return WktShapeParser.this; }
            }

            /**
             * Reads the word starting at the current character position. The word
             * terminates once {@link Character#isJavaIdentifierPart(char)} returns false (or EOF).
             * {@link #offset} is advanced past whitespace.
             *
             * @return Non-null non-empty String.
             */
            public virtual string NextWord()
            {
                int startOffset = offset;
                while (offset < rawString.Length &&
                    Character.isJavaIdentifierPart(rawString[offset]))
                {
                    offset++;
                }
                if (startOffset == offset)
                    throw new ParseException("Word expected"/*, startOffset*/);
                string result = rawString.Substring(startOffset, offset - startOffset);
                NextIfWhitespace();
                return result;
            }

            /**
             * Skips over a dimensionality token (e.g. 'Z' or 'M') if found, storing in
             * {@link #dimension}, and then looks for EMPTY, consuming that and whitespace.
             * <pre>
             *   dimensionToken? 'EMPTY'?
             * </pre>
             * @return True if EMPTY was found.
             */
            public virtual bool NextIfEmptyAndSkipZM()
            {
                if (Eof)
                    return false;
                char c = rawString[offset];
                if (c == '(' || !Character.isJavaIdentifierPart(c))
                    return false;
                string word = NextWord();
                if (word.Equals("EMPTY", StringComparison.OrdinalIgnoreCase))
                    return true;
                //we figure this word is Z or ZM or some other dimensionality signifier. We skip it.
                this.dimension = word;

                if (Eof)
                    return false;
                c = rawString[offset];
                if (c == '(' || !Character.isJavaIdentifierPart(c))
                    return false;
                word = NextWord();
                if (word.Equals("EMPTY", StringComparison.OrdinalIgnoreCase))
                    return true;
                throw new ParseException("Expected EMPTY because found dimension; but got [" + word + "]"/*,
          offset*/);
            }

            /**
             * Reads in a double from the String. Parses digits with an optional decimal, sign, or exponent.
             * NaN and Infinity are not supported.
             * {@link #offset} is advanced past whitespace.
             *
             * @return Double value
             */
            public virtual double NextDouble()
            {
                int startOffset = offset;
                SkipDouble();
                if (startOffset == offset)
                    throw new ParseException("Expected a number"/*, offset*/);
                double result;
                try
                {
                    result = double.Parse(rawString.Substring(startOffset, offset - startOffset));
                }
                catch (Exception e)
                {
                    throw new ParseException(e.ToString()/*, offset*/);
                }
                NextIfWhitespace();
                return result;
            }

            /** Advances offset forward until it points to a character that isn't part of a number. */
            public virtual void SkipDouble()
            {
                int startOffset = offset;
                for (; offset < rawString.Length; offset++)
                {
                    char c = rawString[offset];
                    if (!(char.IsDigit(c) || c == '.' || c == '-' || c == '+'))
                    {
                        //'e' is okay as long as it isn't first
                        if (offset != startOffset && (c == 'e' || c == 'E'))
                            continue;
                        break;
                    }
                }
            }

            /** Advances past as many doubles as there are, with intervening whitespace. */
            public virtual void SkipNextDoubles()
            {
                while (!Eof)
                {
                    int startOffset = offset;
                    SkipDouble();
                    if (startOffset == offset)
                        return;
                    NextIfWhitespace();
                }
            }

            /**
             * Verifies that the current character is of the expected value.
             * If the character is the expected value, then it is consumed and
             * {@link #offset} is advanced past whitespace.
             *
             * @param expected The expected char.
             */
            public virtual void NextExpect(char expected)
            {
                if (Eof)
                    throw new ParseException("Expected [" + expected + "] found EOF"/*, offset*/);
                char c = rawString[offset];
                if (c != expected)
                    throw new ParseException("Expected [" + expected + "] found [" + c + "]"/*, offset*/);
                offset++;
                NextIfWhitespace();
            }

            /** If the string is consumed, i.e. at end-of-file. */
            public bool Eof
            {
                get { return offset >= rawString.Length; }
            }

            /**
             * If the current character is {@code expected}, then offset is advanced after it and any
             * subsequent whitespace. Otherwise, false is returned.
             *
             * @param expected The expected char
             * @return true if consumed
             */
            public virtual bool NextIf(char expected)
            {
                if (!Eof && rawString[offset] == expected)
                {
                    offset++;
                    NextIfWhitespace();
                    return true;
                }
                return false;
            }

            /**
             * Moves offset to next non-whitespace character. Doesn't move if the offset is already at
             * non-whitespace. <em>There is very little reason for subclasses to call this because
             * most other parsing methods call it.</em>
             */
            public virtual void NextIfWhitespace()
            {
                for (; offset < rawString.Length; offset++)
                {
                    if (!char.IsWhiteSpace(rawString[offset]))
                    {
                        return;
                    }
                }
            }

            /**
             * Returns the next chunk of text till the next ',' or ')' (non-inclusive)
             * or EOF. If a '(' is encountered, then it looks past its matching ')',
             * taking care to handle nested matching parenthesis too. It's designed to be
             * of use to subclasses that wish to get the entire subshape at the current
             * position as a string so that it might be passed to other software that
             * will parse it.
             * <p/>
             * Example:
             * <pre>
             *   OUTER(INNER(3, 5))
             * </pre>
             * If this is called when offset is at the first character, then it will
             * return this whole string.  If called at the "I" then it will return
             * "INNER(3, 5)".  If called at "3", then it will return "3".  In all cases,
             * offset will be positioned at the next position following the returned
             * substring.
             *
             * @return non-null substring.
             */
            public virtual string NextSubShapeString()
            {
                int startOffset = offset;
                int parenStack = 0;//how many parenthesis levels are we in?
                for (; offset < rawString.Length; offset++)
                {
                    char c = rawString[offset];
                    if (c == ',')
                    {
                        if (parenStack == 0)
                            break;
                    }
                    else if (c == ')')
                    {
                        if (parenStack == 0)
                            break;
                        parenStack--;
                    }
                    else if (c == '(')
                    {
                        parenStack++;
                    }
                }
                if (parenStack != 0)
                    throw new ParseException("Unbalanced parenthesis"/*, startOffset*/);
                return rawString.Substring(startOffset, offset - startOffset);
            }

        }//class State
    }
}
