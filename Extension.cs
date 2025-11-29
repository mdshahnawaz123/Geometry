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

        public static void Visualiuze(this XYZ point,Document document)
        {
            if(point == null)
            {
                TaskDialog.Show("Message", "Point are Missing");
            }
            document.CreateElement(Point.Create(point));
            
        }

        //Lets Create the Extension Method for Visualiuze the Curve:


        public static void Visualiuze (this Curve curve,Document document)
        {
            if(curve == null)
            {
                TaskDialog.Show("Message", "Curve are Missing");
            }
            document.CreateElement(curve);
        }

        //Lets Create the Extension Method for GetPlacement Point

        public static XYZ GetPlacementPoint(this Element element)
        {
            if(element == null)
            {
                TaskDialog.Show("Message", "Please Check the Selected Element");
            }

            var locPoint = element.Location as LocationPoint;

            return locPoint.Point;
            
        }

        //Lets Create the Extension Method for GetPlacementCurve:

        public static Curve GetPlacementCurve(this Element element)
        {
            if(element == null)
            {
                TaskDialog.Show("Message", "Please Check the Selected Element");
            }
            var locCurve = element.Location as LocationCurve;
            return locCurve.Curve;
        }



        //Lets Create the Extension Method fot Transition doAction:

        public static void doAction(this Transaction transaction, Action doAction)
        {
            if(transaction == null || doAction == null)
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
            if(vector == null || point == null)
            {
                TaskDialog.Show("Message", "Please Check the Selected Element and Vector that need to be move");
            }
            vector.Add(point.Normalize()*distance);
            return vector;
        }

        public static Curve AsCurve(this XYZ vector, XYZ origin = null, double? length = null)
        {
            origin??= XYZ.Zero;
            length??= vector.GetLength();
            return Line.CreateBound(origin,origin.MoveAlongVector(vector.Normalize(),length.GetValueOrDefault()));
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

    }
}
