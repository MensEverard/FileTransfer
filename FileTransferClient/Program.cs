using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FileTransferClient
{

    class MagazineSettings
    {
        public string Name { get; set; }
        public int KkmCount { get; set; }
        public bool Otovatka { get; set; }
        public string Transport { get; set; }

    }

    class ProgramSettings
    {
        public string TransferSide { get; set; }
        public string RootPath { get; set; }
        public List<MagazineSettings> MagazineSettingsList { get; set; }
    }


    class Program : Form
    {
        private NotifyIcon TrayIcon;
        private ContextMenuStrip TrayMenu;


        public Program()
        {
            // Create a simple form without a visible window
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;

            // Initialize the system tray icon
            TrayIcon = new NotifyIcon();
            TrayIcon.Text = "System Tray App";

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
            // Start the application and run the form
            Application.Run(new Program());
        }


        private void CloseApp()
        {
            // Clean up resources and exit the application
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
            if (File.Exists("MagazineSettings.json"))
            {
                programSettings = LoadMagSettingsFromJSON("MagazineSettings.json");
            }
            else
            {
                MagazineSettingsList = new List<MagazineSettings>{
                    new MagazineSettings { Name = "Asino", KkmCount = 1, Otovatka = true, Transport = "yd" }
                    , new MagazineSettings {Name = "Bakchar", KkmCount = 2, Otovatka = true, Transport = "ftp" }
                };

                programSettings = new ProgramSettings();
                programSettings.MagazineSettingsList = MagazineSettingsList;
                programSettings.RootPath = "D:\\VSProjects\\mag_obmen\\server_folders";
                programSettings.TransferSide = "Server";
                SaveMagSettingsToJSON(programSettings, "MagazineSettings.json");

                throw new FileNotFoundException("MagazineSettings.json not found");
            }

            //create timer for run StartTransfer
            Timer timer = new Timer();
            timer.Interval = 10000; // 10 seconds
            timer.Tick += (sender, e) => StartTransfer(programSettings);

            // Start the timer
            timer.Start();

        }


        private static void StartTransfer( ProgramSettings programSettings)
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

            //magUpload
            //еxport.txt        magOut     -> FTPOut    move
            //LoadResult.txt    magOut     -> FTPOut    copy ? if diff date

            //magDounload
            //import.txt        FTPin       -> magIn       move
            //in.flg            FTPin       -> magIn       move
            //SaveResult.txt    FTPOut      -> magOut      move
            //import.txt        FTPOtovatka -> MagOtovarka move
            //in.flg            FTPOtovatka -> MagOtovarka move
            for (int i = 0; i < magazineSettings.KkmCount; i++)
            {
                string localOutPath = programSettings.RootPath + "\\" + magazineSettings.Name + "\\Out" + ((i != 0) ? (i.ToString()) : "");
                string remoteOutPath = "/" + magazineSettings.Name + "/Out" + ((i != 0) ? (i.ToString()) : "");

                //Upload files
                TransferClient.UploadFiles(localOutPath + "\\LoadResult.txt", remoteOutPath + "/LoadResult.txt");
                TransferClient.UploadFiles(localOutPath + "\\еxport.txt", remoteOutPath + "/еxport.txt");
                File.Delete(localOutPath + "\\LoadResult.txt");
                File.Delete(localOutPath + "\\еxport.txt");


                //Download files
                string localInPath = programSettings.RootPath + "\\" + magazineSettings.Name + "\\In" + ((i != 0) ? (i.ToString()) : "");
                string remoteInPath = "/" + magazineSettings.Name + "/In" + ((i != 0) ? (i.ToString()) : "");

                if (File.Exists(localInPath + "\\in.flg") && File.Exists(localInPath + "\\import.txt"))
                {
                    TransferClient.DownloadFiles(remoteInPath + "/import.txt", localInPath + "\\import.txt");
                    TransferClient.DownloadFiles(remoteInPath + "/in.flg", localInPath + "\\in.flg");
                    //Remove file
                    TransferClient.DeleteFile(remoteInPath + "/import.txt");
                    TransferClient.DeleteFile(remoteInPath + "/in.txt");
                }

                TransferClient.DownloadFiles(remoteOutPath + "/SaveResult.txt", localOutPath + "\\SaveResult.txt");
            }

            if (magazineSettings.Otovatka)
            {
                string localOtovatkaPath = programSettings.RootPath + "\\" + magazineSettings.Name + "\\Otovatka";
                string remoteOtovatkaPath = "/" + magazineSettings.Name + "/Otovatka";

                TransferClient.DownloadFiles(remoteOtovatkaPath + "/import.txt", localOtovatkaPath + "\\import.txt");
                TransferClient.DownloadFiles(remoteOtovatkaPath + "/in.flg", localOtovatkaPath + "\\in.flg");
                //Remove file
                TransferClient.DeleteFile(remoteOtovatkaPath + "/import.txt");
                TransferClient.DeleteFile(remoteOtovatkaPath + "/in.txt");
            }
        }

        private static void ServerSideTransfer(ProgramSettings programSettings, MagazineSettings magazineSettings, IFileTransferClient TransferClient)
        {
            // server mode

            //serverUpload
            //import.txt        In ->       FTPin       move if in.flg
            //in.flg            In ->       FTPin       move
            //SaveResult.txt    Out ->      FTPOut      Copy ? if diff date
            //import.txt        Otovarka -> FTPOtovatka move if in.flg
            //in.flg            Otovarka -> FTPOtovatka move

            //serverDounload
            //еxport.txt FTPOut -Out move
            //LoadResult.txt FTPOut -Out move

            for (int i = 0; i < magazineSettings.KkmCount; i++)
            {
                //Upload files
                string localInPath = programSettings.RootPath + "\\" + magazineSettings.Name + "\\In" + ((i != 0) ? (i.ToString()) : "");
                string remoteInPath = "/" + magazineSettings.Name + "/In" + ((i != 0) ? (i.ToString()) : "");
                if (File.Exists(localInPath + "\\in.flg") && File.Exists(localInPath + "\\import.txt"))
                {
                    TransferClient.UploadFiles(localInPath + "\\import.txt", remoteInPath + "/import.txt");
                    TransferClient.UploadFiles(localInPath + "\\in.flg", remoteInPath + "/in.flg");
                    //Remove file
                    File.Delete(localInPath + "\\in.flg");
                    File.Delete(localInPath + "\\import.txt");
                }
                string localOutPath = programSettings.RootPath + "\\" + magazineSettings.Name + "\\Out" + ((i != 0) ? (i.ToString()) : "");
                string remoteOutPath = "/" + magazineSettings.Name + "/Out" + ((i != 0) ? (i.ToString()) : "");

                if (File.Exists(localOutPath + "\\SaveResult.txt"))
                {
                    if (!TransferClient.CompareFiles(remoteOutPath + "/SaveResult.txt", localOutPath + "\\SaveResult.txt"))
                    {
                        TransferClient.UploadFiles(localOutPath + "\\SaveResult.txt", remoteOutPath + "/SaveResult.txt"); continue;
                    }

                }
                //Download files
                TransferClient.DownloadFiles(remoteOutPath + "/LoadResult.txt", localOutPath + "\\LoadResult.txt");
                TransferClient.DownloadFiles(remoteOutPath + "/еxport.txt", localOutPath + "\\еxport.txt");
                //remove from ftp
                TransferClient.DeleteFile(remoteOutPath + "/LoadResult.txt");
                TransferClient.DeleteFile(remoteOutPath + "/еxport.txt");
            }

            if (magazineSettings.Otovatka)
            {
                string localOtovatkaPath = programSettings.RootPath + "\\" + magazineSettings.Name + "\\Otovatka";
                string remoteOtovatkaPath = "/" + magazineSettings.Name + "/Otovatka";

                if (File.Exists(localOtovatkaPath + "\\in.flg") && File.Exists(localOtovatkaPath + "\\import.txt"))
                {
                    TransferClient.UploadFiles(localOtovatkaPath + "\\import.txt", remoteOtovatkaPath + "/import.txt");
                    TransferClient.UploadFiles(localOtovatkaPath + "\\in.flg", remoteOtovatkaPath + "/in.flg");
                    //Remove file
                    File.Delete(localOtovatkaPath + "\\in.flg");
                    File.Delete(localOtovatkaPath + "\\import.txt");
                }
            }
        }
    }

}















