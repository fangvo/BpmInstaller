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
        public string DBBackupPath { get; set; }
        public string Name { get; set; }
        public string DistrPath { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string RedisDBNumber { get; set; }
        public string IisPort { get; set; }
        public bool NeedWorkInFS { get; set; }

        public void Start()
        {

            try
            {
                Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет распаковка zip архива");
                ZipFile.ExtractToDirectory(DistrPath, WorkFolderPath);

                Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет правка ConnectionStrings");
                SetConnectionStrings(Name, Login, Password);
                SetRedisConnectionStrings(RedisDBNumber);
                XmlStringEditor.EditConnectionStrings(WorkFolderPath, ConnectionString_DB, ConnectionString_Redis);
                Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет восстановление базы данных");
                DataBase.RestoreDB(Name, DBBackupPath, Login, Password);
                Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет настройка IIS");
                SetIis(Name, WorkFolderPath, IisPort);

                if (NeedWorkInFS)
                {
                    Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет правка Web.config");
                    XmlStringEditor.SetWebConfig1(WorkFolderPath, NeedWorkInFS);
                    Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет правка WorkspaceConsole.config");
                    XmlStringEditor.EditWebConfToWorkInFS(WorkFolderPath, ConnectionString_DB);
                    Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Идет копирование файлов PrepareWorkspaceConsole");
                    StartBatToWorkInFS(WorkFolderPath);
                }
                Dispatcher.CurrentDispatcher?.Invoke(StageChanged, "Работа завершена");
                OpenSite(IisPort);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Что-то пошло не так. \r\nДанные об ошибке сохранены в файле error.log директории программы.");
                File.WriteAllText("error.log", ex.Message);
            }
        }



        public void SetConnectionStrings(string dbName, string login, string password, string server = "", bool isDataSourceOnLocalhost=true)
        {
            if(isDataSourceOnLocalhost)
                ConnectionString_DB = "Data Source=" + Environment.MachineName + "; ";
            else
                ConnectionString_DB = "Data Source=" + server + "; ";
            ConnectionString_DB += "Initial Catalog=" + dbName + "; ";
            ConnectionString_DB += "Persist Security Info=True; ";
            ConnectionString_DB += "MultipleActiveResultSets=True; ";
            ConnectionString_DB += "MultipleActiveResultSets=True; ";
            ConnectionString_DB += "Pooling = true; ";
            ConnectionString_DB += "Max Pool Size = 100; ";
            ConnectionString_DB += "Async = true; ";
            ConnectionString_DB += "Connection Timeout=500; ";
            ConnectionString_DB += "User ID=" + login + "; ";
            ConnectionString_DB += "Password=" + password + "; ";
        }

        public void SetRedisConnectionStrings(string dbNumber, string host = "127.0.0.1")
        {
            ConnectionString_Redis = "host=" + host + "; ";
            ConnectionString_Redis += "db=" + dbNumber + "; ";
            ConnectionString_Redis += "port=6379; ";
            ConnectionString_Redis += "maxReadPoolSize=25; ";
            ConnectionString_Redis += "maxWritePoolSize=25; ";
        }

        

        private void SetIis(string name, string workFolder, string port)
        {
            ServerManager iisManager = new ServerManager();
            iisManager.ApplicationPools.Add(name);
            Site site = iisManager.Sites.Add(name, "http", "*:" + port + ":", workFolder);
            site.ApplicationDefaults.ApplicationPoolName = name;
            string appPath = workFolder + @"Terrasoft.WebApp\";
            Application application = site.Applications.Add("/0", appPath);
            iisManager.CommitChanges();
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

    }
}
