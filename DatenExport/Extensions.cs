using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatenExport
{
    public static class Extensions

    {

        public static bool IsPhysicalElement(this Element e)
        {

            if (e.Category == null) return false;

            if (e.ViewSpecific) return false;

            // exclude specific unwanted categories

            if (((BuiltInCategory)e.Category.Id.IntegerValue) == BuiltInCategory.OST_HVAC_Zones) return false;

            //

            return e.Category.CategoryType == CategoryType.Model && e.Category.CanAddSubcategory;

        }

        public static string GetElementShape(this Element e)
        {
            string shape = "";

            ElementId tid = e.GetTypeId();

            if (ElementId.InvalidElementId != tid)
            {
                Document doc = e.Document;

                ElementType etyp = doc.GetElement(tid)
                  as ElementType;

                if (null != etyp)
                {
                    shape = etyp.FamilyName;
                }
            }
            return shape;
        }

    }
}
