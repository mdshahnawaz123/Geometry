using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Geometry
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PlaneGeometry : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;
            try
            {
                //Lets Check the Plane Creation and Lets Visualize it:

                var upperleft = new XYZ(10, 0, 0);
                var upperRight = new XYZ(10, 10, 0);
                var lowerLeft = new XYZ(0, 0, 0);
                var lowerRight = new XYZ(0, 10, 0);

                var curve = new List<GeometryObject>();
                curve.Add(Line.CreateBound(upperleft, upperRight));
                curve.Add(Line.CreateBound(upperRight, lowerRight));
                curve.Add(Line.CreateBound(lowerRight, lowerLeft));
                curve.Add(Line.CreateBound(lowerLeft, upperleft));

                doc.CreateElement(curve);


            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }
}
