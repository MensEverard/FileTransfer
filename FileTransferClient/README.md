FTPSettings.json - ��������� ����������� � FTP
{
    "Server": "127.0.0.1",
    "Username": "FTPusername",
    "Password": "FTPpassword",
    "rootPath": "/mag_obmen"
}

YDSettings.json - ��������� ����������� � Yndex API
{
    "AccessToken": "you access token",
    "rootPath": "/mag_obmen"
}


ProgramSettings.json
{
    "TransferSide": "Mag",  (Mag - ������ �� ������� ��������
                            Server - ������ �� ������� �������)

    "RootPath": "D:\\VSProjects\\mag_obmen\\mag_folders",

    "ScanInterval": 10, (������������� �������� ���������)
    
    "MagazineSettingsList": [
    {
        "Name": "Asino",
        "KkmCount": 1,
        "Otovatka": true,
        "Transport": "ftp"
        },
    {
        "Name": "Bakchar",
        "KkmCount": 2,
        "Otovatka": false,
        "Transport": "ftp"
    }
    ]
}