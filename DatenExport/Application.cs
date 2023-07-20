using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Autodesk.Revit.Attributes;
using System.Drawing;

namespace DatenExport
{
    [Transaction(TransactionMode.Manual)]
    public class Application : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {

            RibbonPanel panel = application.CreateRibbonPanel(Tab.AddIns, "RevitConnect");

            Assembly assembly = Assembly.GetExecutingAssembly();
            // assembly path
            string assemblyPath = assembly.Location;

            PushButton pushButton = panel.AddItem(new PushButtonData("RevitConnect", "RevitConnect", assemblyPath, "DatenExport.ShowRevitConnect")) as PushButton;

            pushButton.Image = Convert(Resource.DatenExport);
            pushButton.LargeImage = Convert(Resource.DatenExport);

            return Result.Succeeded;
        }

        // get embedded images from assembly resources
        public ImageSource GetResourceImage(Assembly assembly, string imageName)
        {
            try
            {
                // bitmap stream to construct bitmap frame
                Stream resource = assembly.GetManifestResourceStream(imageName);
                // return image data
                return BitmapFrame.Create(resource);
            }
            catch
            {
                return null;
            }
        }

        public BitmapImage Convert(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
    }
}
