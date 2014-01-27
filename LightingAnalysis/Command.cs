#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Analysis;
#endregion

namespace LightingAnalysis
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
#if !VERSION2014
            Debug.Print("Version 2013");
#else
      Debug.Print( "Version 2014" );
#endif // VERSION2014
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            // Create Analysis Results
            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (null == sfm)
	{
		 sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView,1);
	}

            // Pick Space to perform analysis            

            Reference reference = uidoc.Selection.PickObject(ObjectType.Element, new SpaceSelectionFilter(doc), "Select a Space");            

            // Gather all spaceInfo
            RoomSpace roomSpace = new RoomSpace(doc, reference);            

            // Perform calculations on selected space
            LightingCalculations lc = new LightingCalculations(doc,sfm);
            bool pbpcalc = lc.pointByPointCalculation(roomSpace);

            //Get Or Create Avs
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("DisplayResults");

                bool avs = lc.GetOrCreateAVS();

                tx.Commit();
            }


            return Result.Succeeded;
        }
    }
}
