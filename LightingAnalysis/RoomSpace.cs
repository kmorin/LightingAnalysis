using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Mechanical;

namespace LightingAnalysis
{
    class RoomSpace //: Space (Can't inherit space since no constructors defined
    {
        Document m_doc = null;

        // Properties //
        public double AverageEstimatedIllumination { get; protected set; }
        public double Area { get; protected set; }
        public double CeilingReflectance { get; protected set; }
        public double FloorReflectance { get; protected set; }
        public double WallReflectance { get; protected set; }
        public double CalcWorkPlane { get; protected set; }
        public List<LightFixture> LightFixtures { get; protected set; }
        public Space ParentSpaceObject { get; protected set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="spaceReference"></param>
        public RoomSpace(Document doc, Reference spaceReference)
        {
            m_doc = doc;
            LightFixtures = new List<LightFixture>();
            Space refSpace = m_doc.GetElement(spaceReference) as Space;

            // Set properties of newly create RoomSpace object from Space
            AverageEstimatedIllumination = refSpace.AverageEstimatedIllumination;
            Area = refSpace.Area;
            CeilingReflectance = refSpace.CeilingReflectance;
            FloorReflectance = refSpace.FloorReflectance;
            WallReflectance = refSpace.WallReflectance;
            CalcWorkPlane = refSpace.LightingCalculationWorkplane;
            ParentSpaceObject = refSpace;
            
            // Populate light fixtures list for RoomSpace
            FilteredElementCollector fec = new FilteredElementCollector(m_doc)
            .OfCategory(BuiltInCategory.OST_LightingFixtures)
            .OfClass(typeof(FamilyInstance));
            foreach (FamilyInstance fi in fec)
            {
                if (fi.Space.Id == refSpace.Id)
                {
                    ElementId eID = fi.GetTypeId();
                    Element e = m_doc.GetElement(eID);
                    //TaskDialog.Show("C","LF: SPACEID " + fi.Space.Id.ToString() + "\nSPACE ID: " + refSpace.Id.ToString());
                    LightFixtures.Add(new LightFixture(e,fi));
                }
            }
        }
    }
}
