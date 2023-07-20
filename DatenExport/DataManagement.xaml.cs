using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DatenExport
{
    /// <summary>
    /// Interaction logic for DataManagement.xaml
    /// </summary>
    public partial class DataManagement : Window
    {
        public DataManagement()
        {
            InitializeComponent();
            Category.ItemsSource = new List<string> { "AKH", "BIM", "BIM2", "BIM3" };
        }

        public string SelectedExportName { get; set; } = "Revit Export";
        public string SelectedCategory { get; set; } = "AKH";

        private void Button_Export_Click(object sender, RoutedEventArgs e)
        {
            GroupBox_Details.IsEnabled = false;
            GroupBox_Settings.IsEnabled = false;
            GroupBox_Status.IsEnabled = true;
        }
    }
}
