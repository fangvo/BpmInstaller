using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BpmInstaller
{
	class IISHelper
	{

		public static void SetIis(string name, string workFolder, string port)
		{
			ServerManager iisManager = new ServerManager();
			var pool = iisManager.ApplicationPools[name] ?? iisManager.ApplicationPools.Add(name);
			var site = iisManager.Sites[name] ?? iisManager.Sites.Add(name, "http", "*:" + port + ":", workFolder);
			site.ApplicationDefaults.ApplicationPoolName = name;
			var appPath = workFolder + @"Terrasoft.WebApp\";
			var application = site.Applications["/0"] ?? site.Applications.Add("/0", appPath);
			iisManager.CommitChanges();
		}

		public static void RestartAsAdmin()
		{
			if (!IsAdministrator())
			{
				// Restart program and run as admin
				var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
				ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
				startInfo.Verb = "runas";
				System.Diagnostics.Process.Start(startInfo);
				System.Windows.Application.Current.Shutdown();
				return;
			}
		}


		public static bool IsAdministrator()
		{
			WindowsIdentity identity = WindowsIdentity.GetCurrent();
			WindowsPrincipal principal = new WindowsPrincipal(identity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}
	}
}
