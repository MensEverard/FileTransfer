**��������� ���������������� ������**  
```json
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
}  

MagSettings.json      
[  
    {  
        "Name": "Asino",  
        "KkmNumber": 1,  
        "InFolderName": "In",  
        "OutFolderName": "Out",  
        "Otovarka": true,  
        "Transport": "yd"  
    },  
    {  
        "Name": "Asino",  
        "KkmNumber": 2,  
        "InFolderName": "In1",  
        "OutFolderName": "Out1",  
        "Otovarka": false,  
        "Transport": "ftp"  
    }  
]  
