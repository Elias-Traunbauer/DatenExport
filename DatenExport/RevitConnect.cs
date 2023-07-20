using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DatenExport
{
    public partial class RevitConnect : Form
    {
        private const string basePath = "/api/Revit/";
        private readonly Config config;

        public UIDocument doc;

        private readonly Timer timer = new Timer();

        public RevitConnect()
        {
            timer.Tick += Timer_Tick;

            config = Config.Load();
            config.Save();
            InitializeComponent();
            textBoxAddress.Text = config.ApiAddress;
            textBoxPort.Text = config.ApiPort;
            SetButtonsEnabled(false, false, false);

            timer.Interval = 1000;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckIfAuthenticated();
            CheckIfActionRequested();
        }

        private void CheckIfActionRequested()
        {
            
        }

        public Action<string> MessageCallback { get; set; }

        private void ButtonTestClick(object sender, EventArgs e)
        {
            HttpClient httpClient = new HttpClient();

            httpClient.BaseAddress = new Uri("http://" + textBoxAddress.Text + ":" + textBoxPort.Text);
            testLabel.Text = "Testing...";
            Stopwatch t = new Stopwatch();
            t.Start();
            try
            {
                httpClient.GetAsync("api/Connectivity").ContinueWith(x =>
                {
                    Invoke(new MethodInvoker(() =>
                    {
                        if (x.Exception != null)
                        {
                            testLabel.Text = "Error when checking: " + x.Exception.InnerExceptions[0].GetType().ToString();
                            SetButtonsEnabled(login: true, logout: false, openpanel: false);
                            return;
                        }
                        t.Stop();
                        var result = x.Result;
                        if (result.IsSuccessStatusCode)
                        {
                            testLabel.Text = "Success! Ping: " + t.ElapsedMilliseconds + "ms";
                            t.Stop();
                        }
                        else
                        {
                            testLabel.Text = "Failed: " + result.StatusCode;
                            t.Stop();
                        }
                    }));
                });
            }
            catch (Exception ex)
            {
                testLabel.Text = "Failed: " + ex.ToString();
            }
        }

        private void TabPage2_Click(object sender, EventArgs e)
        {

        }

        private void RevitConnect_Load(object sender, EventArgs e)
        {
            CheckIfAuthenticated();
            MessageCallback("checked");
        }

        private void SetButtonsEnabled(bool? login = null, bool? logout = null, bool? openpanel = null)
        {
            if (login != null)
            {
                btnLogin.Enabled = (bool)login;
            }

            if (logout != null)
            {
                btnLogout.Enabled = (bool)logout;
            }

            if (openpanel != null)
            {
                btnOpenPanel.Enabled = (bool)openpanel;
            }
        }

        private void CheckIfAuthenticated()
        {
            labelStatus.Text = "Checking for login...";
            HttpClient httpClient = new HttpClient();
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, "GetAuthenticationStatus");
            msg.Content = JsonContent.Create(new
            {
                Token = config.ApiSecret
            });
            httpClient.BaseAddress = new Uri("http://" + textBoxAddress.Text + ":" + textBoxPort.Text + basePath);
            httpClient.SendAsync(msg).ContinueWith(x =>
            {
                Invoke(new MethodInvoker(() =>
                {
                    if (x.Exception != null)
                    {
                        labelStatus.Text = "Error when checking: " + x.Exception.InnerExceptions[0].GetType().ToString();
                        SetButtonsEnabled(login: true, logout: false, openpanel: false);
                        return;
                    }
                    var result = x.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        labelStatus.Text = "Logged in, check the panel to continue";
                        SetButtonsEnabled(login: false, logout: true, openpanel: true);
                        ProvideProject();
                    }
                    else
                    {
                        labelStatus.Text = "Not logged in, please login to continue";
                        SetButtonsEnabled(login: true, logout: false, openpanel: false);
                    }
                }));
            });

        }

        private void ProvideProject()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://" + textBoxAddress.Text + ":" + textBoxPort.Text + basePath);

            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, "ProjectInfo");
            msg.Content = JsonContent.Create(new
            {
                Token = config.ApiSecret,
                Info = new
                {
                    doc.Document.ProjectInformation.Address,
                    doc.Document.ProjectInformation.Author,
                    doc.Document.ProjectInformation.BuildingName,
                    doc.Document.ProjectInformation.ClientName,
                    doc.Document.ProjectInformation.IssueDate,
                    doc.Document.ProjectInformation.Name,
                    doc.Document.ProjectInformation.Number,
                    doc.Document.ProjectInformation.OrganizationDescription,
                    doc.Document.ProjectInformation.OrganizationName,
                    doc.Document.ProjectInformation.Status,
                }

            });
            httpClient.BaseAddress = new Uri("http://" + textBoxAddress.Text + ":" + textBoxPort.Text + basePath);
            httpClient.SendAsync(msg).ContinueWith(x =>
            {
                if (x.Exception != null)
                {
                    Invoke(new MethodInvoker(() =>
                    {
                        MessageBox.Show("Failed to provide project to server: " + x.Exception.InnerExceptions[0].ToString());
                    }));
                    return;
                }
            });

        }

        private void BtnSaveConfig_Click(object sender, EventArgs e)
        {
            config.ApiAddress = textBoxAddress.Text;
            config.ApiPort = textBoxPort.Text;
            config.Save();
            CheckIfAuthenticated();
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            CheckIfAuthenticated();
        }

        private void TabControl1_SelectedIndexChanged(object sender, TabControlEventArgs e)
        {
            CheckIfAuthenticated();
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            // get secret from server
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://" + textBoxAddress.Text + ":" + textBoxPort.Text + basePath);

            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, "AuthenticationToken");

            httpClient.SendAsync(msg).ContinueWith(x =>
            {
                dynamic json = JsonConvert.DeserializeObject(x.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult());
                config.ApiSecret = json.token;
                config.Save();
                Invoke(new MethodInvoker(() =>
                {
                    Clipboard.SetText(config.ApiSecret);
                }));
            });

            Process.Start("http://localhost:3000/revit/linkAccount");
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            config.ApiSecret = "";
            config.Save();
        }

        private void BtnOpenPanel_Click(object sender, EventArgs e)
        {
            Process.Start("http://localhost:3000/revit/task");
        }
    }

    class Config
    {
        public string ApiAddress { get; set; }
        public string ApiPort { get; set; }
        public string ApiSecret { get; set; }

        public static Config Load()
        {
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitConnect", "config.json");

            Config config = new Config()
            {
                ApiAddress = "127.0.0.1",
                ApiPort = "5094",
                ApiSecret = ""
            };
            if (File.Exists(configPath))
            {
                var stream = File.OpenRead(configPath);
                TextReader reader = new StreamReader(stream);

                config = (Config)JsonSerializer.CreateDefault().Deserialize(reader, typeof(Config));
                stream.Close();
            }
            if (config == null)
            {
                config = new Config()
                {
                    ApiAddress = "127.0.0.1",
                    ApiPort = "5094",
                    ApiSecret = ""
                };
            }
            return config;
        }

        public void Save()
        {
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitConnect", "config.json");
            if (!Directory.Exists(Path.GetDirectoryName(configPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
            }
            var stream = File.OpenWrite(configPath);
            TextWriter writer = new StreamWriter(stream);
            Newtonsoft.Json.JsonSerializer.CreateDefault().Serialize(writer, this, typeof(Config));
            writer.Flush();
            writer.Close();
            stream.Close();
        }
    }
}
