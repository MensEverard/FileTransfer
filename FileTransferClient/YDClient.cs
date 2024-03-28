using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace FileTransferClient
{
    class YDSettings
    {
        public string AccessToken { get; set; }
        public string rootPath { get; set; }
    }

    class YDClient : IFileTransferClient
    {
        private YDSettings Settings;
        private string api_url = "https://cloud-api.yandex.net/v1/disk/";


        public bool ExistsFile(string remotePath)
        {
            string urlParameters;
            if (string.IsNullOrEmpty(remotePath))
            {
                return false;
            }

            using (HttpClient client = new HttpClient { BaseAddress = new Uri(this.api_url) })
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, api_url))
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", this.Settings.AccessToken);
                    string pth = WebUtility.UrlEncode($"{this.Settings.rootPath}{remotePath}"); //

                    urlParameters = $"resources?path={pth}";

                    HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    return false;
                }
            }
        }

        public void UploadFiles(string localPath, string remotePath)
        {
            string urlParameters;

            using (HttpClient client = new HttpClient { BaseAddress = new Uri(this.api_url) })
            {


                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", this.Settings.AccessToken);
                string pth = WebUtility.UrlEncode($"{this.Settings.rootPath}{remotePath}"); //

                urlParameters = $"resources/upload?path={pth}&overwrite=true";

                MyLogger.Log.Debug($"Запрос ссылки для отправки файла {this.Settings.rootPath}{remotePath}");

                HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
                if (response.IsSuccessStatusCode)
                {
                    var dataObjects = response.Content.ReadAsStringAsync().Result;
                    DownloadInfo downloadInfo = JsonConvert.DeserializeObject<DownloadInfo>(dataObjects);


                    MyLogger.Log.Debug("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                    MyLogger.Log.Debug("Upload link: " + downloadInfo.href);
                    MyLogger.Log.Debug("Starting upload...");


                    var fileStream = File.OpenRead(localPath);
                    var content = new StreamContent(fileStream);
                    content.Headers.Add("Content-Type", "application/octet-stream");
                    response = client.PutAsync(downloadInfo.href, content).Result;

                    if ((int)response.StatusCode == 201)
                    {

                        MyLogger.Log.Debug("Upload complete");
                    }
                    else
                    {
                        MyLogger.Log.Debug("Bad {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                    }

                }
                else
                {
                    MyLogger.Log.Debug("BAD {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
            }

        }

        public class DownloadInfo
        {
            public string href { get; set; }
            public string method { get; set; }
            public bool templated { get; set; }
        }

        public void RenameFile(string sourceRemoteFilePath, string newRemoteFilePath)
        {
            string urlParameters;
            using (HttpClient client = new HttpClient { BaseAddress = new Uri(this.api_url) })
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", this.Settings.AccessToken);

                urlParameters = $"resources/move?from={this.Settings.rootPath}{WebUtility.UrlEncode(sourceRemoteFilePath)}&path={this.Settings.rootPath}{WebUtility.UrlEncode(newRemoteFilePath)}&overwrite=true";

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    //{ "from", WebUtility.UrlEncode($"{this.Settings.rootPath}{sourceRemoteFilePath}" )},
                    //{ "path", WebUtility.UrlEncode($"{this.Settings.rootPath}{newRemoteFilePath}" )},
                    //{ "overwrite", "true" }
                });



                MyLogger.Log.Debug("Переименование файла " + sourceRemoteFilePath);

                HttpResponseMessage response = client.PostAsync(urlParameters, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    MyLogger.Log.Debug("Файл переименован " + newRemoteFilePath);
                }
                else
                {
                    MyLogger.Log.Debug("BAD {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
            }

        }

        public void DownloadFiles(string remotePath, string localPath)
        {
            string urlParameters;

            using (HttpClient client = new HttpClient { BaseAddress = new Uri(this.api_url) })
            {


                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", this.Settings.AccessToken);

                string pth = WebUtility.UrlEncode($"{this.Settings.rootPath}{remotePath}"); //
                urlParameters = $"resources/download?path={pth}";


                MyLogger.Log.Debug("Запрос ссылки для скачивания файла " + remotePath);

                HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.


                if (response.IsSuccessStatusCode)
                {
                    var dataObjects = response.Content.ReadAsStringAsync().Result;

                    DownloadInfo downloadInfo = JsonConvert.DeserializeObject<DownloadInfo>(dataObjects);

                    MyLogger.Log.Debug("Получена ссылка для загрузки " + downloadInfo.href);


                    using (var fileStream = File.Create(localPath))
                    {

                        var content = new StreamContent(fileStream);
                        content.Headers.Add("Content-Type", "application/octet-stream");

                        // download file from downloadLink.href with GET method and save to localPath

                        response = client.GetAsync(downloadInfo.href).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            MyLogger.Log.Debug("Загрузка файла " + downloadInfo.href);
                            fileStream.Write(response.Content.ReadAsByteArrayAsync().Result, 0, response.Content.ReadAsByteArrayAsync().Result.Length);
                            MyLogger.Log.Debug("OK {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                        }

                        else
                        {
                            MyLogger.Log.Debug("Bad {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                        }
                    }

                }
                else
                {
                    MyLogger.Log.Debug("BAD {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
            }
        }


        public void DeleteFile(string remotePath)
        {
            string urlParameters;

            using (HttpClient client = new HttpClient { BaseAddress = new Uri(this.api_url) })
            {

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", this.Settings.AccessToken);

                string pth = WebUtility.UrlEncode($"{this.Settings.rootPath}{remotePath}"); //
                urlParameters = $"resources?path={pth}&permanently=true";

                MyLogger.Log.Debug("Удаление файла " + remotePath);

                HttpResponseMessage response = client.DeleteAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
                if ((int)response.StatusCode == 204)
                {

                    MyLogger.Log.Debug("OK {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
                else
                {
                    MyLogger.Log.Debug("BAD {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
            }
        }


        public bool CompareFiles(string remotePath, string localPath)
        {

            string urlParameters;

            using (HttpClient client = new HttpClient { BaseAddress = new Uri(this.api_url) })
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", this.Settings.AccessToken);

                string pth = WebUtility.UrlEncode($"{this.Settings.rootPath}{remotePath}"); //
                urlParameters = $"resources?path={pth}&fields=md5";

                MyLogger.Log.Debug("Сравнение файла " + remotePath);

                HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
                if ((int)response.StatusCode == 200)
                {
                    string LocalMD = CalculateMD5(localPath);

                    var dataObjects = response.Content.ReadAsStringAsync().Result;

                    dynamic jsonObj = JsonConvert.DeserializeObject(dataObjects);
                    string RemoteMD = jsonObj.md5;

                    if (LocalMD == RemoteMD)
                    {
                        MyLogger.Log.Debug("Файлы совпадают {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                        return true;
                    }
                    else
                    {
                        MyLogger.Log.Debug("Файлы не совпадают {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                        return false;
                    }

                }
                else
                {
                    MyLogger.Log.Debug("BAD {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
                return false;
            }
        }

        public string CalculateMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public YDClient()
        {
            if (File.Exists("YDSettings.json"))
            {
                this.Settings = LoadSettingsFromJSON("YDSettings.json");
            }
            else
            {
                this.Settings = new YDSettings();
                this.Settings.AccessToken = "past your access token here";
                SaveSettingsToJSON(this.Settings, "YDSettings.json");

                throw new FileNotFoundException("YDSettings.json not found");
            }
        }

        private YDSettings LoadSettingsFromJSON(string jsonFilePath)
        {
            // Load FTP settings from a JSON file
            using (StreamReader file = File.OpenText(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (YDSettings)serializer.Deserialize(file, typeof(YDSettings));
            }
        }
        void SaveSettingsToJSON(YDSettings Settings, string jsonFilePath)
        {
            using (StreamWriter file = File.CreateText(jsonFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, Settings);
            }
        }

    }

}















