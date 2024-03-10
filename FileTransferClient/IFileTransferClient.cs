namespace FileTransferClient
{
    internal interface IFileTransferClient
    {
        void UploadFiles(string localPath, string remotePath);
        void DownloadFiles(string remotePath, string localPath);
        void DeleteFile(string remotePath);
        bool CompareFiles(string remotePath, string localPath);
    }

}















