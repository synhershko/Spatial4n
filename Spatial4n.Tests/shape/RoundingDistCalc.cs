using System;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Spatial4n.Tests.shape
{
    public class RoundingDistCalc : AbstractDistanceCalculator 
    {
        private readonly IDistanceCalculator _delegate;

        public RoundingDistCalc(IDistanceCalculator _delegate)
        {
            this._delegate = _delegate;
        }

        double Round(double val)
        {
            double scale = Math.Pow(10, 10 /*digits precision*/);
            return Math.Round(val*scale)/scale;
        }

        public override double Distance(IPoint @from, double toX, double toY)
        {
            return Round(_delegate.Distance(from, toX, toY));
        }

        public override IPoint PointOnBearing(IPoint @from, double distDEG, double bearingDEG, SpatialContext ctx, IPoint reuse)
        {
            return _delegate.PointOnBearing(from, distDEG, bearingDEG, ctx, reuse);
        }

        public override IRectangle CalcBoxByDistFromPt(IPoint @from, double distDEG, SpatialContext ctx, IRectangle reuse)
        {
            return _delegate.CalcBoxByDistFromPt(from, distDEG, ctx, reuse);
        }

        public override double CalcBoxByDistFromPt_yHorizAxisDEG(IPoint @from, double distDEG, SpatialContext ctx)
        {
            return _delegate.CalcBoxByDistFromPt_yHorizAxisDEG(from, distDEG, ctx);
        }

        public override double Area(IRectangle rect)
        {
            return _delegate.Area(rect);
        }

        public override double Area(ICircle circle)
        {
            return _delegate.Area(circle);
        }
    }
}
