using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Geometry
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MasterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // 1. Get the ViewFamilyType for Sections
            ViewFamilyType sectionType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(x => x.ViewFamily == ViewFamily.Section)!;

            if (sectionType! == null)
            {
                message = "No Section ViewFamilyType found in this document.";
                return Result.Failed;
            }

            // 2. Collect all doors (instances, not types)
            IList<Element> doors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .ToElements();

            if (!doors.Any())
            {
                TaskDialog.Show("Info", "No doors found in the active document.");
                return Result.Succeeded;
            }

            using (Transaction t = new Transaction(doc, "Create Sections for Doors"))
            {
                t.Start();

                foreach (Element e in doors)
                {
                    FamilyInstance door = e as FamilyInstance;
                    if (door == null)
                        continue;

                    LocationPoint lp = door.Location as LocationPoint;
                    if (lp == null)
                        continue;

                    XYZ doorPoint = lp.Point;

                    // 3. Define section orientation based on door facing
                    // View direction looks opposite FacingOrientation
                    XYZ viewDir = -door.FacingOrientation;      // direction we look towards
                    XYZ up = XYZ.BasisZ;                         // vertical
                    XYZ right = up.CrossProduct(viewDir);        // horizontal, to the right

                    // Normalize
                    viewDir = viewDir.Normalize();
                    up = up.Normalize();
                    right = right.Normalize();

                    // 4. Build the transform for the section box
                    Transform boxTransform = Transform.Identity;
                    boxTransform.Origin = doorPoint;             // center of the door (adjust if needed)
                    boxTransform.BasisX = right;                 // X axis: right
                    boxTransform.BasisY = up;                    // Y axis: up
                    boxTransform.BasisZ = viewDir;               // Z axis: towards viewer (Revit looks along -Z)

                    // 5. Define the extents of the section box in local (section) coordinates
                    // Units are in feet (Revit internal units)
                    double halfWidth = 3.0;      // 3' to left/right of door center
                    double below = 2.0;          // 2' below door base
                    double above = 7.0;          // 7' above door base
                    double depth = 3.0;          // 3' depth in front of the door

                    BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
                    sectionBox.Transform = boxTransform;

                    // In local coords:
                    // X: left/right, Y: up, Z: towards viewer
                    // Revit looks in -Z, so we usually keep Min.Z = 0, Max.Z = depth
                    sectionBox.Min = new XYZ(-halfWidth, -below, 0);
                    sectionBox.Max = new XYZ(halfWidth, above, depth);

                    // 6. Create the section view
                    ViewSection view = ViewSection.CreateSection(doc, sectionType.Id, sectionBox);
                }

                t.Commit();
            }

            return Result.Succeeded;
        }
    }
}
