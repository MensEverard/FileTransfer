namespace FileTransferClient
{
    internal interface IFileTransferClient
    {
        void UploadFiles(string localPath, string remotePath);
        void RenameFile(string sourceRemoteFilePath, string newRemoteFilePath);

        void DownloadFiles(string remotePath, string localPath);
        void DeleteFile(string remotePath);
        bool CompareFiles(string remotePath, string localPath);

        bool ExistsFile(string remotePath);


    }

}















