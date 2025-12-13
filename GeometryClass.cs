using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace Geometry
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class GeometryClass : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                using (Transaction t = new Transaction(doc, "Set Mark = Room Number"))
                {
                    t.Start();

                    // Collect all rooms
                    List<Room> rooms = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Rooms)
                        .WhereElementIsNotElementType()
                        .Cast<Room>()
                        .ToList();

                    if (!rooms.Any())
                    {
                        message = "No rooms found in document.";
                        return Result.Failed;
                    }

                    int totalSet = 0;

                    foreach (Room room in rooms)
                    {
                        if (room == null) continue;
                        string roomNumber = room.Number;
                        if (string.IsNullOrEmpty(roomNumber)) continue;

                        BoundingBoxXYZ bb = room.get_BoundingBox(null);
                        if (bb == null) continue;

                        Outline outline = new Outline(bb.Min, bb.Max);
                        var bbFilter = new BoundingBoxIntersectsFilter(outline);

                        // All elements whose bounding box intersects this room
                        IList<Element> candidates = new FilteredElementCollector(doc)
                            .WherePasses(bbFilter)
                            .WhereElementIsNotElementType()
                            .ToElements();

                        foreach (Element e in candidates)
                        {
                            if (e.Id == room.Id) // skip the room itself
                                continue;

                            if (!IsElementInRoom(e, room))
                                continue;

                            if (SetMarkToRoomNumber(e, roomNumber))
                                totalSet++;
                        }
                    }

                    TaskDialog.Show("Room Marks",
                        $"Elements updated with Room Number as Mark: {totalSet}");

                    t.Commit();
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Sets the ALL_MODEL_MARK parameter to the given room number.
        /// </summary>
        private static bool SetMarkToRoomNumber(Element e, string roomNumber)
        {
            Parameter mark = e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            if (mark == null || mark.IsReadOnly)
                return false;

            mark.Set(roomNumber);
            return true;
        }

        /// <summary>
        /// Tries to decide whether an element should belong to a given room.
        /// Handles FamilyInstances (doors, windows, equipment, etc.), walls and generic elements.
        /// </summary>
        private static bool IsElementInRoom(Element? e, Room? room)
        {
            // ---------- FAMILY INSTANCES (doors, windows, furniture, MEP equipment, etc.) ----------
            if (e is FamilyInstance fi)
            {
                Category cat = fi.Category;
                Room? relatedRoom = null;

                if (cat != null && cat.Id == new ElementId(BuiltInCategory.OST_Doors))
                {
                    // Door between rooms – use ToRoom / FromRoom
                    relatedRoom = fi.ToRoom ?? fi.FromRoom;
                }
                else if (cat != null && cat.Id == new ElementId(BuiltInCategory.OST_Windows))
                {
                    // Window – same logic
                    relatedRoom = fi.ToRoom ?? fi.FromRoom;
                }
                else
                {
                    // Furniture, equipment, fixtures, most MEP families, etc.
                    relatedRoom = fi.Room;
                }

                if (relatedRoom != null && relatedRoom.Id == room?.Id)
                    return true;

                // if Room data did not help, fall through to geometric check
            }

            // ---------- WALLS ----------
            if (e is Wall wall)
            {
                return IsWallInRoom(wall, room);
            }

            // ---------- GENERAL GEOMETRIC CHECK ----------
            Location loc = e.Location;
            if (loc == null)
                return false;

            XYZ testPoint = null;

            if (loc is LocationPoint lp)
            {
                testPoint = lp.Point;
            }
            else if (loc is LocationCurve lc)
            {
                // midpoint of the curve
                testPoint = lc.Curve.Evaluate(0.5, true);
            }

            if (testPoint == null)
                return false;

            return room.IsPointInRoom(testPoint);
        }

        /// <summary>
        /// Approximate test for whether a wall belongs to a given room.
        /// Offsets a point on the wall to each side and checks the room on each side.
        /// </summary>
        private static bool IsWallInRoom(Wall? wall, Room room)
        {
            LocationCurve locCurve = wall.Location as LocationCurve;
            if (locCurve == null)
                return false;

            Curve c = locCurve.Curve;
            if (c == null)
                return false;

            XYZ p0 = c.GetEndPoint(0);
            XYZ p1 = c.GetEndPoint(1);

            XYZ dir = p1 - p0;
            // project to XY plane
            dir = new XYZ(dir.X, dir.Y, 0.0);
            if (dir.IsZeroLength())
                return false;

            dir = dir.Normalize();
            // perpendicular vector in XY plane
            XYZ normal = new XYZ(-dir.Y, dir.X, 0.0);

            // midpoint of wall
            XYZ mid = c.Evaluate(0.5, true);

            // offset distance in feet (0.3 ft ≈ 9 cm)
            double offset = 0.3;

            XYZ ptSide1 = mid + normal * offset;
            XYZ ptSide2 = mid - normal * offset;

            bool side1InRoom = room.IsPointInRoom(ptSide1);
            bool side2InRoom = room.IsPointInRoom(ptSide2);

            // In per-room loop: wall "belongs" to any room that one of its sides is in
            return side1InRoom || side2InRoom;
        }
    }
}