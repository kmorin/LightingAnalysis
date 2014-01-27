using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Analysis;

namespace LightingAnalysis
{    
    class LightingCalculations
    {
        Document m_doc = null;
        SpatialFieldManager m_sfm = null;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="doc"></param>
        public LightingCalculations(Document doc, SpatialFieldManager sfm)
        {
            m_doc = doc;
            m_sfm = sfm;
        }

        // TODO: change to proper return value
        /// <summary>
        /// Method call to calculate light at points in space
        /// using Lumens property of light fixtures in space
        /// </summary>
        /// <param name="roomSpace"></param>
        /// <returns>TRUE always, need to fix to return more useful.</returns>
        public bool pointByPointCalculation(RoomSpace roomSpace)
        {
            // Perform PointByPoint lighting calc on RoomSpace            

            // Get faces of roomSpace
            Face calcFace = GetFace(roomSpace);

            // bounding box UV
            BoundingBoxUV bb = calcFace.GetBoundingBox();
            UV min = bb.Min;
            UV max = bb.Max;

            // Face Transform
            UV faceCenter = new UV((max.U + min.U) / 2, (max.V + min.V) / 2);
            Transform computeDerivatives = calcFace.ComputeDerivatives(faceCenter);
            XYZ faceCenterNormal = computeDerivatives.BasisZ;

            // Normalize the normal vector and multiply by 2.5???
            XYZ faceCenterNormalMultiplied = faceCenterNormal.Normalize().Multiply(2.5);

            // Set Transform
            // Obsolete in 204:
            // Transform faceTransform = Transform.get_Translation(faceCenterNormalMultiplied);
            Transform faceTransform = Transform.CreateTranslation(faceCenterNormalMultiplied);

            List<double> doubleList = new List<double>();
            IList<UV> uvPts = new List<UV>();
            IList<ValueAtPoint> valList = new List<ValueAtPoint>();

            for (double u = min.U; u < max.U; u = u + (max.U - min.U) / 15)
            {
                for (double v = min.V; v < max.V; v = v + (max.V - min.V) / 15)
                {
                    UV uvPnt = new UV(u, v);
                    if (calcFace.IsInside(uvPnt))
                    {
                        XYZ pointToCalc = calcFace.Evaluate(uvPnt);
                        double resultFC = CalcFCAtPoint(pointToCalc, roomSpace.LightFixtures,roomSpace.CalcWorkPlane);
                        uvPts.Add(uvPnt);
                        doubleList.Add(resultFC);
                        valList.Add(new ValueAtPoint(doubleList));
                        doubleList.Clear();
                    }
                }
            }

            DoVisualization(uvPts, valList, calcFace, faceTransform);

            return true;
        }

        /// <summary>
        /// Create Visualization framework for view
        /// </summary>
        /// <param name="uvPts"></param>
        /// <param name="valList"></param>
        /// <param name="calcFace"></param>
        /// <param name="faceTransform"></param>
        private void DoVisualization(IList<UV> uvPts, IList<ValueAtPoint> valList, Face calcFace, Transform faceTransform)
        {
            // Visualization framework
            FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(uvPts);
            FieldValues vals = new FieldValues(valList);
            AnalysisResultSchema resultSchema = new AnalysisResultSchema("Point by Point", "Illumination Point-by-Point");

            // Units
            IList<string> names = new List<string> { "FC" };
            IList<double> multipliers = new List<double> { 1 };
            resultSchema.SetUnits(names, multipliers);

            // Add field primative to view
            int idx = m_sfm.AddSpatialFieldPrimitive(calcFace, faceTransform);
            int resultIndex = m_sfm.RegisterResult(resultSchema);

            // Update Field Primatives
            m_sfm.UpdateSpatialFieldPrimitive(idx, pnts, vals, resultIndex);
            
            
        }

