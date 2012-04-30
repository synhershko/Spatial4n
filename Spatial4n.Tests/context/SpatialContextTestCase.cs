using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spatial4n.Core.Context;

namespace Spatial4n.Tests.context
{
	public class SpatialContextTestCase : BaseSpatialContextTestCase
	{
		protected override SpatialContext GetSpatialContext()
		{
			return SpatialContext.GEO_KM;
		}
	}
}
