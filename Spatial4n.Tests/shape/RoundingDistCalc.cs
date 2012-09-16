using System;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Spatial4n.Tests.shape
{
    public class RoundingDistCalc : AbstractDistanceCalculator 
    {
        private readonly DistanceCalculator _delegate;

        public RoundingDistCalc(DistanceCalculator _delegate)
        {
            this._delegate = _delegate;
        }

        double Round(double val)
        {
            double scale = Math.Pow(10, 10 /*digits precision*/);
            return Math.Round(val*scale)/scale;
        }

        public override double Distance(Point @from, double toX, double toY)
        {
            return Round(_delegate.Distance(from, toX, toY));
        }

        public override Point PointOnBearing(Point @from, double distDEG, double bearingDEG, SpatialContext ctx, Point reuse)
        {
            return _delegate.PointOnBearing(from, distDEG, bearingDEG, ctx, reuse);
        }

        public override Rectangle CalcBoxByDistFromPt(Point @from, double distDEG, SpatialContext ctx, Rectangle reuse)
        {
            return _delegate.CalcBoxByDistFromPt(from, distDEG, ctx, reuse);
        }

        public override double CalcBoxByDistFromPt_yHorizAxisDEG(Point @from, double distDEG, SpatialContext ctx)
        {
            return _delegate.CalcBoxByDistFromPt_yHorizAxisDEG(from, distDEG, ctx);
        }

        public override double Area(Rectangle rect)
        {
            return _delegate.Area(rect);
        }

        public override double Area(Circle circle)
        {
            return _delegate.Area(circle);
        }
    }
}
