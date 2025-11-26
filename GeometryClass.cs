using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Geometry
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GeometryClass : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;
            try
            {
                //Points and Vector
                //Planes
                //Transform
                //Geometry Creation
                //how we can visualize our geometrical objects:
                //Extension Methods for DirectShape
                // Your code logic here

                //Lets try our Extension Methods:

                var selectedIds = uidoc.Selection.GetElementIds();

                var xVector = new XYZ(10, 0, 0);

                foreach (var selectedId in selectedIds)
                {
                    // Fix: Call GetPlacementPoint as a method (with parentheses)
                    var point = doc.GetElement(selectedId).GetPlacementPoint();

                    using (Transaction tx = new Transaction(doc, "Visualize Point"))
                    {
                        tx.Start();
                        //Lets Create the Line from Oragin of Geometry to XVector

                        var line = Line.CreateBound(point, point + xVector);
                        line.Visualiuze(doc);
                        point.Visualiuze(doc);
                        tx.Commit();
                    }
                    TaskDialog.Show("GeometryClass", "Execute method ran successfully.");
                }
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
