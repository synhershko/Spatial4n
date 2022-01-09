Spatial4n - A GeoSpatial Library for .NET
=========

[![Nuget](https://img.shields.io/nuget/dt/Spatial4n.Core)](https://www.nuget.org/packages/Spatial4n.Core)
[![Azure DevOps builds (branch)](https://img.shields.io/azure-devops/build/Spatial4n/Spatial4n/1/master)](https://dev.azure.com/Spatial4n/Spatial4n/_build?definitionId=1)
[![GitHub](https://img.shields.io/github/license/synhershko/Spatial4n)](https://github.com/synhershko/J2N/blob/master/LICENSE.txt)

Spatial4n is a general purpose spatial / geospatial library for .NET, a direct port of the [spatial4j Java library](https://github.com/locationtech/spatial4j). Its core capabilities are:

1. To provide common shapes that can be used in Euclidean and geodesic world models.
2. To provide distance calculations and other math.
3. To read and write shapes from formats like [WKT](http://en.wikipedia.org/wiki/Well-known_text).

When working with grid square indexing schemes, you will likely to find something especially useful in Spatial4n.

## Shapes and Other Features

The main part of Spatial4n is its collection of shapes.  Shapes in Spatial4n have these features:

* Compute its lat-lon bounding box.
* Compute an area.  For some shapes its more of an estimate.
* Compute if it contains a provided point.
* Compute the relationship to a lat-lon rectangle. Relationships are: `CONTAINS`, `WITHIN`, `DISJOINT`, `INTERSECTS`.  Note that Spatial4n doesn't have a notion of "touching".

Spatial4n has a variety of shapes that operate in Euclidean-space -- i.e. a flat 2D plane.  Most shapes are augmented to support a wrap-around at `X` -180/+180 for compatibility with latitude & longitudes, which is effectively a cylindrical model.  But the real bonus is its circle (i.e. point-radius shape that can operate on a surface-of-a-sphere model.  See below for further info.  The term "geodetic" or "geodesic" or "geo" is used here as synonymous with that model but technically those words have a more broad meaning.

| Shape      | Euclidean | Cylindrical | Spherical|
| -----------|:---------:|:-----------:|:--------:|
| **Point**      | Y     | Y           | Y        |
| **Rectangle**  | Y     | Y           | Y        |
| **Circle**     | Y     | N           | Y        |
| **LineString** | Y     | N           | N        |
| **Buffered L/S** | Y   | N           | N        |
| **Polygon**    | Y     | Y           | N        |
| **ShapeCollection** | Y | Y          | Y        |

* The Rectangle shape exists in the spherical model as a lat-lon rectangle, which basically means it's math is no different than cylindrical.
* Polygons don't support pole-wrap (sorry, no Antarctica polygon); just dateline-cross.  Polygons are supported by wrapping NTS's `Geometry`, which is to say that most of the fundamental logic for that shape is implemented by NTS.

### Other Features

* Read and write Shapes as [WKT](http://en.wikipedia.org/wiki/Well-known_text).  Include the ENVELOPE extension from CQL, plus a Spatial4n custom BUFFER operation. Buffering a point gets you a Circle.
* 3 great-circle distance calculators: Law of Cosines, Haversine, Vincenty

## Documentation

Currently, the best sources of documentation are the [Spatial4j javadocs](https://locationtech.github.io/spatial4j/apidocs/) and the [Spatial4j Getting Started section](https://github.com/locationtech/spatial4j#getting-started).

## Building and Testing

To build the project from source, see the [Building and Testing documentation](https://github.com/synhershko/Spatial4n/docs/building-and-testing).

## Saying Thanks

If you find this library to be useful, please star us [on GitHub](https://github.com/synhershko/Spatial4n) and consider a financial sponsorship so we can continue bringing you great free tools like this one.
