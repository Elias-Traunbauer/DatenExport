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
    public class ExportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            StatusForm statusForm = null;
            Thread statusThread = null;
            try
            {
                UIApplication uiapp = commandData.Application;
                Document doc = uiapp.ActiveUIDocument.Document;


                string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

                var res = TaskDialog.Show("Confirmation", "Do you want to export all the elements in this revit project?", TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.Cancel, TaskDialogResult.Yes);

                if (res == TaskDialogResult.Yes)
                {
                    bool running = true;
                    void StopIfStopped()
                    {
                        if (!running)
                        {
                            throw new OperationCanceledException();
                        }
                    }

                    statusThread = new Thread(() =>
                    {
                        statusForm = new StatusForm();
                        statusForm.TopLevel = true;
                        statusForm.ShowDialog();
                    });
                    statusThread.SetApartmentState(ApartmentState.STA);
                    statusThread.Start();
                    while (statusForm == null)
                    {

                    }
                    statusForm.Canceled += (sender, e) =>
                    {
                        running = false;
                    };

                    HashSet<string> definitionNames = new HashSet<string>();

                    List<Element> AllElem = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .WhereElementIsViewIndependent()
                        .Where(e => e.IsPhysicalElement())
                        .ToList<Element>();

                    int count = 0;
                    double progressPerElement = 50d / AllElem.Count;
                    foreach (var revitElement in AllElem)
                    {
                        StopIfStopped();
                        if (count % 100 == 0)
                        {
                            statusForm.SetStatus((int)(count * progressPerElement), $"Collecting definitions [{count}/{AllElem.Count()}]");
                        }
                        count++;
                        foreach (Parameter parameter in revitElement.GetOrderedParameters())
                        {
                            if (
                                   parameter.Definition.ParameterGroup != BuiltInParameterGroup.INVALID
                                && parameter.HasValue
                                )
                            {
                                definitionNames.Add(parameter.Definition.Name);
                            }
                        }
                    }

                    string path = commonAppData + "\\revitExport.csv";
                    File.Delete(path);
                    FileStream fileStream = File.OpenWrite(path);

                    StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
                    TextWriter textWriter = TextWriter.Synchronized(streamWriter);

                    textWriter.Write("Name;Type;Details;");

                    // CSV Header
                    foreach (var definition in definitionNames)
                    {
                        textWriter.Write(definition + ";");
                    }

                    count = 0;
                    // CSV Data
                    foreach (var element in AllElem)
                    {
                        StopIfStopped();
                        if (count % 100 == 0)
                        {
                            statusForm.SetStatus(49 + (int)(count * progressPerElement), $"Writing data [{count}/{AllElem.Count()}]");
                        }
                        count++;
                        string line = Environment.NewLine + element.Name + ";" + element.GetType().Name + ";" + element.GetElementShape() + ";";

                        // list of the parameters joined -> if no parameter is found , a ";" is added

                        var result =
                            from definition in definitionNames
                            join parameter in element.GetOrderedParameters() on definition equals parameter.Definition.Name into gj
                            from subpet in gj.DefaultIfEmpty()
                            select subpet;

                        foreach (var d in result)
                        {
                            if (d == null)
                            {
                                line += ";";
                            }
                            else
                            {
                                line += d.AsValueString() + ";";
                            }
                        }

                        textWriter.Write(line);
                    }

                    textWriter.Close();
                    streamWriter.Close();
                    fileStream.Close();

                    statusForm.SetStatus(100, "Export ready at: " + path);
                }
                else
                {
                    return Result.Cancelled;
                }

                return Result.Succeeded;
            }
            catch (OperationCanceledException)
            {
                statusThread?.Join();
                return Result.Cancelled;
            }
            finally
            {

            }
        }
    }
}
