using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DatenExport
{
    [Transaction(TransactionMode.Manual)]
    public class ShowRevitConnect : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Action<string> action = (string msg) =>
            {
                TaskDialog.Show("RevitConnect", msg);
            };

            Thread statusThread = new Thread(() =>
            {
                RevitConnect connect = new RevitConnect
                {
                    MessageCallback = action,
                    doc = commandData.Application.ActiveUIDocument
                };
                connect.ShowDialog();
            });
            statusThread.SetApartmentState(ApartmentState.STA);
            statusThread.Start();

            return Result.Succeeded;
        }
    }
}
