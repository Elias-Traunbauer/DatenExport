using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatenExport
{
    public partial class StatusForm : Form
    {
        public StatusForm()
        {
            InitializeComponent();
            buttonClipboard.Visible = false;
        }

        public event EventHandler<EventArgs> Canceled;

        public void SetStatus(int progress, string description)
        {
            Invoke(new MethodInvoker(() =>
            {
                progressBar.Value = progress;
                lblDescription.Text = description;
                if (progress == 100)
                {
                    buttonClipboard.Visible = true;
                    buttonCancel.Text = "Close";
                }
            }));
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void StatusForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (progressBar.Value != 100)
            {
                var res = MessageBox.Show("Are you sure that you want to cancel the export?", "Confirm", MessageBoxButtons.YesNo);
                if (res == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    Canceled?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void ButtonClipboard_Click(object sender, EventArgs e)
        {
            string path = lblDescription.Text.Split(new string[] { ": " }, StringSplitOptions.None)[1];

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "revit_export.csv";
            saveFileDialog.Filter = "CSV file (*.csv)|*.csv";
            var res = saveFileDialog.ShowDialog();
            if (res != DialogResult.OK)
            {
                return;
            }
            while (true)
            {
                try
                {
                    File.Copy(path, saveFileDialog.FileName, true);
                    lblDescription.Text = "Export saved at: " + saveFileDialog.FileName;
                    return;
                }
                catch (System.IO.IOException ex)
                {
                    var ress = MessageBox.Show("Could not save the file. If you are overwriting a file, make sure that file is not opened in any program.: " + ex.ToString(), "Error", MessageBoxButtons.RetryCancel);
                    if (ress == DialogResult.Cancel)
                    {
                        return;
                    }
                }
            }
        }
    }
}
