using Newtonsoft.Json;
using System;
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

        public void UploadFiles(string localPath, string remotePath)
        {
            string urlParameters;

            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(this.api_url)
            };


            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", this.Settings.AccessToken);
            urlParameters = $"resources/upload?path={this.Settings.rootPath}/{WebUtility.UrlEncode(remotePath)}&overwrite=true";

            MyLogger.Log.Warn($"Запрос ссылки для отправки файла {this.Settings.rootPath}/{remotePath}");

            HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            if (response.IsSuccessStatusCode)
            {
                var dataObjects = response.Content.ReadAsStringAsync().Result;
                DownloadInfo downloadInfo = JsonConvert.DeserializeObject<DownloadInfo>(dataObjects);

                
                MyLogger.Log.Info("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                MyLogger.Log.Info("Upload link: " + downloadInfo.href);
                MyLogger.Log.Info("Starting upload...");


                var fileStream = File.OpenRead(localPath);
                var content = new StreamContent(fileStream);
                content.Headers.Add("Content-Type", "application/octet-stream");
                response = client.PutAsync(downloadInfo.href, content).Result;

                if ((int)response.StatusCode == 201)
                {
                    
                    MyLogger.Log.Info("Upload complete");
                }
                else
                {
                    MyLogger.Log.Error("Bad {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }

            }
            else
            {
                MyLogger.Log.Error("BAD {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            client.Dispose();

        }

        public class DownloadInfo
        {
            public string href { get; set; }
            public string method { get; set; }
            public bool templated { get; set; }
        }


        public void DownloadFiles(string remotePath, string localPath)
        {
            string urlParameters;

            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(this.api_url)
            };

            
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", this.Settings.AccessToken);
            urlParameters = $"resources/download?path={this.Settings.rootPath}/{WebUtility.UrlEncode(remotePath)}";


            MyLogger.Log.Warn("Запрос ссылки для скачивания файла "+remotePath);

            HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            

            if (response.IsSuccessStatusCode)
            {
                var dataObjects = response.Content.ReadAsStringAsync().Result;
                
                DownloadInfo downloadInfo = JsonConvert.DeserializeObject<DownloadInfo>(dataObjects);

                MyLogger.Log.Warn("Получена ссылка для загрузки "+downloadInfo.href);


                var fileStream = File.OpenWrite(localPath);

                var content = new StreamContent(fileStream);
                content.Headers.Add("Content-Type", "application/octet-stream");

                // download file from downloadLink.href with GET method and save to localPath

                response = client.GetAsync(downloadInfo.href).Result;
                if (response.IsSuccessStatusCode)
                {
                    MyLogger.Log.Warn("Загрузка файла "+downloadInfo.href);
                    fileStream.Write(response.Content.ReadAsByteArrayAsync().Result, 0, response.Content.ReadAsByteArrayAsync().Result.Length);
                    MyLogger.Log.Warn("OK {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }

                else
                {
                    MyLogger.Log.Warn("Bad {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }

            }
            else
            {
                MyLogger.Log.Warn("BAD {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            client.Dispose();
        }


        public void DeleteFile(string remotePath)
        {
            string urlParameters;

            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(this.api_url)
            };

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", this.Settings.AccessToken);

            urlParameters = $"resources?path={this.Settings.rootPath}/{WebUtility.UrlEncode(remotePath)}&permanently=true";

            MyLogger.Log.Info("Удаление файла "+remotePath);

            HttpResponseMessage response = client.DeleteAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            if ((int)response.StatusCode == 204)
            {
                
                MyLogger.Log.Info("OK {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            else
            {
                MyLogger.Log.Error("BAD {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            client.Dispose();
        }


        public bool CompareFiles(string remotePath, string localPath)
        {

            string urlParameters;

            HttpClient client = new HttpClient
            {
                BaseAddress = new Uri(this.api_url)
            };

            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", this.Settings.AccessToken);

            urlParameters = $"resources?path={this.Settings.rootPath}/{WebUtility.UrlEncode(remotePath)}&fields=md5";

            MyLogger.Log.Info("Сравнение файла "+remotePath);

            HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            if ((int)response.StatusCode == 200)
            {
                string LocalMD = CalculateMD5(localPath);

                var dataObjects = response.Content.ReadAsStringAsync().Result;

                dynamic jsonObj = JsonConvert.DeserializeObject(dataObjects);
                string RemoteMD = jsonObj.md5;

                if (LocalMD == RemoteMD)
                {
                    MyLogger.Log.Info("Файлы совпадают {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                    return true;
                }
                else
                {
                    MyLogger.Log.Error("Файлы не совпадают {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                    return false;
                }

            }
            else
            {
                MyLogger.Log.Error("BAD {0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            client.Dispose();

            return false;
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















