using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Lighting;

namespace LightingAnalysis
{
	class LightFixture
    {
        public double CandlePower { get; protected set; }
        public double Lumens { get; protected set; }
        public double Efficacy { get; protected set; }
        public double LightLossFactor { get; protected set; }
        public double CoefficientOfUtilization { get; protected set; }
        public double Elevation { get; protected set; }
        public LocationPoint LocationPoint { get; protected set; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="e">Accepts generic element (after processing) as input</param>
        public LightFixture(Element e, FamilyInstance fi)
        {
            // Set properties initially based on passed FamilyInstance          
            CandlePower = e.get_Parameter(BuiltInParameter.FBX_LIGHT_LIMUNOUS_INTENSITY).AsDouble();
            Lumens = e.get_Parameter(BuiltInParameter.FBX_LIGHT_LIMUNOUS_FLUX).AsDouble();
            Efficacy = e.get_Parameter(BuiltInParameter.FBX_LIGHT_EFFICACY).AsDouble();
            LightLossFactor = e.get_Parameter(BuiltInParameter.FBX_LIGHT_TOTAL_LIGHT_LOSS).AsDouble();
            CoefficientOfUtilization = e.get_Parameter(BuiltInParameter.RBS_ELEC_CALC_COEFFICIENT_UTILIZATION).AsDouble();
            
            Elevation = fi.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();

            LocationPoint = fi.Location as LocationPoint;

            // Future
            // TODO: Get photometric file and parse IES to provide correct                       
        }
    }
}
