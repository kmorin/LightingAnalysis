using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace LightingAnalysis
{
    class SpaceSelectionFilter : ISelectionFilter
    {
        Document m_doc = null;

        public SpaceSelectionFilter(Document doc)
        {
            m_doc = doc;
        }

        public bool AllowElement(Element elem)
        {
            return elem is Autodesk.Revit.DB.Mechanical.Space;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }        
    }
}
