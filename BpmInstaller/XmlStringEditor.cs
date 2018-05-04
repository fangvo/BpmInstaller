using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Web.Administration;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;


namespace BpmInstaller
{
    class XmlStringEditor
    {
        public static void EditConnectionStrings(string workFolderPath, string connectionString_DB, string connectionString_Redis)
        {
            string fileName = workFolderPath + @"ConnectionStrings.config";
            XDocument doc = XDocument.Load(fileName);

            //Чтение XML
            foreach (XElement el in doc.Root.Elements())
            {
                //Находим 1 строку БД
                var attrib = el.Attributes();
                var db = el.Attributes().FirstOrDefault(i => i.Value == "db");
                var redis = el.Attributes().FirstOrDefault(i => i.Value == "redis");
                if (db != null)
                {
                    db.NextAttribute.Value = connectionString_DB;
                }
                //Находим 2 строку БД
                else if (redis != null)
                {
                    redis.NextAttribute.Value = connectionString_Redis;
                }
            }
            //Запись XML
            doc.Save(fileName);
        }

        public static void SetWebConfig1(string workFolderPath, bool isWorkInFS = true)
        {
            string fileName = workFolderPath + @"Web.config";
            XDocument doc = XDocument.Load(fileName);

            var terrasoft = doc.Root.Elements().FirstOrDefault(i => i.Name == "terrasoft");
            var fileDesignMode = terrasoft.Elements().FirstOrDefault(i => i.Name == "fileDesignMode");
            fileDesignMode.FirstAttribute.Value = (isWorkInFS) ? "true" : "false";

            var appSettings = doc.Root.Elements().FirstOrDefault(i => i.Name == "appSettings");
            var useStaticFileContent = appSettings.Elements().FirstOrDefault(i => i.FirstAttribute.Value == "UseStaticFileContent").LastAttribute.Value = (isWorkInFS) ? "false" : "true";

            doc.Save(fileName);
        }

        public static void EditWebConfToWorkInFS(string workFolderPath, string connectString_db)
        {
            string fileName = workFolderPath + @"Terrasoft.WebApp\DesktopBin\WorkspaceConsole\Terrasoft.Tools.WorkspaceConsole.exe.config";
            XDocument doc = XDocument.Load(fileName);

            var connectionStrings = doc.Root.Elements().FirstOrDefault(i => i.Name == "connectionStrings");
            var u = connectionStrings.Elements();
            
            var connectionString = connectionStrings.Elements().FirstOrDefault(i => i.FirstAttribute.Value == "db");
            connectionString.LastAttribute.Value = connectString_db;

            doc.Save(fileName);
        }

    }
}
