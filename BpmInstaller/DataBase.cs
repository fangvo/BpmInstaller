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
    class DataBase
    {
        public static void RestoreDB(string dbName, string backupPath, string login, string password)
        {
            ServerConnection serverConnection = new ServerConnection(Environment.MachineName, login, password);
            Server smoServer = new Server(serverConnection);
            Database db = smoServer.Databases[dbName];

            if (db == null)
            {
                db = new Database(smoServer, dbName);
                db.Create();
                db.Refresh();
            }

            Restore restore = new Restore();
            BackupDeviceItem deviceItem = new BackupDeviceItem(backupPath, DeviceType.File);
            restore.Devices.Add(deviceItem);
            restore.Database = dbName;
            restore.Action = RestoreActionType.Database;
            restore.ReplaceDatabase = true;
            restore.NoRecovery = false;
            restore.SqlRestore(smoServer);

            db = smoServer.Databases[dbName];
            db.SetOnline();
            smoServer.Refresh();
            db.Refresh();
        }
    }
}
