using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geometry
{
    public static class Extension
    {
        // This Method will create DirectShape from List of GeometryObject:
        public static DirectShape CreateElement(this Document doc, IList<GeometryObject> geoObject, BuiltInCategory builtInCategory = BuiltInCategory.OST_GenericModel)
        {
            var categoryId = new ElementId(builtInCategory);
            var ds = DirectShape.CreateElement(doc, categoryId);
            ds.SetShape(geoObject);
            return ds;
        }

        // This Method will create DirectShape from single GeometryObject:
        public static DirectShape CreateElement(this Document doc, GeometryObject geoObject, BuiltInCategory builtInCategory = BuiltInCategory.OST_GenericModel)
        {
            var categoryId = new ElementId(builtInCategory);
            var ds = DirectShape.CreateElement(doc, categoryId);
            ds.SetShape(new List<GeometryObject> { geoObject });
            return ds;
        }

        //Lets Create the Extension Method for Visualiuze the xyz point:

        public static void Visualiuze(this XYZ point, Document document)
        {
            if (point == null)
            {
                TaskDialog.Show("Message", "Point are Missing");
            }
            document.CreateElement(Point.Create(point));

        }

        //Lets Create the Extension Method for Visualiuze the Curve:


        public static void Visualiuze(this Curve curve, Document document)
        {
            if (curve == null)
            {
                TaskDialog.Show("Message", "Curve are Missing");
            }
            document.CreateElement(curve);
        }

        //Lets Create the Extension Method for GetPlacement Point

        public static XYZ GetPlacementPoint(this Element element)
        {
            if (element == null)
            {
                TaskDialog.Show("Message", "Please Check the Selected Element");
            }

            var locPoint = element.Location as LocationPoint;

            return locPoint.Point;

        }

        //Lets Create the Extension Method for GetPlacementCurve:

        public static Curve GetPlacementCurve(this Element element)
        {
            if (element == null)
            {
                TaskDialog.Show("Message", "Please Check the Selected Element");
            }
            var locCurve = element.Location as LocationCurve;
            return locCurve.Curve;
        }



        //Lets Create the Extension Method fot Transition doAction:

        public static void doAction(this Transaction transaction, Action doAction)
        {
            if (transaction == null || doAction == null)
            {
                TaskDialog.Show("Message", "Please Check the Transaction and Action");
            }
            transaction.Start();
            doAction.Invoke();
            transaction.Commit();
        }

        //Lets Create the Extension Method for MovePointAlongVector:

        public static XYZ MoveAlongVector(this XYZ vector, XYZ point)
        {
            if (vector == null || point == null)
            {
                TaskDialog.Show("Message", "Please Check the Selected Element and Vector that need to be move");
            }
            return vector.Add(point);
        }

        //Lets Create the Extension Method for MovePointAlongVector with distance:

        /// <summary>
        /// This Method will move the point along the vector with distance
        /// <param name="vector">The Vector to move along</param>
        /// <param name="point">The Point to move</param>
        /// <param name="distance">The Distance to move</param>
        /// <Returns>The new point after moving</returns>


        public static XYZ MoveAlongVector(this XYZ vector, XYZ point, double distance)
        {
            if (vector == null || point == null)
            {
                TaskDialog.Show("Message", "Please Check the Selected Element and Vector that need to be move");
            }
            vector.Add(point.Normalize() * distance);
            return vector;
        }

        public static Curve AsCurve(this XYZ vector, XYZ origin = null, double? length = null)
        {
            origin ??= XYZ.Zero;
            length ??= vector.GetLength();
            return Line.CreateBound(origin, origin.MoveAlongVector(vector.Normalize(), length.GetValueOrDefault()));
        }

        public static void Visualize(
            this Plane plane, Document doc, int scale = 3)
        {
            var planeOrigin = plane.Origin;
            var upperRightCorner = planeOrigin + (plane.XVec * scale) + (plane.YVec * scale);
            var upperLeftCorner = planeOrigin - (plane.XVec * scale) + (plane.YVec * scale);
            var bottomRightCorner = planeOrigin + (plane.XVec * scale) - (plane.YVec * scale);
            var bottomLeftCorner = planeOrigin - (plane.XVec * scale) - (plane.YVec * scale);
            var curves = new List<GeometryObject>();
            curves.Add(Line.CreateBound(upperRightCorner, upperLeftCorner));
            curves.Add(Line.CreateBound(upperRightCorner, bottomRightCorner));
            curves.Add(Line.CreateBound(upperLeftCorner, bottomLeftCorner));
            curves.Add(Line.CreateBound(bottomLeftCorner, bottomRightCorner));
            curves.Add(Line.CreateBound(planeOrigin, planeOrigin + plane.Normal));
            doc.CreateElement(curves);
        }

        public static void Visualize(this Solid solid, Document doc)
        {
            doc.CreateElement(solid);
        }
        public enum GeometryRepresentation
        {
            Instance,
            Symbol
        }
        //Lets Write the Extension Method for Geometry:

        public static IEnumerable<T> GetRootElements<T>(
            this GeometryElement geometryElement,
            GeometryRepresentation geometryRepresentation = GeometryRepresentation.Instance)
            where T : GeometryObject
        {
            if (geometryElement == null)
                throw new ArgumentNullException(nameof(geometryElement));

            foreach (GeometryObject geometryObject in geometryElement)
            {
                // Try to return this object if it is T
                T ultimateElement = geometryObject as T;
                if (ultimateElement != null)
                {
                    yield return ultimateElement;
                    continue;
                }

                // Drill into GeometryInstance
                GeometryInstance geometryInstance = geometryObject as GeometryInstance;
                if (geometryInstance != null)
                {
                    GeometryElement familyGeometries =
                        (geometryRepresentation == GeometryRepresentation.Symbol)
                            ? geometryInstance.SymbolGeometry
                            : geometryInstance.GetInstanceGeometry();

                    foreach (T familyGeometry in GetRootElements<T>(familyGeometries, geometryRepresentation))
                        yield return familyGeometry;

                    continue;
                }

                // Drill into nested GeometryElement
                GeometryElement nestedGeometryElement = geometryObject as GeometryElement;
                if (nestedGeometryElement != null)
                {
                    foreach (T nestedElement in GetRootElements<T>(nestedGeometryElement, geometryRepresentation))
                        yield return nestedElement;
                }
            }
        }

        //Lets Create Extension Method, so When we will select Element that will give us Edge Point:

        public static IList<Solid> GetSolid(this Element ele, Document doc)
        {
            var solids = new List<Solid>();
            var geoElement = ele.get_Geometry(new Options());
            foreach (var solid in geoElement.GetRootElements<Solid>())
            {
                solids.Add(solid);
            }
            return solids;
        }

        //Lets Create Extension Method to get all Edge

        public static IList<Edge> GetEdges(this Element ele, Document doc)
        {
            var edges = new List<Edge>();
            var geoElement = ele.get_Geometry(new Options());

            var edge = geoElement.Cast<Edge>().ToList();
            foreach (var e in edge)
            {
                edges.Add(e);
            }
            return edges;
        }

        public static IList<Face> getFace(this Element ele, Document doc)
        {
            var geo = ele.get_Geometry(new Options());
            var faces = geo.OfType<Solid>().SelectMany(x => x.Faces.Cast<Face>()).ToList();
            return faces;
        }

        //Lets Create Extension Method to get all Edge
        public static IList<Edge> getEdges(this Element ele, Document doc)
        {
            var opt = new Options
            {
                IncludeNonVisibleObjects = false,
                ComputeReferences = true,
                DetailLevel = ViewDetailLevel.Fine
                // You can also do: View = doc.ActiveView;
            };

            var geo = ele.get_Geometry(opt);
            if (geo == null)
                return new List<Edge>();

            var edges = geo
                // Flatten: root objects + contents of any instances
                .SelectMany(go =>
                    go is GeometryInstance gi
                        ? gi.GetInstanceGeometry().Cast<GeometryObject>()
                        : new[] { go })
                .OfType<Solid>()                       // only solids
                .Where(s => s.Edges.Size > 0)
                .SelectMany(s => s.Edges.Cast<Edge>()) // EdgeArray → Edge
                .ToList();

            return edges;
        }

        //Lets Create Extension Method for Solid Union

        public static Solid unionSolid(this IEnumerable<Solid> solids)
        {
            return solids
                .Where(x => x.HasVolumne())
                .Aggregate((x, y) => BooleanOperationsUtils
                .ExecuteBooleanOperation(x, y, BooleanOperationsType.Union));
        }
        public static bool HasVolumne(this Solid solids)
        {
            return solids.Volume > 0;
        }
        //Lets Create Extension and Visulize Method for The Point XYZ

        public static XYZ Mid(this Curve curve)
        {
            return curve.Evaluate(0.5, true);
        }
    }
}
