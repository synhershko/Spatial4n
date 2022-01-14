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
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using System;
using System.Diagnostics;
using Xunit;
using Range = Spatial4n.Core.Shapes.Impl.Range;

namespace Spatial4n.Core.Shape
{
    /// <summary>
    /// A base test class with utility methods to help test shapes.
    /// Extends from RandomizedTest.
    /// </summary>
    public abstract class RandomizedShapeTest
    {
        protected readonly Random random = new Random(RandomSeed.Seed());
        private static readonly double DEFAULT_MULTIPLIER = 1.0d;

        /**
        * The global multiplier property (Double).
        * 
        * @see #multiplier()
        */
        public static readonly string SYSPROP_MULTIPLIER = "randomized.multiplier";

        protected static readonly double EPS = 10e-9;

        protected SpatialContext ctx;//needs to be set ASAP

        /** Used to reduce the space of numbers to increase the likelihood that
         * random numbers become equivalent, and thus trigger different code paths.
         * Also makes some random shapes easier to manually examine.
         */
        protected readonly double DIVISIBLE = 2;// even coordinates; (not always used)

        protected RandomizedShapeTest()
        {
        }

        public RandomizedShapeTest(SpatialContext ctx)
        {
            this.ctx = ctx;
        }

        public static void CheckShapesImplementEquals(Type[] classes)
        {
            foreach (Type clazz in classes)
            {
                try
                {
                    clazz.GetMethod("Equals");
                }
#pragma warning disable 168
                catch (Exception e)
#pragma warning restore 168
                {
                    Assert.True(false, "Shape needs to define 'equals' : " + clazz.Name);
                }
                try
                {
                    clazz.GetMethod("GetHashCode");
                }
#pragma warning disable 168
                catch (Exception e)
#pragma warning restore 168
                {
                    Assert.True(false, "Shape needs to define 'hashCode' : " + clazz.Name);
                }
            }
        }

        /**
         * BUG FIX: https://github.com/carrotsearch/randomizedtesting/issues/131
         *
         * Returns a random value greater or equal to <code>min</code>. The value
         * picked is affected by {@link #isNightly()} and {@link #multiplier()}.
         *
         * @see #scaledRandomIntBetween(int, int)
         */
        public virtual int AtLeast(int min)
        {
            if (min < 0) throw new ArgumentException("atLeast requires non-negative argument: " + min);

            min = (int)Math.Min(min, /*(isNightly() ? 3 * min :*/ min/*)*/ * Multiplier());
            int max = (int)Math.Min(int.MaxValue, (long)min + (min / 2));
            return random.Next(min, max);
        }

        //These few norm methods normalize the arguments for creating a shape to
        // account for the dateline. Some tests loop past the dateline or have offsets
        // that go past it and it's easier to have them coded that way and correct for
        // it here.  These norm methods should be used when needed, not frivolously.

        protected virtual double NormX(double x)
        {
            return ctx.IsGeo ? DistanceUtils.NormLonDEG(x) : x;
        }

        protected virtual double NormY(double y)
        {
            return ctx.IsGeo ? DistanceUtils.NormLatDEG(y) : y;
        }

        protected virtual IRectangle MakeNormRect(double minX, double maxX, double minY, double maxY)
        {
            if (ctx.IsGeo)
            {
                if (Math.Abs(maxX - minX) >= 360)
                {
                    minX = -180;
                    maxX = 180;
                }
                else
                {
                    minX = DistanceUtils.NormLonDEG(minX);
                    maxX = DistanceUtils.NormLonDEG(maxX);
                }

            }
            else
            {
                if (maxX < minX)
                {
                    double t = minX;
                    minX = maxX;
                    maxX = t;
                }
                minX = BoundX(minX, ctx.WorldBounds);
                maxX = BoundX(maxX, ctx.WorldBounds);
            }
            if (maxY < minY)
            {
                double t = minY;
                minY = maxY;
                maxY = t;
            }
            minY = BoundY(minY, ctx.WorldBounds);
            maxY = BoundY(maxY, ctx.WorldBounds);
            return ctx.MakeRectangle(minX, maxX, minY, maxY);
        }

        public static double Divisible(double v, double divisible)
        {
            return (int)(Math.Round(v / divisible) * divisible);
        }

        protected virtual double Divisible(double v)
        {
            return Divisible(v, DIVISIBLE);
        }

        /** reset()'s p, and confines to world bounds. Might not be divisible if
         * the world bound isn't divisible too.
         */
        protected virtual IPoint Divisible(IPoint p)
        {
            IRectangle bounds = ctx.WorldBounds;
            double newX = BoundX(Divisible(p.X), bounds);
            double newY = BoundY(Divisible(p.Y), bounds);
            p.Reset(newX, newY);
            return p;
        }

        static double BoundX(double i, IRectangle bounds)
        {
            return Bound(i, bounds.MinX, bounds.MaxX);
        }

        static double BoundY(double i, IRectangle bounds)
        {
            return Bound(i, bounds.MinY, bounds.MaxY);
        }

