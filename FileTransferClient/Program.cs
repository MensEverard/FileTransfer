using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace FileTransferClient
{

    class MagazineSettings
    {
        public string Name { get; set; }
        public int KkmCount { get; set; }
        public bool Otovarka { get; set; }
        public string Transport { get; set; }



    }

    class ProgramSettings
    {
        public string TransferSide { get; set; } //Mag|Server
        public string RootPath { get; set; }

        public int ScanInterval { get; set; }
        public List<MagazineSettings> MagazineSettingsList { get; set; }
    }


    class Program : Form
    {
        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayMenu;
        private Timer timer = new Timer();


        public Program()
        {
            // Create a simple form without a visible window
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;


            // Initialize the system tray icon
            TrayIcon = new NotifyIcon();
            TrayIcon.Text = "File Transfer Client";

            // Load an icon resource for the tray
            TrayIcon.Icon = TrayIcon.Icon = SystemIcons.Asterisk; // new System.Drawing.Icon("icon.ico");

            TrayMenu = new ContextMenuStrip();

            // Create a context menu with an "Exit" command
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += ExitMenuItem_Click;
            TrayMenu.Items.Add(exitMenuItem);

            // Assign the context menu to the tray icon
            TrayIcon.ContextMenuStrip = TrayMenu;

            // Add a handler for the mouse click event
            TrayIcon.MouseClick += TrayIcon_MouseClick;

            TrayIcon.Visible = true;

            OnStart();

        }

        static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                MyLogger.Log.Error("An unhandled exception occurred:");
                MyLogger.Log.Error("Exception Message: " + ex.Message);
                MyLogger.Log.Error("Exception Stack Trace: " + ex.StackTrace);

                // Log variable values
                if (ex.Data.Count > 0)
                {
                    MyLogger.Log.Error("Variable values:");
                    foreach (var key in ex.Data.Keys)
                    {
                        MyLogger.Log.Error($"{key}: {ex.Data[key]}");
                    }
                }
            }
        }

        static ProgramSettings LoadMagSettingsFromJSON(string jsonFilePath)
        {
            // Load FTP settings from a JSON file
            using (StreamReader file = File.OpenText(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (ProgramSettings)serializer.Deserialize(file, typeof(ProgramSettings));
            }
        }

        static void SaveMagSettingsToJSON(ProgramSettings magSettings, string jsonFilePath)
        {
            using (StreamWriter file = File.CreateText(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, magSettings);
            }
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            // Check if right mouse button was clicked
            if (e.Button == MouseButtons.Right)
            {
                // Show the context menu at the mouse position
                TrayMenu.Show(Control.MousePosition);
            }
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            // Clean up resources and exit the application
            CloseApp();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            // Register an unhandled exception handler
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            // Start the application and run the form
            Application.Run(new Program());
        }


        private void CloseApp()
        {
            // Clean up resources and exit the application
            timer.Stop();
            timer.Dispose();
            TrayIcon.Visible = false;
            TrayIcon.Dispose();
            TrayMenu.Dispose();
            Application.Exit();
        }


        private void OnStart()
        {
            // Load FTP settings from a JSON file
            List<MagazineSettings> MagazineSettingsList;
            ProgramSettings programSettings;

            //сдлать проверку наличие файла MagazineSettings.json
            //загрузить из него значения
            if (File.Exists("ProgramSettings.json"))
            {
                programSettings = LoadMagSettingsFromJSON("ProgramSettings.json");
            }
            else
            {
                MagazineSettingsList = new List<MagazineSettings>{
                    new MagazineSettings { Name = "Asino", KkmCount = 1, Otovarka = true, Transport = "ftp" }
                    , new MagazineSettings {Name = "Bakchar", KkmCount = 2, Otovarka = false, Transport = "ftp" }
                };

                programSettings = new ProgramSettings();
                programSettings.MagazineSettingsList = MagazineSettingsList;
                programSettings.RootPath = "D:\\VSProjects\\mag_obmen\\server_folders";
                programSettings.TransferSide = "Server";
                SaveMagSettingsToJSON(programSettings, "ProgramSettings.json");

                throw new FileNotFoundException("ProgramSettings.json not found");
            }

            TrayIcon.Text = TrayIcon.Text + " " + programSettings.TransferSide;

            //create timer for run StartTransfer

            timer.Interval = programSettings.ScanInterval * 1000; // 10 seconds
            timer.Tick += (sender, e) => StartTransfer(programSettings);

            // Start the timer
            timer.Start();

        }


        private static void StartTransfer(ProgramSettings programSettings)
        {
            MyLogger.Log.Info("StartTransfer()");

            foreach (MagazineSettings magazineSettings in programSettings.MagazineSettingsList)
            {
                IFileTransferClient client;
                if (magazineSettings.Transport == "ftp")
                {
                    client = new FtpClient();
                }
                else if (magazineSettings.Transport == "yd")
                {
                    client = new YDClient();
                }
                else
                {
                    throw new Exception($"Unknown transport {magazineSettings.Transport}");
                }



                if (programSettings.TransferSide == "Server")
                {
                    ServerSideTransfer(programSettings, magazineSettings, client);
                }
                else
                {
                    MagasineSideTransfer(programSettings, magazineSettings, client);
                }
            }
        }

        private static void MagasineSideTransfer(ProgramSettings programSettings, MagazineSettings magazineSettings, IFileTransferClient TransferClient)
        {
            //magazine mode

            for (int i = 1; i <= magazineSettings.KkmCount; i++)
            {
                string localOutPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, "Out" + ((i != 1) ? (i.ToString()) : ""));
                string remoteOutPath = Path.Combine("/", magazineSettings.Name, "Out" + ((i != 1) ? (i.ToString()) : "")).Replace("\\", "/");
                string localInPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, "In" + ((i != 1) ? (i.ToString()) : ""));
                string remoteInPath = Path.Combine("/", magazineSettings.Name, "In" + ((i != 1) ? (i.ToString()) : "")).Replace("\\", "/");

                if (!File.Exists(localInPath + "\\in.flg") && File.Exists(localInPath + "\\import.txt"))
                {
                    string secondLine = File.ReadLines(localInPath + "\\import.txt").ElementAtOrDefault(1);
                    if (secondLine != null && secondLine.Contains("@"))
                        UploadImportAFile(localInPath, remoteInPath);
                }

                string LoadResultName = "LoadResult001.txt";
                string SaveResultName = "SaveResult001.txt";

                if (File.Exists(localOutPath + $"\\{LoadResultName}"))
                    if (!TransferClient.CompareFiles(remoteOutPath + $"/{LoadResultName}", localOutPath + $"\\{LoadResultName}"))
                        TransferClient.UploadFiles(localOutPath + $"\\{LoadResultName}", remoteOutPath + $"/{LoadResultName}");

                if (File.Exists(localOutPath + $"\\{SaveResultName}"))
                    if (!TransferClient.CompareFiles(remoteOutPath + $"/{SaveResultName}", localOutPath + $"\\{SaveResultName}"))
                        TransferClient.UploadFiles(localOutPath + $"\\{SaveResultName}", remoteOutPath + $"/{SaveResultName}");

                UploadExportFile(localOutPath, remoteOutPath);

                DownloadImportFile(remoteInPath, localInPath);

            }

            if (magazineSettings.Otovarka)
            {
                string localOtovarkaPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, "Otovarka");
                string remoteOtovarkaPath = Path.Combine("/", magazineSettings.Name, "Otovarka").Replace("\\", "/");
                DownloadImportFile(remoteOtovarkaPath, localOtovarkaPath);
            }

            void DownloadImportFile(string remotePath = null, string localPath = null)
            {
                string LocalInFile = Path.Combine(localPath, "in.flg");
                string LocalImportFile = Path.Combine(localPath, "import.txt");
                string LocalImportTempFile = Path.Combine(localPath, "import.tmp");

                string RemoteInFile = Path.Combine(remotePath, "in.flg").Replace("\\", "/");
                string RemoteImportFile = Path.Combine(remotePath, "import.txt").Replace("\\", "/");

                if (!File.Exists(LocalImportFile))
                {
                    if (TransferClient.ExistsFile(RemoteImportFile) && TransferClient.ExistsFile(RemoteInFile))
                    {
                        TransferClient.DownloadFiles(RemoteImportFile, LocalImportTempFile);
                        if (File.Exists(LocalImportTempFile))
                            File.Move(LocalImportTempFile, LocalImportFile);
                        TransferClient.DownloadFiles(RemoteInFile, LocalInFile);

                        TransferClient.DeleteFile(RemoteImportFile);
                        TransferClient.DeleteFile(RemoteInFile);
                    }
                }
            }

            void UploadExportFile(string localPath = null, string remotePath = null)
            {
                string LocalExportFile = Path.Combine(localPath, "export.txt");
                string RemoteExportFile = Path.Combine(remotePath, "export.txt").Replace("\\", "/");
                string RemoteExportTempFile = Path.Combine(remotePath, "export.tmp").Replace("\\", "/");

                if (File.Exists(LocalExportFile))
                {
                    TransferClient.UploadFiles(LocalExportFile, RemoteExportTempFile);
                    TransferClient.RenameFile(RemoteExportTempFile, RemoteExportFile);
                    File.Delete(LocalExportFile);
                }
            }

            void UploadImportAFile(string localPath = null, string remotePath = null)
            {
                string LocalImportFile = Path.Combine(localPath, "import.txt");

                string RemoteImportFile = Path.Combine(remotePath, "import@.txt").Replace("\\", "/");
                string RemoteImportTempFile = Path.Combine(remotePath, "import@.tmp").Replace("\\", "/");

                TransferClient.UploadFiles(LocalImportFile, RemoteImportTempFile);
                TransferClient.RenameFile(RemoteImportTempFile, RemoteImportFile);

                File.Delete(LocalImportFile);

            }
        }

        private static void ServerSideTransfer(ProgramSettings programSettings, MagazineSettings magazineSettings, IFileTransferClient TransferClient)
        {
            // server mode

            for (int i = 1; i <= magazineSettings.KkmCount; i++)
            {
                //Upload files
                string localInPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, "In" + ((i != 1) ? (i.ToString()) : ""));
                string remoteInPath = Path.Combine("/", magazineSettings.Name, "In" + ((i != 1) ? (i.ToString()) : "")).Replace("\\", "/");
                UploadImportFile(localInPath, remoteInPath);

                string localOutPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, "Out" + ((i != 1) ? (i.ToString()) : ""));
                string remoteOutPath = Path.Combine("/", magazineSettings.Name, "Out" + ((i != 1) ? (i.ToString()) : "")).Replace("\\", "/");

                string LoadResultName = "LoadResult001.txt";
                string SaveResultName = "SaveResult001.txt";

                if (!File.Exists(localOutPath + $"\\{LoadResultName}") ||  !TransferClient.CompareFiles(remoteOutPath + $"/{LoadResultName}", localOutPath + $"\\{LoadResultName}"))
                    TransferClient.DownloadFiles(remoteOutPath + $"/{LoadResultName}", localOutPath + $"\\{LoadResultName}");

                if (!File.Exists(localOutPath + $"\\{SaveResultName}") || !TransferClient.CompareFiles(remoteOutPath + $"/{SaveResultName}", localOutPath + $"\\{SaveResultName}"))
                    TransferClient.DownloadFiles(remoteOutPath + $"/{SaveResultName}", localOutPath + $"\\{SaveResultName}");

                string exportName = "export.txt";
                string exportTempName = "export.tmp";

                TransferClient.DownloadFiles(remoteOutPath + $"/{exportName}", localOutPath + $"\\{exportTempName}");

                if (File.Exists(localOutPath + $"\\{exportTempName}"))
                {
                    if (File.Exists(localOutPath + $"\\{exportName}"))
                        File.Delete(localOutPath + $"\\{exportName}");
                    File.Move(localOutPath + $"\\{exportTempName}", localOutPath + $"\\{exportName}");
                    TransferClient.DeleteFile(remoteOutPath + $"/{exportName}");
                }

                string importAName = "import@.txt";
                string importATempName = "import@.tmp";

                TransferClient.DownloadFiles(remoteInPath + $"/{importAName}", localInPath + $"\\{importATempName}");

                if (File.Exists(localInPath + $"\\{importATempName}"))
                {
                    if (File.Exists(localInPath + $"\\{importAName}"))
                        File.Delete(localInPath + $"\\{importAName}");
                    File.Move(localInPath + $"\\{importATempName}", localInPath + $"\\{importAName}");
                    TransferClient.DeleteFile(remoteInPath + $"/{importAName}");
                }
            }

            if (magazineSettings.Otovarka)
            {
                string localOtovarkaPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, "Otovarka");
                string remoteOtovarkaPath = Path.Combine("/", magazineSettings.Name, "Otovarka").Replace("\\", "/");

                UploadImportFile(localOtovarkaPath, remoteOtovarkaPath);
            }


            void UploadImportFile(string localPath = null, string remotePath = null)
            {
                string LocalInFile = Path.Combine(localPath, "in.flg");
                string LocalImportFile = Path.Combine(localPath, "import.txt");

                string RemoteInFile = Path.Combine(remotePath, "in.flg").Replace("\\", "/");
                string RemoteImportFile = Path.Combine(remotePath, "import.txt").Replace("\\", "/");
                string RemoteImportTempFile = Path.Combine(remotePath, "import.tmp").Replace("\\", "/");


                if (File.Exists(LocalInFile) && File.Exists(LocalImportFile))
                {
                    TransferClient.UploadFiles(LocalImportFile, RemoteImportTempFile);
                    TransferClient.RenameFile(RemoteImportTempFile, RemoteImportFile);
                    TransferClient.UploadFiles(LocalInFile, RemoteInFile);

                    //Remove file
                    File.Delete(LocalInFile);
                    File.Delete(LocalImportFile);
                }
            }

        }
    }

}















