using System;
using System.IO;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Web.Administration;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Windows.Threading;


namespace BpmInstaller
{

    class Installer
    {
        public Installer()
        {
        }

        public delegate void InstellEvent(string log);
        public InstellEvent StageChanged;
        public string ConnectionString_DB { get; set; }
        public string ConnectionString_Redis { get; set; }
        public string WorkFolderPath { get; set; }
        public string ServerName { get; set; }
        public string DBBackupPath { get; set; }
        public string Name { get; set; }
        public string DistrPath { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string RedisDBNumber { get; set; }
        public string IisPort { get; set; }
        public bool NeedWorkInFS { get; set; }
        public bool NeedRestoreBD { get; set; }
        public bool UseWindowsAuthorization { get; set; }

        public void Start()
        {
            try
            {
                Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет распаковка zip архива");
                if (!Directory.Exists(WorkFolderPath))
                    ZipFile.ExtractToDirectory(DistrPath, WorkFolderPath);
                Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет правка ConnectionStrings");
                SetConnectionStrings(Name, Login, Password, ServerName);
                SetRedisConnectionStrings(RedisDBNumber);
                XmlStringEditor.EditConnectionStrings(WorkFolderPath, ConnectionString_DB, ConnectionString_Redis);
                if (NeedRestoreBD)
                {
                    Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет восстановление базы данных");
                    if (UseWindowsAuthorization)
                    {
                        DataBase.Restore(ServerName, Name, DBBackupPath);
                    }
                    else
                    {
                        DataBase.Restore(ServerName, Name, DBBackupPath, Login, Password);
                    }
                }

                if (NeedWorkInFS)
                {
                    Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет правка Web.config");
                    XmlStringEditor.SetWebConfig1(WorkFolderPath, NeedWorkInFS);
                    Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет правка WorkspaceConsole.config");
                    XmlStringEditor.EditWebConfToWorkInFS(WorkFolderPath, ConnectionString_DB);
                    Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет копирование файлов PrepareWorkspaceConsole");
                    StartBatToWorkInFS(WorkFolderPath);
                }

                Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет настройка IIS");
                IISHelper.SetIis(Name, WorkFolderPath, IisPort);

                Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Работа завершена");
                OpenSite(IisPort);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Что-то пошло не так. \r\nДанные об ошибке сохранены в файле error.log директории программы.");
                File.WriteAllText("error.log", $"{ex.Message}\r\n{ex.StackTrace}");
            }
        }



        public void SetConnectionStrings(string dbName, string login = "", string password = "", string server = "")
        {
            ConnectionString_DB = "Data Source=" + server + "; ";
            ConnectionString_DB += "Initial Catalog=" + dbName + "; ";
            ConnectionString_DB += "Persist Security Info=True; ";
            ConnectionString_DB += "MultipleActiveResultSets=True; ";
            ConnectionString_DB += "MultipleActiveResultSets=True; ";
            ConnectionString_DB += "Pooling = true; ";
            ConnectionString_DB += "Max Pool Size = 100; ";
            ConnectionString_DB += "Async = true; ";
            ConnectionString_DB += "Connection Timeout=500; ";
            if (UseWindowsAuthorization)
            {
                ConnectionString_DB += "Integrated Security = SSPI;";
            }
            else
            {
                ConnectionString_DB += "User ID=" + login + "; ";
                ConnectionString_DB += "Password=" + password + "; ";
            }

        }

        public void SetRedisConnectionStrings(string dbNumber, string host = "127.0.0.1")
        {
            ConnectionString_Redis = "host=" + host + "; ";
            ConnectionString_Redis += "db=" + dbNumber + "; ";
            ConnectionString_Redis += "port=6379; ";
            ConnectionString_Redis += "maxReadPoolSize=25; ";
            ConnectionString_Redis += "maxWritePoolSize=25; ";
        }





        private void OpenSite(string port, string host = "http://127.0.0.1")
        {
            string site = host + ":" + port;
            System.Diagnostics.Process.Start(site);
        }


        private void StartBatToWorkInFS(string workFolder)
        {
            string prepareBatFilePath = workFolder + @"Terrasoft.WebApp\DesktopBin\WorkspaceConsole\PrepareWorkspaceConsole";
            if (Environment.Is64BitOperatingSystem)
            {
                prepareBatFilePath += ".x64.bat";
            }
            else
            {
                prepareBatFilePath += ".x86.bat";
            }

            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = prepareBatFilePath;
            proc.StartInfo.WorkingDirectory = workFolder + @"Terrasoft.WebApp\DesktopBin\WorkspaceConsole";
            proc.Start();
        }

        public void MoveDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd('\\', ' ');
            var targetPath = target.TrimEnd('\\', ' ');
            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    File.Move(file, targetFile);
                }
            }
            Directory.Delete(source, true);
        }

    }
}