        static double Bound(double i, double min, double max)
        {
            if (i < min) return min;
            if (i > max) return max;
            return i;
        }

        protected virtual void AssertRelation(SpatialRelation expected, IShape a, IShape b)
        {
            AssertRelation(null, expected, a, b);
        }

        protected virtual void AssertRelation(string msg, SpatialRelation expected, IShape a, IShape b)
        {
            AssertIntersect(msg, expected, a, b);
            //check flipped a & b w/ transpose(), while we're at it
            AssertIntersect(msg, expected.Transpose(), b, a);
        }

        private void AssertIntersect(string msg, SpatialRelation expected, IShape a, IShape b)
        {
            SpatialRelation sect = a.Relate(b);
            if (sect == expected)
                return;
            msg = ((msg == null) ? "" : msg + "\r") + a + " intersect " + b;
            if (expected == SpatialRelation.WITHIN || expected == SpatialRelation.CONTAINS)
            {
                if (a.GetType().Equals(b.GetType())) // they are the same shape type
                    Assert.Equal(/*msg,*/ a, b);
                else
                {
                    //they are effectively points or lines that are the same location
                    Assert.True(!a.HasArea, msg);
                    Assert.True(!b.HasArea, msg);

                    IRectangle aBBox = a.BoundingBox;
                    IRectangle bBBox = b.BoundingBox;
                    if (aBBox.Height == 0 && bBBox.Height == 0
                        && (aBBox.MaxY == 90 && bBBox.MaxY == 90
                      || aBBox.MinY == -90 && bBBox.MinY == -90))
#pragma warning disable 642
                        ;//== a point at the pole
#pragma warning restore 642
                    else
                        Assert.Equal(/*msg,*/ aBBox, bBBox);
                }
            }
            else
            {
                Assert.Equal(/*msg,*/ expected, sect);//always fails
            }
        }

        protected virtual void AssertEqualsRatio(string msg, double expected, double actual)
        {
            double delta = Math.Abs(actual - expected);
            double @base = Math.Min(actual, expected);
            double deltaRatio = @base == 0 ? delta : Math.Min(delta, delta / @base);
            CustomAssert.EqualWithDelta(/*msg,*/ 0, deltaRatio, EPS);
        }

        protected virtual int RandomIntBetweenDivisible(int start, int end)
        {
            return RandomIntBetweenDivisible(start, end, (int)DIVISIBLE);
        }
        /** Returns a random integer between [start, end]. Integers between must be divisible by the 3rd argument. */
        protected virtual int RandomIntBetweenDivisible(int start, int end, int divisible)
        {
            // DWS: I tested this
            int divisStart = (int)Math.Ceiling((start + 1) / (double)divisible);
            int divisEnd = (int)Math.Floor((end - 1) / (double)divisible);
            int divisRange = Math.Max(0, divisEnd - divisStart + 1);
            int r = random.Next(1 + divisRange);//remember that '0' is counted
            if (r == 0)
                return start;
            if (r == 1)
                return end;
            return (r - 2 + divisStart) * divisible;
        }

        protected virtual IRectangle RandomRectangle(IPoint nearP)
        {
            IRectangle bounds = ctx.WorldBounds;
            if (nearP == null)
                nearP = RandomPointIn(bounds);

            Range xRange = RandomRange(Rarely() ? 0 : nearP.X, Range.XRange(bounds, ctx));
            Range yRange = RandomRange(Rarely() ? 0 : nearP.Y, Range.YRange(bounds, ctx));

            return MakeNormRect(
                Divisible(xRange.Min),
                Divisible(xRange.Max),
                Divisible(yRange.Min),
                Divisible(yRange.Max));
        }

        private Range RandomRange(double near, Range bounds)
        {
            double mid = near + RandomGaussian() * bounds.Width / 6;
            double width = Math.Abs(RandomGaussian()) * bounds.Width / 6;//1/3rd
            return new Range(mid - width / 2, mid + width / 2);
        }

        private double RandomGaussianZeroTo(double max)
        {
            if (max == 0)
                return max;
            Debug.Assert(max > 0);
            double r;
            do
            {
                r = Math.Abs(RandomGaussian()) * (max * 0.50);
            } while (r > max);
            return r;
        }

        protected virtual IRectangle RandomRectangle(int divisible)
        {
            double rX = RandomIntBetweenDivisible(-180, 180, divisible);
            double rW = RandomIntBetweenDivisible(0, 360, divisible);
            double rY1 = RandomIntBetweenDivisible(-90, 90, divisible);
            double rY2 = RandomIntBetweenDivisible(-90, 90, divisible);
            double rYmin = Math.Min(rY1, rY2);
            double rYmax = Math.Max(rY1, rY2);
            if (rW > 0 && rX == 180)
                rX = -180;
            return MakeNormRect(rX, rX + rW, rYmin, rYmax);
        }

        protected virtual IPoint RandomPoint()
        {
            return RandomPointIn(ctx.WorldBounds);
        }

