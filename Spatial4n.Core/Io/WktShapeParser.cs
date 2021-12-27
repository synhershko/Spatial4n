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
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Spatial4n.Core.IO
{
    /// <summary>
    /// An extensible parser for <a href="http://en.wikipedia.org/wiki/Well-known_text">
    /// Well Known Text (WKT)</a>.
    /// The shapes supported by this class are:
    /// <list type="bullet">
    ///     <item>POINT</item>
    ///     <item>MULTIPOINT</item>
    ///     <item>ENVELOPE (strictly isn't WKT but is defined by OGC's <a href="http://docs.geoserver.org/stable/en/user/tutorials/cql/cql_tutorial.html"> Common Query Language(CQL)</a>)</item>
    ///     <item>LINESTRING</item>
    ///     <item>MULTILINESTRING</item>
    ///     <item>GEOMETRYCOLLECTION</item>
    ///     <item>BUFFER (non-standard Spatial4n operation)</item>
    /// </list>
    /// 'EMPTY' is supported. Specifying 'Z', 'M', or any other dimensionality in the WKT is effectively
    /// ignored.  Thus, you can specify any number of numbers in the coordinate points but only the first
    /// two take effect.  The docs for the <c>parse___Shape</c> methods further describe these
    /// shapes, or you
    /// <para>
    /// Most users of this class will call just one method: <see cref="Parse(string)"/>, or
    /// <see cref="ParseIfSupported(string)"/> to not fail if it isn't parse-able.
    /// </para>
    /// <para>
    /// To support more shapes, extend this class and override
    /// <see cref="ParseShapeByType(State, string)"/>. It's also possible to delegate to
    /// a WKTParser by also delegating <see cref="NewState(string)"/>.
    /// </para>
    /// <para>
    /// Note, instances of this base class are threadsafe.
    /// </para>
    /// </summary>
    public class WktShapeParser
    {
        //TODO support SRID:  "SRID=4326;pointPOINT(1,2)

        //TODO should reference proposed ShapeFactory instead of ctx, which is a point of indirection that
        // might optionally do data validation & normalization
        protected readonly SpatialContext m_ctx;

        /// <summary>
        /// This constructor is required by <see cref="SpatialContextFactory.MakeWktShapeParser(SpatialContext)"/>.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="factory"></param>
        public WktShapeParser(SpatialContext ctx, SpatialContextFactory factory)
        {
            this.m_ctx = ctx;
        }

        public virtual SpatialContext Ctx
        {
            get { return m_ctx; }
        }

        /// <summary>
        /// Parses the wktString, returning the defined <see cref="IShape"/>.
        /// </summary>
        /// <param name="wktString"></param>
        /// <returns>Non-null <see cref="IShape"/> defined in the string</returns>
        /// <exception cref="ParseException">Thrown if there is an error in the <see cref="IShape"/> definition</exception>
        public virtual IShape Parse(string wktString)
        {
            IShape shape = ParseIfSupported(wktString);//sets rawString & offset
            if (shape != null)
                return shape;
            string shortenedString = (wktString.Length <= 128 ? wktString : wktString.Substring(0, (128 - 3) - 0) + "...");
            throw new ParseException("Unknown Shape definition [" + shortenedString + "]", 0);
        }

        /// <summary>
        /// Parses the wktString, returning the defined <see cref="IShape"/>. If it can't because the
        /// shape name is unknown or an empty or blank string was passed, then it returns null.
        /// If the WKT starts with a supported shape but contains an inner unsupported shape then
        /// it will result in a <see cref="ParseException"/>.
        /// </summary>
        /// <param name="wktString">non-null, can be empty or have surrounding whitespace</param>
        /// <returns><see cref="IShape"/>, null if unknown / unsupported shape.</returns>
        /// <exception cref="ParseException">Thrown if there is an error in the <see cref="IShape"/> definition</exception>
        public virtual IShape ParseIfSupported(string wktString)
        {
            State state = NewState(wktString);
            state.NextIfWhitespace();//leading
            if (state.Eof)
                return null;
            //shape types must start with a letter
            if (!char.IsLetter(state.rawString[state.offset]))
                return null;
            string shapeType = state.NextWord();
            IShape result = null;
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
                ParseException pe = new ParseException(e.ToString(), state.offset);
                //pe.initCause(e);
                throw pe;
            }
            if (result != null && !state.Eof)
                throw new ParseException("end of shape expected", state.offset);
            return result;
        }

        /// <summary>
        /// (internal) Creates a new State with the given String. It's only called by
        /// <see cref="ParseIfSupported(string)"/>. This is an extension point for subclassing.
        /// </summary>
        protected internal virtual State NewState(string wktString)
        {
            //NOTE: if we wanted to re-use old States to reduce object allocation, we might do that
            // here. But in the scheme of things, it doesn't seem worth the bother as it complicates the
            // thread-safety story of the API for too little of a gain.
            return new State(this, wktString);
        }

        /// <summary>
        /// (internal) Parses the remainder of a shape definition following the shape's name
        /// given as <paramref name="shapeType"/> already consumed via
        /// <see cref="State.NextWord()"/>. If
        /// it's able to parse the shape, <see cref="State.offset"/> 
        /// should be advanced beyond
        /// it (e.g. to the ',' or ')' or EOF in general). The default implementation
        /// checks the name against some predefined names and calls corresponding
        /// parse methods to handle the rest. Overriding this method is an
        /// excellent extension point for additional shape types. Or, use this class by delegation to this
        /// method.
        /// <para>
        /// When writing a parse method that reacts to a specific shape type, remember to handle the
        /// dimension and EMPTY token via <see cref="State.NextIfEmptyAndSkipZM()"/>.
        /// </para>
        /// </summary>
        /// <param name="state"></param>
        /// <param name="shapeType">Non-Null string; could have mixed case. The first character is a letter.</param>
        /// <returns>The shape or null if not supported / unknown.</returns>
        protected virtual IShape ParseShapeByType(State state, string shapeType)
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

        /// <summary>
        /// Parses the BUFFER operation applied to a parsed shape.
        /// <code>
        ///   '(' shape ',' number ')'
        /// </code>
        /// Whereas 'number' is the distance to buffer the shape by.
        /// </summary>
        protected virtual IShape ParseBufferShape(State state)
        {
            state.NextExpect('(');
            IShape shape = Shape(state);
            state.NextExpect(',');
            double distance = NormDist(state.NextDouble());
            state.NextExpect(')');
            return shape.GetBuffered(distance, m_ctx);
        }

        /// <summary>
        /// Called to normalize a value that isn't X or Y. X &amp; Y or normalized via
        /// <see cref="SpatialContext.NormX(double)"/> &amp; <see cref="SpatialContext.NormY(double)"/>.
        /// </summary>
        protected virtual double NormDist(double v)
        {//TODO should this be added to ctx?
            return v;
        }

        /// <summary>
        /// Parses a POINT shape from the raw string.
        /// <code>
        ///   '(' coordinate ')'
        /// </code>
        /// </summary>
        /// <seealso cref="Point(State)"/>
        protected virtual IShape ParsePointShape(State state)
        {
            if (state.NextIfEmptyAndSkipZM())
                return m_ctx.MakePoint(double.NaN, double.NaN);
            state.NextExpect('(');
            IPoint coordinate = Point(state);
            state.NextExpect(')');
            return coordinate;
        }

        /// <summary>
        /// Parses a MULTIPOINT shape from the raw string -- a collection of points.
        /// <code>
        ///   '(' coordinate (',' coordinate )* ')'
        /// </code>
        /// Furthermore, coordinate can optionally be wrapped in parenthesis.
        /// </summary>
        /// <seealso cref="Point(State)"/>
        protected virtual IShape ParseMultiPointShape(State state)
        {
            if (state.NextIfEmptyAndSkipZM())
                return m_ctx.MakeCollection(new List<IShape>());
            IList<IShape> shapes = new List<IShape>();
            state.NextExpect('(');
            do
            {
                bool openParen = state.NextIf('(');
                IPoint coordinate = Point(state);
                if (openParen)
                    state.NextExpect(')');
                shapes.Add(coordinate);
            } while (state.NextIf(','));
            state.NextExpect(')');
            return m_ctx.MakeCollection(shapes);
        }

        /// <summary>
        /// Parses an ENVELOPE (aka Rectangle) shape from the raw string. The values are normalized.
        /// <para>
        /// Source: OGC "Catalogue Services Specification", the "CQL" (Common Query Language) sub-spec.
        /// <c>Note the inconsistent order of the min &amp; max values between x &amp; y!</c>
        /// <code>
        ///   '(' x1 ',' x2 ',' y2 ',' y1 ')'
        /// </code>
        /// </para>
        /// </summary>
        protected virtual IShape ParseEnvelopeShape(State state)
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
            return m_ctx.MakeRectangle(m_ctx.NormX(x1), m_ctx.NormX(x2), m_ctx.NormY(y1), m_ctx.NormY(y2));
        }

        /// <summary>
        /// Parses a LINESTRING shape from the raw string -- an ordered sequence of points.
        /// <code>
        ///   coordinateSequence
        /// </code>
        /// </summary>
        /// <seealso cref="PointList(State)"/>
        protected virtual IShape ParseLineStringShape(State state)
        {
            if (state.NextIfEmptyAndSkipZM())
                return m_ctx.MakeLineString(new List<IPoint>());
            IList<IPoint> points = PointList(state);
            return m_ctx.MakeLineString(points);
        }

        /// <summary>
        /// Parses a MULTILINESTRING shape from the raw string -- a collection of line strings.
        /// <code>
        ///   '(' coordinateSequence (',' coordinateSequence )* ')'
        /// </code>
        /// </summary>
        /// <seealso cref="ParseLineStringShape(State)"/>
        protected virtual IShape ParseMultiLineStringShape(State state)
        {
            if (state.NextIfEmptyAndSkipZM())
                return m_ctx.MakeCollection(new List<IShape>());
            IList<IShape> shapes = new List<IShape>();
            state.NextExpect('(');
            do
            {
                shapes.Add(ParseLineStringShape(state));
            } while (state.NextIf(','));
            state.NextExpect(')');
            return m_ctx.MakeCollection(shapes);
        }

        /// <summary>
        /// Parses a GEOMETRYCOLLECTION shape from the raw string.
        /// <code>
        ///   '(' shape (',' shape )* ')'
        /// </code>
        /// </summary>
        protected virtual IShape ParseGeometryCollectionShape(State state)
        {
            if (state.NextIfEmptyAndSkipZM())
                return m_ctx.MakeCollection(new List<IShape>());
            IList<IShape> shapes = new List<IShape>();
            state.NextExpect('(');
            do
            {
                shapes.Add(Shape(state));
            } while (state.NextIf(','));
            state.NextExpect(')');
            return m_ctx.MakeCollection(shapes);
        }

        /// <summary>
        /// Reads a shape from the current position, starting with the name of the shape. It
        /// calls <seealso cref="ParseShapeByType(State, string)"/>
        /// and throws an exception if the shape wasn't supported.
        /// </summary>
        protected virtual IShape Shape(State state)
        {
            string type = state.NextWord();
            IShape shape = ParseShapeByType(state, type);
            if (shape == null)
                throw new ParseException("Shape of type " + type + " is unknown", state.offset);
            return shape;
        }

        /// <summary>
        /// Reads a list of Points (AKA CoordinateSequence) from the current position.
        /// <code>
        ///   '(' coordinate (',' coordinate )* ')'
        /// </code>
        /// </summary>
        /// <seealso cref="Point(State)"/>
        protected virtual IList<IPoint> PointList(State state)
        {
            IList<IPoint> sequence = new List<IPoint>();
            state.NextExpect('(');
            do
            {
                sequence.Add(Point(state));
            } while (state.NextIf(','));
            state.NextExpect(')');
            return sequence;
        }

        /// <summary>
        /// Reads a raw Point (AKA Coordinate) from the current position. Only the first 2 numbers are
        /// used.  The values are normalized.
        /// <code>
        ///   number number number*
        /// </code>
        /// </summary>
        protected virtual IPoint Point(State state)
        {
            double x = state.NextDouble();
            double y = state.NextDouble();
            state.SkipNextDoubles();
            return m_ctx.MakePoint(m_ctx.NormX(x), m_ctx.NormY(y));
        }

        /// <summary>
        /// The parse state.
        /// </summary>
        public class State
        {
            private readonly WktShapeParser outerInstance;

            /// <summary>Set in <see cref="ParseIfSupported(string)"/>.</summary>
            public string rawString;
            /// <summary>Offset of the next char in <see cref="rawString"/> to be read. </summary>
            public int offset;
            /// <summary>Dimensionality specifier (e.g. 'Z', or 'M') following a shape type name.</summary>
            public string dimension;

            public State(WktShapeParser outerInstance, string rawString)
            {
                if (outerInstance == null)
                    throw new ArgumentNullException("outerInstance");
                this.outerInstance = outerInstance;
                this.rawString = rawString;
            }

            public virtual SpatialContext Ctx
            {
                get { return outerInstance.m_ctx; }
            }

            public virtual WktShapeParser Parser
            {
                get { return outerInstance; }
            }

            /// <summary>
            /// Reads the word starting at the current character position. The word
            /// terminates once <see cref="IsIdentifierPartCharacter(char)"/> (which is identical to Java's <c>Character.isJavaIdentifierPart(char)</c>) returns false (or EOF).
            /// <see cref="offset"/> is advanced past whitespace.
            /// </summary>
            /// <returns>Non-null non-empty string.</returns>
            public virtual string NextWord()
            {
                int startOffset = offset;
                while (offset < rawString.Length &&
                    IsIdentifierPartCharacter(rawString[offset]))
                {
                    offset++;
                }
                if (startOffset == offset)
                    throw new ParseException("Word expected", startOffset);
                string result = rawString.Substring(startOffset, offset - startOffset);
                NextIfWhitespace();
                return result;
            }

            /// <summary>
            /// Skips over a dimensionality token (e.g. 'Z' or 'M') if found, storing in
            /// <see cref="dimension"/>, and then looks for EMPTY, consuming that and whitespace.
            /// <code>
            ///   dimensionToken? 'EMPTY'?
            /// </code>
            /// </summary>
            /// <returns>True if EMPTY was found.</returns>
            public virtual bool NextIfEmptyAndSkipZM()
            {
                if (Eof)
                    return false;
                char c = rawString[offset];
                if (c == '(' || !IsIdentifierPartCharacter(rawString[offset]))
                    return false;
                string word = NextWord();
                if (word.Equals("EMPTY", StringComparison.OrdinalIgnoreCase))
                    return true;
                //we figure this word is Z or ZM or some other dimensionality signifier. We skip it.
                this.dimension = word;

                if (Eof)
                    return false;
                c = rawString[offset];
                if (c == '(' || !IsIdentifierPartCharacter(rawString[offset]))
                    return false;
                word = NextWord();
                if (word.Equals("EMPTY", StringComparison.OrdinalIgnoreCase))
                    return true;
                throw new ParseException("Expected EMPTY because found dimension; but got [" + word + "]",
                    offset);
            }

            /// <summary>
            /// Reads in a double from the string. Parses digits with an optional decimal, sign, or exponent.
            /// NaN and Infinity are not supported.
            /// <see cref="offset"/> is advanced past whitespace.
            /// </summary>
            /// <returns><see cref="double"/> value</returns>
            public virtual double NextDouble()
            {
                int startOffset = offset;
                SkipDouble();
                if (startOffset == offset)
                    throw new ParseException("Expected a number", offset);
                double result;
                try
                {
                    result = double.Parse(rawString.Substring(startOffset, offset - startOffset), CultureInfo.InvariantCulture);
                }
                catch (RuntimeException e)
                {
                    throw new ParseException(e.ToString(), offset);
                }
                NextIfWhitespace();
                return result;
            }

            /// <summary>
            /// Advances offset forward until it points to a character that isn't part of a number.
            /// </summary>
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

            /// <summary>
            /// Advances past as many doubles as there are, with intervening whitespace.
            /// </summary>
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

            /// <summary>
            /// Verifies that the current character is of the expected value.
            /// If the character is the expected value, then it is consumed and
            /// <see cref="offset"/> is advanced past whitespace.
            /// </summary>
            /// <param name="expected">The expected char.</param>
            public virtual void NextExpect(char expected)
            {
                if (Eof)
                    throw new ParseException("Expected [" + expected + "] found EOF", offset);
                char c = rawString[offset];
                if (c != expected)
                    throw new ParseException("Expected [" + expected + "] found [" + c + "]", offset);
                offset++;
                NextIfWhitespace();
            }

            /// <summary>
            /// If the string is consumed, i.e. at end-of-file.
            /// </summary>
            public bool Eof
            {
                get { return offset >= rawString.Length; }
            }

            /// <summary>
            /// If the current character is <paramref name="expected"/>, then offset is advanced after it and any
            /// subsequent whitespace. Otherwise, false is returned.
            /// </summary>
            /// <param name="expected">The expected char</param>
            /// <returns>true if consumed</returns>
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

            /// <summary>
            /// Moves offset to next non-whitespace character. Doesn't move if the offset is already at
            /// non-whitespace. <c>There is very little reason for subclasses to call this because
            /// most other parsing methods call it.</c>
            /// </summary>
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

            /// <summary>
            /// Returns the next chunk of text till the next ',' or ')' (non-inclusive)
            /// or EOF. If a '(' is encountered, then it looks past its matching ')',
            /// taking care to handle nested matching parenthesis too. It's designed to be
            /// of use to subclasses that wish to get the entire subshape at the current
            /// position as a string so that it might be passed to other software that
            /// will parse it.
            /// <para>
            /// Example:
            /// <code>
            ///   OUTER(INNER(3, 5))
            /// </code>
            /// If this is called when offset is at the first character, then it will
            /// return this whole string.  If called at the "I" then it will return
            /// "INNER(3, 5)".  If called at "3", then it will return "3".  In all cases,
            /// offset will be positioned at the next position following the returned
            /// substring.
            /// </para>
            /// </summary>
            /// <returns>non-null substring.</returns>
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
                    throw new ParseException("Unbalanced parenthesis", startOffset);
                return rawString.Substring(startOffset, offset - startOffset);
            }

            // Was Character.isJavaIdentifierPart(char) in Java
            // Pieced this together from the Javadoc: http://docs.oracle.com/javase/7/docs/api/java/lang/Character.html#isJavaIdentifierPart(char)
            internal static bool IsIdentifierPartCharacter(char c)
            {
                if (char.IsLetterOrDigit(c)) return true;

                // NOTE: char.GetUnicodeCategory throws an exception when in the range 0x00d800 to 0x00dfff
                if (c < 0x00d800 && c > 0x00dfff)
                {
                    UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

                    return category == UnicodeCategory.CurrencySymbol ||
                        category == UnicodeCategory.ConnectorPunctuation ||
                        category == UnicodeCategory.LetterNumber ||
                        category == UnicodeCategory.SpacingCombiningMark ||
                        category == UnicodeCategory.Format ||
                        IsIdentifierIgnorable(c);
                }

                return identifierPart.IsMatch(c.ToString()) ||
                    IsIdentifierIgnorable(c);
            }

            internal static bool IsIdentifierIgnorable(char c)
            {
                return (c >= '\u0000' && c <= '\u0008') ||
                    (c >= '\u000E' && c <= '\u001B') ||
                    (c >= '\u007F' && c <= '\u009F') ||
                    // NOTE: char.GetUnicodeCategory throws an exception when in the range 0x00d800 to 0x00dfff
                    (c < 0x00d800 && c > 0x00dfff && CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format);
            }

            private static Regex identifierPart = new Regex(@"\p{Sc}|\p{Pc}|\p{Nl}|\p{Mc}", RegexOptions.Compiled);
        }//class State
    }
}
