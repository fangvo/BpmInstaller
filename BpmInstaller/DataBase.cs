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
		public static bool Exist(string dbName, string login, string password)
		{
			ServerConnection serverConnection = new ServerConnection(Environment.MachineName, login, password);
			Server smoServer = new Server(serverConnection);
			Database db = smoServer.Databases[dbName];
			return db == null;
		}
        public static void Restore(string serverName, string dbName, string backupPath, string login, string password)
        {
            ServerConnection serverConnection = new ServerConnection(serverName, login, password);
            RestoreDB(serverConnection, dbName, backupPath);
        }

        public static void Restore(string serverName, string dbName, string backupPath)
        {
            ServerConnection serverConnection = new ServerConnection(serverName);
            RestoreDB(serverConnection, dbName, backupPath);
        }

        private static void RestoreDB(ServerConnection connection, string dbName, string backupPath)
        {
            Server smoServer = new Server(connection);
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
            var mdfPath = $"{smoServer.DefaultFile}{dbName}.mdf";
            var ldfPath = $"{smoServer.DefaultFile}{dbName}_log.ldf";

            System.Data.DataTable logicalRestoreFiles = restore.ReadFileList(smoServer);
            restore.RelocateFiles.Add(new RelocateFile(logicalRestoreFiles.Rows[0][0].ToString(), mdfPath));
            restore.RelocateFiles.Add(new RelocateFile(logicalRestoreFiles.Rows[1][0].ToString(), ldfPath));

            smoServer.KillAllProcesses(dbName);
            restore.SqlRestore(smoServer);

            db = smoServer.Databases[dbName];
            db.SetOnline();
            smoServer.Refresh();
            db.Refresh();
        }
    }
}