        protected virtual IPoint RandomPointIn(ICircle c)
        {
            double d = c.Radius * random.NextDouble();
            double angleDEG = 360 * random.NextDouble();
            IPoint p = ctx.DistCalc.PointOnBearing(c.Center, d, angleDEG, ctx, null);
            Assert.Equal(SpatialRelation.CONTAINS, c.Relate(p));
            return p;
        }

        protected virtual IPoint RandomPointIn(IRectangle r)
        {
            double x = r.MinX + random.NextDouble() * r.Width;
            double y = r.MinY + random.NextDouble() * r.Height;
            x = NormX(x);
            y = NormY(y);
            IPoint p = ctx.MakePoint(x, y);
            Assert.Equal(SpatialRelation.CONTAINS, r.Relate(p));
            return p;
        }

        protected virtual IPoint RandomPointIn(IShape shape)
        {
            if (!shape.HasArea)// or try the center?
                throw new InvalidOperationException("Need area to define shape!");
            IRectangle bbox = shape.BoundingBox;
            IPoint p;
            do
            {
                p = RandomPointIn(bbox);
            } while (!bbox.Relate(p).Intersects());
            return p;
        }


        #region Support from RandomizedTest

        /// <summary>
        /// Returns true if something should happen rarely,
        /// <p>
        /// The actual number returned will be influenced by whether <seealso cref="#TEST_NIGHTLY"/>
        /// is active and <seealso cref="#RANDOM_MULTIPLIER"/>.
        /// </summary>
        public bool Rarely(Random random)
        {
            int p = 1; // TEST_NIGHTLY ? 10 : 1;
            p += (int)(p * Math.Log(Multiplier() /*RANDOM_MULTIPLIER*/));
            int min = 100 - Math.Min(p, 50); // never more than 50
            return random.Next(100) >= min;
        }

        public bool Rarely()
        {
            return Rarely(random);
        }

        private double nextNextGaussian;
        private bool haveNextNextGaussian = false;

        public static object ConfigurationManager { get; private set; }

        /**
         * Returns the next pseudorandom, Gaussian ("normally") distributed
         * {@code double} value with mean {@code 0.0} and standard
         * deviation {@code 1.0} from this random number generator's sequence.
         * <p>
         * The general contract of {@code nextGaussian} is that one
         * {@code double} value, chosen from (approximately) the usual
         * normal distribution with mean {@code 0.0} and standard deviation
         * {@code 1.0}, is pseudorandomly generated and returned.
         *
         * <p>The method {@code nextGaussian} is implemented by class
         * {@code Random} as if by a threadsafe version of the following:
         *  <pre> {@code
         * private double nextNextGaussian;
         * private boolean haveNextNextGaussian = false;
         *
         * public double nextGaussian() {
         *   if (haveNextNextGaussian) {
         *     haveNextNextGaussian = false;
         *     return nextNextGaussian;
         *   } else {
         *     double v1, v2, s;
         *     do {
         *       v1 = 2 * nextDouble() - 1;   // between -1.0 and 1.0
         *       v2 = 2 * nextDouble() - 1;   // between -1.0 and 1.0
         *       s = v1 * v1 + v2 * v2;
         *     } while (s >= 1 || s == 0);
         *     double multiplier = StrictMath.sqrt(-2 * StrictMath.log(s)/s);
         *     nextNextGaussian = v2 * multiplier;
         *     haveNextNextGaussian = true;
         *     return v1 * multiplier;
         *   }
         * }}</pre>
         * This uses the <i>polar method</i> of G. E. P. Box, M. E. Muller, and
         * G. Marsaglia, as described by Donald E. Knuth in <i>The Art of
         * Computer Programming</i>, Volume 3: <i>Seminumerical Algorithms</i>,
         * section 3.4.1, subsection C, algorithm P. Note that it generates two
         * independent values at the cost of only one call to {@code StrictMath.log}
         * and one call to {@code StrictMath.sqrt}.
         *
         * @return the next pseudorandom, Gaussian ("normally") distributed
         *         {@code double} value with mean {@code 0.0} and
         *         standard deviation {@code 1.0} from this random number
         *         generator's sequence
         */
        public double RandomGaussian()
        {
            // See Knuth, ACP, Section 3.4.1 Algorithm C.
            if (haveNextNextGaussian)
            {
                haveNextNextGaussian = false;
                return nextNextGaussian;
            }
            else
            {
                double v1, v2, s;
                do
                {
                    v1 = 2 * random.NextDouble() - 1; // between -1 and 1
                    v2 = 2 * random.NextDouble() - 1; // between -1 and 1
                    s = v1 * v1 + v2 * v2;
                } while (s >= 1 || s == 0);
                double multiplier = Math.Sqrt(-2 * Math.Log(s) / s);
                nextNextGaussian = v2 * multiplier;
                haveNextNextGaussian = true;
                return v1 * multiplier;
            }
        }

        public int Multiplier()
        {
            string mult = Environment.GetEnvironmentVariable(SYSPROP_MULTIPLIER);
            int result;
            if (int.TryParse(mult, out result))
            {
                return result;
            }

            return (int)DEFAULT_MULTIPLIER;
        }

        #endregion

    }
}
