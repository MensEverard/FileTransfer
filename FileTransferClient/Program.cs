using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileTransferClient
{

    class MagazineSettings
    {
        public string Name { get; set; }
        public int KkmNumber { get; set; }
        public string InFolderName { get; set; }
        public string OutFolderName { get; set; }
        public bool Otovarka { get; set; }
        public string Transport { get; set; }
    }

    class ProgramSettings
    {
        public string TransferSide { get; set; } //Mag|Server
        public string RootPath { get; set; }

        public int ScanInterval { get; set; }

    }


    class Program : Form
    {
        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayMenu;
        private ToolStripMenuItem showLogMenuItem;

        //private Timer timer = new Timer();
        private static System.Threading.Timer _timer;


        public Program()
        {

            // Create a simple form without a visible window
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;


            // Initialize the system tray icon
            TrayIcon = new NotifyIcon();
            TrayIcon.Text = "File Transfer";

            // Load an icon resource for the tray
            TrayIcon.Icon = Properties.Resources.iconfinder_servertransfer_4417112_116623;


            TrayMenu = new ContextMenuStrip();


            showLogMenuItem = new ToolStripMenuItem("Show Log");
            showLogMenuItem.Click += ShowLogMenuItem_Click;
            TrayMenu.Items.Add(showLogMenuItem);

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

        private void ShowLogMenuItem_Click(object sender, EventArgs e)
        {
            MyLogger.logWindow.Show($"{TrayIcon.Text} Logs");
            //MyLogger.logWindow.SetTitle();
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


        static List<MagazineSettings> LoadMagSettingsFromJSON(string jsonFilePath)
        {
            using (StreamReader file = File.OpenText(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (List<MagazineSettings>)serializer.Deserialize(file, typeof(List<MagazineSettings>));
            }
        }

        static void SaveMagSettingsToJSON(List<MagazineSettings> magSettings, string jsonFilePath)
        {
            using (StreamWriter file = File.CreateText(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, magSettings);
            }
        }


        static ProgramSettings LoadProgSettingsFromJSON(string jsonFilePath)
        {
            // Load FTP settings from a JSON file
            using (StreamReader file = File.OpenText(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (ProgramSettings)serializer.Deserialize(file, typeof(ProgramSettings));
            }
        }

        static void SaveProgSettingsToJSON(ProgramSettings progSettings, string jsonFilePath)
        {
            using (StreamWriter file = File.CreateText(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, progSettings);
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

            _timer.Dispose();
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
                programSettings = LoadProgSettingsFromJSON("ProgramSettings.json");
            }
            else
            {
                programSettings = new ProgramSettings();
                programSettings.RootPath = "D:\\VSProjects\\mag_obmen\\server_folders";
                programSettings.TransferSide = "Server";
                SaveProgSettingsToJSON(programSettings, "ProgramSettings.json");
                throw new FileNotFoundException("ProgramSettings.json not found");
            }

            if (File.Exists("MagSettings.json"))
            {
                MagazineSettingsList = LoadMagSettingsFromJSON("MagSettings.json");
            }
            else
            {
                MagazineSettingsList = new List<MagazineSettings>{
                    new MagazineSettings { Name = "Asino", KkmNumber = 1, Otovarka = true, InFolderName = "In", OutFolderName = "Out", Transport = "ftp" }
                    , new MagazineSettings {Name = "Bakchar", KkmNumber = 2, Otovarka = false,InFolderName = "In1", OutFolderName = "Out1", Transport = "ftp" }
                };

                SaveMagSettingsToJSON(MagazineSettingsList, "MagSettings.json");
                throw new FileNotFoundException("MagSettings.json not found");
            }

            TrayIcon.Text = TrayIcon.Text + " " + programSettings.TransferSide;

            //create timer for run StartTransfer

            //timer.Interval = programSettings.ScanInterval * 1000; // 10 seconds
            //timer.Tick += (sender, e) => StartTransfer(programSettings, MagazineSettingsList);

            //// Start the timer
            //timer.Start();


            int intervalInMilliseconds = programSettings.ScanInterval * 1000; // Convert seconds to milliseconds

            _timer = new System.Threading.Timer(async (state) =>
            {
                await Task.Run(() => StartTransfer(programSettings, MagazineSettingsList));
            }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(intervalInMilliseconds));


        }


        private static void StartTransfer(ProgramSettings programSettings, List<MagazineSettings> MagazineSettingsList)
        {
            MyLogger.Log.Debug("Transfer");

            foreach (MagazineSettings magazineSettings in MagazineSettingsList)
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
                    throw new Exception($"Unknown transport {magazineSettings.Transport} need to be ftp or yd");
                }

                MyLogger.Log.Debug($"Start Transfer {magazineSettings.Name} {magazineSettings.Transport}");


                if (programSettings.TransferSide == "Server")
                {
                    ServerSideTransfer(programSettings, magazineSettings, client);

                }
                else
                {
                    MagasineSideTransfer(programSettings, magazineSettings, client);
                }

                MyLogger.Log.Debug("");
                Application.DoEvents();
            }
        }

        private static void MagasineSideTransfer(ProgramSettings programSettings, MagazineSettings magazineSettings, IFileTransferClient TransferClient)
        {
            //magazine mode

            string localOutPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, magazineSettings.OutFolderName);
            string remoteOutPath = Path.Combine("/", magazineSettings.Name, magazineSettings.OutFolderName).Replace("\\", "/");
            string localInPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, magazineSettings.InFolderName);
            string remoteInPath = Path.Combine("/", magazineSettings.Name, magazineSettings.InFolderName).Replace("\\", "/");

            MyLogger.Log.Debug("Check Folders");
            if (!CheckFolders(magazineSettings.Name, localOutPath, remoteOutPath, localInPath, remoteInPath, TransferClient))
                return;

            MyLogger.Log.Debug("Check IN Files");
            if (!File.Exists(localInPath + "\\in.flg") && File.Exists(localInPath + "\\import.txt"))
            {
                MyLogger.Log.Debug("Check import.txt for @");
                string secondLine = File.ReadLines(localInPath + "\\import.txt").ElementAtOrDefault(1);
                if (secondLine != null && secondLine.Contains("@"))
                {
                    MyLogger.Log.Debug("Upload import@.txt");
                    UploadImportAFile(localInPath, remoteInPath);
                }
            }

            string LoadResultName = "LoadResult001.txt";
            string SaveResultName = "SaveResult001.txt";

            MyLogger.Log.Debug($"Compare Files {LoadResultName}");
            if (File.Exists(localOutPath + $"\\{LoadResultName}"))
                if (!TransferClient.CompareFiles(remoteOutPath + $"/{LoadResultName}", localOutPath + $"\\{LoadResultName}"))
                {
                    MyLogger.Log.Debug($"Upload {LoadResultName}");
                    TransferClient.UploadFiles(localOutPath + $"\\{LoadResultName}", remoteOutPath + $"/{LoadResultName}");
                }

            MyLogger.Log.Debug($"Compare Files {SaveResultName}");
            if (File.Exists(localOutPath + $"\\{SaveResultName}"))
                if (!TransferClient.CompareFiles(remoteOutPath + $"/{SaveResultName}", localOutPath + $"\\{SaveResultName}"))
                {
                    MyLogger.Log.Debug($"Upload {SaveResultName}");
                    TransferClient.UploadFiles(localOutPath + $"\\{SaveResultName}", remoteOutPath + $"/{SaveResultName}");
                }


            MyLogger.Log.Debug("Upload OUT export.txt");
            UploadExportFile(localOutPath, remoteOutPath);

            MyLogger.Log.Debug("Download IN import.txt");
            DownloadImportFile(remoteInPath, localInPath);

            if (magazineSettings.Otovarka)
            {

                string localOtovarkaPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, "Otovarka");
                string remoteOtovarkaPath = Path.Combine("/", magazineSettings.Name, "Otovarka").Replace("\\", "/");

                MyLogger.Log.Debug("Check Otovarka Folders");
                bool notExistsFolders = false;
                if (!Directory.Exists(localOtovarkaPath))
                {
                    MyLogger.Log.Error($"Local In Path for {magazineSettings.Name} not exists {localOtovarkaPath}");
                    notExistsFolders = true;
                }

                if (!TransferClient.ExistsFile(remoteOtovarkaPath))
                {
                    MyLogger.Log.Error($"Remote Out Path for {magazineSettings.Name} not exists {remoteOtovarkaPath}");
                    notExistsFolders = true;
                }


                if (!notExistsFolders)
                {
                    MyLogger.Log.Debug("Download Otovarka Files");
                    DownloadImportFile(remoteOtovarkaPath, localOtovarkaPath);
                }
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

            string localInPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, magazineSettings.InFolderName);
            string remoteInPath = Path.Combine("/", magazineSettings.Name, magazineSettings.InFolderName).Replace("\\", "/");
            string localOutPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, magazineSettings.OutFolderName);
            string remoteOutPath = Path.Combine("/", magazineSettings.Name, magazineSettings.OutFolderName).Replace("\\", "/");
            

            MyLogger.Log.Debug("Check Folders");
            if (!CheckFolders(magazineSettings.Name, localOutPath, remoteOutPath, localInPath, remoteInPath, TransferClient))
                return;


            MyLogger.Log.Debug("Upload IN Files");
            UploadImportFile(localInPath, remoteInPath);


            string LoadResultName = "LoadResult001.txt";
            string SaveResultName = "SaveResult001.txt";

            MyLogger.Log.Debug($"Check OUT Files {LoadResultName} {SaveResultName}");
            if (!File.Exists(localOutPath + $"\\{LoadResultName}") || !TransferClient.CompareFiles(remoteOutPath + $"/{LoadResultName}", localOutPath + $"\\{LoadResultName}"))
            {
                MyLogger.Log.Debug($"Download Files {LoadResultName}");
                TransferClient.DownloadFiles(remoteOutPath + $"/{LoadResultName}", localOutPath + $"\\{LoadResultName}");
            }


            MyLogger.Log.Debug($"Check OUT Files {SaveResultName}");
            if (!File.Exists(localOutPath + $"\\{SaveResultName}") || !TransferClient.CompareFiles(remoteOutPath + $"/{SaveResultName}", localOutPath + $"\\{SaveResultName}"))
            {
                MyLogger.Log.Debug($"Download OUT Files {SaveResultName}");
                TransferClient.DownloadFiles(remoteOutPath + $"/{SaveResultName}", localOutPath + $"\\{SaveResultName}");
            }

            string exportName = "export.txt";
            string exportTempName = "export.tmp";

            MyLogger.Log.Debug($"Download OUT Files {exportName}");
            TransferClient.DownloadFiles(remoteOutPath + $"/{exportName}", localOutPath + $"\\{exportTempName}");

            MyLogger.Log.Debug($"Check OUT Files {exportTempName}");
            if (File.Exists(localOutPath + $"\\{exportTempName}"))
            {
                if (File.Exists(localOutPath + $"\\{exportName}"))
                    File.Delete(localOutPath + $"\\{exportName}");
                File.Move(localOutPath + $"\\{exportTempName}", localOutPath + $"\\{exportName}");

                TransferClient.DeleteFile(remoteOutPath + $"/{exportName}");
            }

            string importAName = "import@.txt";
            string importATempName = "import@.tmp";

            MyLogger.Log.Debug($"Download IN Files {importAName}");
            TransferClient.DownloadFiles(remoteInPath + $"/{importAName}", localInPath + $"\\{importATempName}");

            if (File.Exists(localInPath + $"\\{importATempName}"))
            {
                if (File.Exists(localInPath + $"\\{importAName}"))
                    File.Delete(localInPath + $"\\{importAName}");

                File.Move(localInPath + $"\\{importATempName}", localInPath + $"\\{importAName}");

                TransferClient.DeleteFile(remoteInPath + $"/{importAName}");
            }


            if (magazineSettings.Otovarka)
            {

                string localOtovarkaPath = Path.Combine(programSettings.RootPath, magazineSettings.Name, "Otovarka");
                string remoteOtovarkaPath = Path.Combine("/", magazineSettings.Name, "Otovarka").Replace("\\", "/");

                MyLogger.Log.Debug("Check Otovarka Folders");
                bool notExistsFolders = false;
                if (!Directory.Exists(localOtovarkaPath))
                {
                    MyLogger.Log.Error($"Local In Path for {magazineSettings.Name} not exists {localOtovarkaPath}");
                    notExistsFolders = true;
                }

                if (!TransferClient.ExistsFile(remoteOtovarkaPath))
                {
                    MyLogger.Log.Error($"Remote Out Path for {magazineSettings.Name} not exists {remoteOtovarkaPath}");
                    notExistsFolders = true;
                }


                if (!notExistsFolders)
                {
                    MyLogger.Log.Debug("Upload Otovarka Files");
                    UploadImportFile(localOtovarkaPath, remoteOtovarkaPath, false);
                }
            }


            void UploadImportFile(string localPath = null, string remotePath = null, bool createFlg = true)
            {
                string LocalInFile = Path.Combine(localPath, "in.flg");


                string LocalImportFile = Path.Combine(localPath, "import.txt");

                string RemoteInFile = Path.Combine(remotePath, "in.flg").Replace("\\", "/");
                string RemoteImportFile = Path.Combine(remotePath, "import.txt").Replace("\\", "/");
                string RemoteImportTempFile = Path.Combine(remotePath, "import.tmp").Replace("\\", "/");

                MyLogger.Log.Debug($"Check Files {LocalImportFile}");

                if (File.Exists(LocalImportFile))
                {
                    FileInfo fileInfo = new FileInfo(LocalImportFile);
                    if (fileInfo.Length != 0)
                    {
                        try
                        {
                            using (File.Open(LocalImportFile, FileMode.Open, FileAccess.Read, FileShare.None))
                            { }
                            if (createFlg)
                                File.WriteAllText(LocalInFile, "1", Encoding.UTF8);

                            if (File.Exists(LocalInFile))
                            {
                                MyLogger.Log.Debug($"Upload Files {RemoteInFile} {RemoteImportFile}");
                                TransferClient.UploadFiles(LocalImportFile, RemoteImportTempFile);
                                TransferClient.RenameFile(RemoteImportTempFile, RemoteImportFile);
                                TransferClient.UploadFiles(LocalInFile, RemoteInFile);

                                //Remove file
                                File.Delete(LocalInFile);
                                File.Delete(LocalImportFile);
                            }

                        }
                        catch (IOException)
                        { }

                    }
                }
            }



        }

        private static bool CheckFolders(string magName, string localOutPath, string remoteOutPath, string localInPath, string remoteInPath, IFileTransferClient TransferClient)
        {
            bool notExistsFolders = false;
            if (!Directory.Exists(localOutPath))
            {
                MyLogger.Log.Error($"Local Out Path for {magName} not exists {localOutPath}");
                notExistsFolders = true;
            }

            if (!Directory.Exists(localInPath))
            {
                MyLogger.Log.Error($"Local In Path for {magName} not exists {localInPath}");
                notExistsFolders = true;
            }

            Application.DoEvents();

            if (!TransferClient.ExistsFile(remoteOutPath))
            {
                MyLogger.Log.Error($"Remote Out Path for {magName} not exists {remoteOutPath}");
                notExistsFolders = true;
            }

            Application.DoEvents();

            if (!TransferClient.ExistsFile(remoteInPath))
            {
                MyLogger.Log.Error($"Remote In Path for {magName} not exists {remoteInPath}");
                notExistsFolders = true;
            }

            Application.DoEvents();

            return !notExistsFolders;

        }
    }



}