        /// <summary>
        /// Creates new AnalysisVisualizationStyle for document
        /// </summary>
        /// <returns>TRUE</returns>
        public bool GetOrCreateAVS()
        {
            AnalysisDisplayStyle analysisDisplayStyle = null;

            FilteredElementCollector fec = new FilteredElementCollector(m_doc);
            ICollection<Element> collector = fec.OfClass(typeof(AnalysisDisplayStyle)).ToElements();

            var displayStyle = from element in collector
                               where element.Name == "KTMStyle"
                               select element;

            if (displayStyle.Count() == 0)
            {
                // Set marker settings
                AnalysisDisplayMarkersAndTextSettings markerSettings = new AnalysisDisplayMarkersAndTextSettings();
                markerSettings.MarkerType = AnalysisDisplayStyleMarkerType.Circle;
                markerSettings.MarkerSize = markerSettings.MarkerSize / 2;
                markerSettings.Rounding = 1.0;
                markerSettings.ShowText = true;
                markerSettings.TextLabelType = AnalysisDisplayStyleMarkerTextLabelType.ShowAll;

                // Another FilteredElementCollector
                FilteredElementCollector fecTextNoteType = new FilteredElementCollector(m_doc).OfClass(typeof(TextNoteType));

                foreach (TextNoteType t in fecTextNoteType)
                {
                    if (t.Name.Contains("3/32"))
                        markerSettings.TextTypeId = t.Id;
                }

                // Color settings
                AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings();
                Color red = new Color(255, 0, 0);
                Color blue = new Color(0, 0, 255);
                Color green = new Color(0, 255, 0);

                colorSettings.MaxColor = red;
                colorSettings.MinColor = blue;
                IList<AnalysisDisplayColorEntry> map = new List<AnalysisDisplayColorEntry> { new AnalysisDisplayColorEntry(green) };
                colorSettings.SetIntermediateColors(map);

                AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings();
                legendSettings.NumberOfSteps = 6;
                legendSettings.Rounding = 1.0;
                legendSettings.ShowDataDescription = true;
                legendSettings.ShowLegend = true;

                // Yet another FilteredElementCollector
                FilteredElementCollector fecLegendText = new FilteredElementCollector(m_doc);
                ICollection<Element> elementCol = fecLegendText.OfClass(typeof(TextNoteType)).ToElements();
                var textElements = from element in fecLegendText
                                   where element.Name == "LegendText"
                                   select element;

                if (textElements.Count() > 0)
                {
                    TextNoteType textType = textElements.Cast<TextNoteType>().ElementAt<TextNoteType>(0);
                    legendSettings.TextTypeId = textType.Id;
                }
                // Create AnalysisDisplayStyle
                analysisDisplayStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(m_doc, "KTMStyle", markerSettings, colorSettings, legendSettings);
            }
            else
            {
                analysisDisplayStyle = displayStyle.Cast<AnalysisDisplayStyle>().ElementAt<AnalysisDisplayStyle>(0);
            }

            // Set current view AnalysisDisplayStyleId to newly created
            m_doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

            return true;
        }

        /// <summary>
        /// Actual method to perform the calculations at each point
        /// in a loop based on light fixtures in space and adds up
        /// illuminance falling on each point.
        /// </summary>
        /// <param name="pointToCalc"></param>
        /// <param name="lightFixtures">List of lights in space</param>
        /// <param name="CalcPlaneHeight">Space workplane height (2.5ft)</param>
        /// <returns>Total illuminance falling on each point</returns>
        private double CalcFCAtPoint(XYZ pointToCalc, List<LightFixture> lightFixtures, double CalcPlaneHeight)
        {
        	double result = 0; //Use to accumulate total light falling on point for each light fixutre.
        	
            // Create modified calc point using the CalcPlaneHeight of room as 'Z'
            XYZ modifiedPoint = new XYZ(pointToCalc.X, pointToCalc.Y, CalcPlaneHeight);
            foreach (LightFixture lf in lightFixtures)
            {
                XYZ lightLocation = lf.LocationPoint.Point;                
                double distToPoint = lightLocation.DistanceTo(modifiedPoint);
                double angle = lightLocation.AngleTo(modifiedPoint);
                double cosAngle = Math.Cos(angle);

                // Transforms?
                Options opts = new Options();
                // Why use this?
                //GeometryElement geoElement = fi.get_Geometry(opts);
                // end Transforms

                // TODO: Update to a better formula?
                /* Calculation Formula */
                /*
                * Lumens = lamp lumens defined in type parameters
                * angle = angle to point (already expressed in Cos needed) 
                * no need to adjust futher I found out
                * 
                * distToPoint = trig distance calculated from light
                * source to point              
                */
                
                double valueAtPoint = Math.Pow((lf.Lumens * angle) / Math.Pow(distToPoint, 2), 1);
				result += valueAtPoint;                
            }
            
            return result;
        }

        /// <summary>
        /// Gets the topmost face of the space geometry
        /// *Basically to use as a pseudo calc face to present
        /// the analysis results in the view
        /// </summary>
        /// <param name="space"></param>
        /// <returns>Topmost face of space geometry</returns>
        private Face GetFace(RoomSpace space)
        {
            Options options = new Options();
            options.ComputeReferences = true;

            GeometryElement ge = space.ParentSpaceObject.get_Geometry(options);
            //Array geoArray = ge.ToArray();
            //Solid sol = geoArray.GetValue(0) as Solid;          
            Solid sol = ge.ElementAt(0) as Solid;
            Face face = sol.Faces.get_Item(1) as Face;

            return face;
        }
    }
}
