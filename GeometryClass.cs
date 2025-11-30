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
                //Lets Select the Elemet and Get the Solid

                Transaction Trans = new Transaction(doc, "Select Element");
                Trans.Start();

                var selection = uidoc.Selection.GetElementIds().First();
                var ele = doc.GetElement(selection);
                var edge = ele.getEdges(doc);
                foreach(var e in elements)
                {
                     
                   
                }
                Trans.Commit();

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
