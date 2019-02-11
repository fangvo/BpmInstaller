using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Compression;
using System.IO;

namespace BpmInstaller
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Installer installer;
        public string Log { get; set; }


        public MainWindow()
        {
            InitializeComponent();
            labelLog.Content = string.Empty;
            installer = new Installer();
            installer.NeedWorkInFS = (bool)devInFS.IsChecked;
            installer.StageChanged += SetLog;
            ShowHideClearInstall();
		}


        private void SetLog(string log)
        {
            Log = log;
            labelLog.Dispatcher.Invoke(() => { labelLog.Content = Log; });
        }



        //Start
        private void Button_Click(object sender, RoutedEventArgs e)
        {
			if (!File.Exists(textBoxDistrPath.Text))
			{
				SetLog("Указанный дистрибутив не существует");
				return;
			}

			SetInstaller(installer);

			Thread thread = new Thread(Work);
			thread.Start();
		}

        //browse folder
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxPath.Text = FBD.SelectedPath;
            }
        }

        //browse Distr file
        private void buttonChoseDist_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxDistrPath.Text = OFD.FileName;
            }
        }

        //browse file backup
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxBackupPath.Text = OFD.FileName;
            }
        }

        private void SetInstaller(Installer installer)
        {
            installer.Name = textBoxName.Text;
            if (!textBoxPath.Text.EndsWith(@"\"))
				textBoxPath.Text += @"\";
            installer.WorkFolderPath = textBoxPath.Text + textBoxName.Text + @"\";
            installer.DistrPath = textBoxDistrPath.Text;
            installer.Login = textBoxLogin.Text;
            installer.Password = textBoxPassword.Password;
            installer.RedisDBNumber = redisNumber.Text;
            installer.IisPort = textBoxIisPort.Text;

            if ((bool)clearInstall.IsChecked)
            {
                var filesInDistr = ZipFile.OpenRead(textBoxDistrPath.Text);
                var bakFileNeme = filesInDistr.Entries.FirstOrDefault(fileName => fileName.FullName.Contains(".bak"));
                installer.DBBackupPath = installer.WorkFolderPath + @"db\" + bakFileNeme.Name;
            }
            else
            {
                installer.DBBackupPath = textBoxBackupPath.Text;
            }
        }

        private void Work()
        {
            progressBar.Dispatcher.Invoke(SatartWork);

            installer.Start();

            progressBar.Dispatcher.Invoke(EndWork);
        }

        private void SatartWork()
        {
            progressBar.IsIndeterminate = true;
        }

        private void EndWork()
        {
            progressBar.IsIndeterminate = false;
            progressBar.Value = progressBar.Maximum;
        }

        private void ShowHideClearInstall()
        {
            if ((bool)clearInstall.IsChecked)
            {
                textBoxBackupPath.Visibility = Visibility.Hidden;
                labelBackupPath.Visibility = Visibility.Hidden;
                buttonBackupBrowse.Visibility = Visibility.Hidden;
            }
            if (!(bool)clearInstall.IsChecked)
            {
                textBoxBackupPath.Visibility = Visibility.Visible;
                labelBackupPath.Visibility = Visibility.Visible;
                buttonBackupBrowse.Visibility = Visibility.Visible;
            } 
        }

        private void clearInstall_Click(object sender, RoutedEventArgs e)
        {
            ShowHideClearInstall();
        }


        private void devInFS_Click(object sender, RoutedEventArgs e)
        {
            installer.NeedWorkInFS = (bool)devInFS.IsChecked;
        }

    }
}
