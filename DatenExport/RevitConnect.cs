using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace DatenExport
{
    public partial class RevitConnect : Form
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

        public Autodesk.Revit.UI.UIDocument revitDocument;

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
        bool uploadRunning = false;
        private void CheckIfActionRequested()
        {
            if (uploadRunning)
            {
                return;
            }
            httpClient.SendAsync(RequestWithToken("GetTODO")).ContinueWith((x) =>
            {
                if (x.Exception != null)
                {
                    return;
                }
                if (x.Result.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return;
                }
                string result = x.Result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                dynamic json = JsonConvert.DeserializeObject(result);
                string actionStatus = json.actionStatus; // "idle" | "upload"

                if (actionStatus == "upload" && !uploadRunning)
                {
                    uploadRunning = true;
                    Invoke(() => MessageBox.Show("Upload requested"));
                    UploadDocument().ContinueWith(y =>
                    {
                        if (y.Exception != null)
                        {
                            MessageBox.Show("Error while uploading: " + x.Exception.InnerExceptions[0].GetType().ToString());
                            return;
                        }
                        MessageBox.Show("Upload successful");
                    });
                }
            });
        }

        private async Task UploadDocument()
        {
            // go through all elements and put them in chunks of 100 elements and upload them

            List<Autodesk.Revit.DB.Element> AllElem = new Autodesk.Revit.DB.FilteredElementCollector(revitDocument.Document)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent()
                    .Where(e => e.IsPhysicalElement())
                    .ToList<Autodesk.Revit.DB.Element>();

            for (int i = 0; i < AllElem.Count() / 100 + 1; i++)
            {
                List<Autodesk.Revit.DB.Element> chunk = AllElem.Skip(i * 100).Take(100).ToList<Autodesk.Revit.DB.Element>();

                var res = from element in chunk
                          select new
                          {
                              Name = element.Name,
                              RevitElementId = element.Id.IntegerValue,
                              Parameters = element.GetOrderedParameters().Select(p => new
                              {
                                  p.Definition.Name,
                                  Value = p.AsValueString()
                              })
                          };

                var content = JsonContent.Create(new
                {
                    Token = configuration.ApiSecret,
                    Elements = res
                });
                var result = await httpClient.PostAsync("Upload", content);
                string contentRes = await result.Content.ReadAsStringAsync();
            }
            httpClient.SendAsync(RequestWithToken("FinishUpload")).Wait();
            uploadRunning = false;
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
            if (ProjectInfo == null)
            {
                List<Autodesk.Revit.DB.Element> AllElem = new Autodesk.Revit.DB.FilteredElementCollector(revitDocument.Document)
                        .WhereElementIsNotElementType()
                        .WhereElementIsViewIndependent()
                        .Where(e => e.IsPhysicalElement())
                        .ToList<Autodesk.Revit.DB.Element>();

                ProjectInfo = new
                {
                    revitDocument.Document.ProjectInformation.Address,
                    revitDocument.Document.ProjectInformation.Author,
                    revitDocument.Document.ProjectInformation.BuildingName,
                    revitDocument.Document.ProjectInformation.ClientName,
                    revitDocument.Document.ProjectInformation.IssueDate,
                    revitDocument.Document.ProjectInformation.Name,
                    revitDocument.Document.ProjectInformation.Number,
                    revitDocument.Document.ProjectInformation.OrganizationDescription,
                    revitDocument.Document.ProjectInformation.OrganizationName,
                    revitDocument.Document.ProjectInformation.Status,
                    revitDocument.Document.ProjectInformation.UniqueId,
                    TotalElements = AllElem.Count()
                };
            }
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, "ProjectInfo");
            msg.Content = JsonContent.Create(new
            {
                Token = configuration.ApiSecret,
                Info = ProjectInfo

            });
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

