using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Forms;

namespace DatenExport
{
    public partial class RevitConnect : System.Windows.Forms.Form
    {
        private const string baseApiUrl = "/api/Revit/";
        private readonly Config configuration;
        private HttpClient httpClient;

        private HttpRequestMessage RequestWithToken(string path)
        {
            return new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new
                {
                    Token = configuration.ApiSecret
                })
            };
        }

        public UIDocument revitDocument;

        private readonly Timer networkTimer = new Timer();

        public RevitConnect()
        {
            networkTimer.Tick += Timer_Tick;

            configuration = Config.Load();
            configuration.Save();
            InitializeComponent();
            textBoxAddress.Text = configuration.ApiAddress;
            textBoxPort.Text = configuration.ApiPort;
            SetButtonsEnabled(false, false, false);

            networkTimer.Interval = 2000;

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
            testLabel.Text = "Testing...";
            Stopwatch t = new Stopwatch();
            t.Start();
            httpClient.GetAsync("api/Connectivity").ContinueWith(x =>
            {
                Invoke(() =>
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
                });
            });

        }

        private void RevitConnect_Load(object sender, EventArgs e)
        {
            MessageCallback("checked");
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://" + textBoxAddress.Text + ":" + textBoxPort.Text + baseApiUrl)
            };
            CheckIfAuthenticated();
            networkTimer.Start();
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

            httpClient.SendAsync(RequestWithToken("GetAuthenticationStatus")).ContinueWith(x =>
            {
                Invoke(() =>
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
                });
            });

        }
        private object ProjectInfo { get; set; }
        private void ProvideProject()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://" + textBoxAddress.Text + ":" + textBoxPort.Text + basePath);

            if (ProjectInfo == null)
            {
                List<Element> AllElem = new FilteredElementCollector(doc.Document)
                        .WhereElementIsNotElementType()
                        .WhereElementIsViewIndependent()
                        .Where(e => e.IsPhysicalElement())
                        .ToList<Element>();

                ProjectInfo = new
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
                    TotalElements = AllElem.Count()
                };
            }
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, "ProjectInfo");
            msg.Content = JsonContent.Create(new
            {
                Token = config.ApiSecret,
                Info = ProjectInfo

                })
            };
            httpClient.SendAsync(msg).ContinueWith(x =>
            {
                if (x.Exception != null)
                {
                    Invoke(() =>
                    {
                        MessageBox.Show("Failed to provide project to server: " + x.Exception.InnerExceptions[0].ToString());
                    });
                    return;
                }
            });

        }

        private void BtnSaveConfig_Click(object sender, EventArgs e)
        {
            configuration.ApiAddress = textBoxAddress.Text;
            configuration.ApiPort = textBoxPort.Text;
            configuration.Save();
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
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, "AuthenticationToken");

            httpClient.SendAsync(msg).ContinueWith(x =>
            {
                string result = x.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                dynamic json = JsonConvert.DeserializeObject(result);
                configuration.ApiSecret = json.token;
                configuration.Save();
                Invoke(() =>
                {
                    Clipboard.SetText(configuration.ApiSecret);
                });
            });

            Process.Start("http://localhost:3000/revit/linkAccount");
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            configuration.ApiSecret = "";
            configuration.Save();
        }

        private void BtnOpenPanel_Click(object sender, EventArgs e)
        {
            Process.Start("http://localhost:3000/revit/task");
        }

        private void Invoke(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() =>
                {
                    action();
                }));
            }
            else
            {
                action();
            }
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
