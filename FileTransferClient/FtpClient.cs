using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Runtime;
using System.Threading;

namespace FileTransferClient
{

    class FTPSettings
    {
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string rootPath { get; set; }
    }
    class FtpClient : IFileTransferClient
    {
        private FTPSettings Settings;
        public FtpClient()
        {
            if (File.Exists("FTPSettings.json"))
            {
                this.Settings = LoadSettingsFromJSON("FTPSettings.json");
            }
            else
            {
                this.Settings = new FTPSettings();
                this.Settings.Server = "ftp.example.com";
                this.Settings.Username = "username";
                this.Settings.Password = "password";
                this.Settings.rootPath = "/mag_obmen";

                SaveSettingsToJSON(this.Settings, "FTPSettings.json");

                throw new FileNotFoundException("FTPSettings.json not found");
            }
        }


        public bool ExistsFile(string remotePath)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{this.Settings.Server}{this.Settings.rootPath}{remotePath}");
                request.Credentials = new NetworkCredential(this.Settings.Username, this.Settings.Password);
                request.Method = WebRequestMethods.Ftp.ListDirectory;

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    return true;
                }
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable || response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailableOrBusy )
                {
                    return false;
                }
                else if (response.StatusCode == FtpStatusCode.EnteringPassive)
                {
                    MyLogger.Log.Info($"({((FtpWebResponse)ex.Response).StatusDescription} - retrying in 1 second...");
                    Thread.Sleep(1000); // Wait for 1 second
                    return ExistsFile(remotePath);
                }
                else
                {
                    MyLogger.Log.Error(((FtpWebResponse)ex.Response).StatusDescription);
                    return false;
                }
            }
        }


        public void DownloadFiles(string remotePath, string localPath)
        {
            // Download files from the FTP server
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(this.Settings.Username, this.Settings.Password);
                    client.DownloadFile($"ftp://{this.Settings.Server}{this.Settings.rootPath}{remotePath}", localPath);
                }
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable || response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailableOrBusy)
                {
                    return;
                }
                else if (response.StatusCode == FtpStatusCode.EnteringPassive)
                {
                    MyLogger.Log.Info($"({((FtpWebResponse)ex.Response).StatusDescription} - retrying in 1 second...");
                    Thread.Sleep(1000); // Wait for 1 second
                    this.DownloadFiles(remotePath, localPath);
                }
                else
                {
                    MyLogger.Log.Error(((FtpWebResponse)ex.Response).StatusDescription);
                    throw;
                }

            }
        }


        public void DeleteFile(string remotePath)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{this.Settings.Server}{this.Settings.rootPath}{remotePath}");
            request.Credentials = new NetworkCredential(this.Settings.Username, this.Settings.Password);
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            response.Close();
        }


        public void RenameFile(string sourceRemoteFilePath, string newRemoteFilePath)
        {
            // Create an FTP request
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"ftp://{this.Settings.Server}{this.Settings.rootPath}{sourceRemoteFilePath}");
            request.Method = WebRequestMethods.Ftp.Rename;
            request.RenameTo = $"{this.Settings.rootPath}{newRemoteFilePath}";
            request.Credentials = new NetworkCredential(this.Settings.Username, this.Settings.Password);

            // Send the request and get the response
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            // Check the response status
            if (response.StatusCode == FtpStatusCode.CommandOK)
            {
                Console.WriteLine("File renamed successfully.");
            }
            else
            {
                Console.WriteLine("Failed to rename file. Status: " + response.StatusDescription);
            }

            // Close the response
            response.Close();
        }

        public void UploadFiles(string localPath, string remotePath)
        {
            // Upload files to the FTP server
            if (!File.Exists(localPath))
            {
                return;
            }
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(this.Settings.Username, this.Settings.Password);
                    client.UploadFile($"ftp://{this.Settings.Server}{this.Settings.rootPath}{remotePath}", WebRequestMethods.Ftp.UploadFile, localPath);
                }
            }
            catch (WebException ex)
            {
                FtpWebResponse response = (FtpWebResponse)ex.Response;
                if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable || response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailableOrBusy)
                {
                    return;
                }
                else if (response.StatusCode == FtpStatusCode.EnteringPassive || response.StatusCode == FtpStatusCode.OpeningData)
                {
                    MyLogger.Log.Info($"({((FtpWebResponse)ex.Response).StatusDescription} - retrying in 1 second...");
                    Thread.Sleep(1000); // Wait for 1 second
                    UploadFiles(localPath, remotePath);
                }
                else
                {
                    MyLogger.Log.Error(((FtpWebResponse)ex.Response).StatusDescription);
                    throw;
                }
            }
        }

        public bool CompareFiles(string remotePath, string localPath)
        {
            if (!File.Exists(localPath)) { return false; }

            byte[] localFileBytes = File.ReadAllBytes(localPath);
            byte[] ftpFileBytes = this.DownloadData(remotePath);

            if (AreByteArraysEqual(localFileBytes, ftpFileBytes))
            {
                return true;
            }
            return false;
        }

        byte[] DownloadData(string remotePath)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(this.Settings.Username, this.Settings.Password);
                    return client.DownloadData($"ftp://{this.Settings.Server}{this.Settings.rootPath}/{remotePath}");
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    FtpWebResponse response = (FtpWebResponse)ex.Response;
                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable || response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailableOrBusy)
                    {
                        return new byte[0];
                    }
                    else if (response.StatusCode == FtpStatusCode.EnteringPassive)
                    {
                        MyLogger.Log.Info($"({((FtpWebResponse)ex.Response).StatusDescription} - retrying in 1 second...");
                        Thread.Sleep(1000); // Wait for 1 second
                        return DownloadData(remotePath);
                    }
                    else
                    {
                        MyLogger.Log.Error(((FtpWebResponse)ex.Response).StatusDescription);
                        return new byte[0];
                    }
                }
                MyLogger.Log.Error(((FtpWebResponse)ex.Response).StatusDescription);
                return new byte[0];

            }
        }

        FTPSettings LoadSettingsFromJSON(string jsonFilePath)
        {
            // Load FTP settings from a JSON file
            using (StreamReader file = File.OpenText(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (FTPSettings)serializer.Deserialize(file, typeof(FTPSettings));
            }
        }

        //create save settings to json
        void SaveSettingsToJSON(FTPSettings ftpSettings, string jsonFilePath)
        {
            using (StreamWriter file = File.CreateText(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, ftpSettings);
            }
        }

        bool AreByteArraysEqual(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
            {
                return false;
            }

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }

            return true;
        }

    }

}















