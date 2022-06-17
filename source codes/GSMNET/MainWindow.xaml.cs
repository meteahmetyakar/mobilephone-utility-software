using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GSMNET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Process adb_process, fb_process;
        ProcessStartInfo adb_startInfo, fb_startInfo;
        String deviceId = "";

        public MainWindow()
        {


            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            adb_process = new Process();
            fb_process = new Process();

            adb_startInfo = new ProcessStartInfo();
            adb_startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            adb_startInfo.FileName = "./platform-tools/adb.exe";
            adb_startInfo.Arguments = "";
            adb_startInfo.RedirectStandardOutput = true;
            adb_startInfo.CreateNoWindow = true;
            adb_startInfo.UseShellExecute = false;
            
            adb_process.StartInfo = adb_startInfo;

            fb_startInfo = new ProcessStartInfo();
            fb_startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            fb_startInfo.FileName = "~/platform-tools/fastboot.exe";
            fb_startInfo.Arguments = "";
            fb_startInfo.RedirectStandardOutput = true;
            fb_startInfo.CreateNoWindow = true;
            fb_startInfo.UseShellExecute = false;

            fb_process.StartInfo = fb_startInfo;

            fb_process.Exited += new EventHandler(process_Exited);
            adb_process.Exited += new EventHandler(process_Exited);

        }
        private void process_Exited(object sender, System.EventArgs e)
        {
            MessageBox.Show("İşlem Bitti!");
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Bir hata oluştu");
            File.AppendAllText("error.txt", (e.ExceptionObject as Exception).ToString());
        }

        private List<string> runAdbCommand(string command)
        {
            if (deviceId != "")
                command = $"-s {deviceId} {command}";
            adb_startInfo.Arguments = command;
            adb_process.Start();

         while(!adb_process.HasExited)
            {
                
            }

            var result = adb_process.StandardOutput.ReadToEnd().Replace("\r", "").Split("\n").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

           
            return result;
        }

        private List<string> runFbCommand(string command)
        {
            if (deviceId != "")
                command = $"-i {deviceId} {command}";
            fb_startInfo.Arguments = command;
            fb_process.Start();
            while (!fb_process.HasExited)
            {

            }
            var result = fb_process.StandardOutput.ReadToEnd().Replace("\r", "").Split("\n").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            return result;
        }

        private async void getDeviceInfo()
        {
            lblBrand.Content = "Marka: " + runAdbCommand("shell getprop ro.product.brand").FirstOrDefault();
            lblModel.Content = "Model: " + runAdbCommand("shell getprop ro.product.model").FirstOrDefault();
            lblVersion.Content = "Versiyon: " + runAdbCommand("shell getprop ro.build.version.release").FirstOrDefault();
            lblPBoard.Content = "P. Board: " + runAdbCommand("shell getprop ro.product.board").FirstOrDefault();
            lblId.Content = "Id: " + runAdbCommand("shell getprop ro.build.id").FirstOrDefault();
        }

        private void openDiagPort()
        {
            var result = runAdbCommand("shell su -c setprop sys.usb.config diag,adb");
            if (result.Count > 0)
            {
                if (result.First() != "")
                {
                    MessageBox.Show("Diag port açılırken bir sorun oluştu");
                    MessageBox.Show(result.First());
                }
                else
                {
                    MessageBox.Show("Diag port açıldı");
                }      
            }

        }


        private void btnOpenDiag_Click(object sender, RoutedEventArgs e)
        {
            openDiagPort();
        }

        private void setToFastbootMode()
        {
            runAdbCommand("reboot bootloader");

        }

        private void btnFastbootMode_Click(object sender, RoutedEventArgs e)
        {
            setToFastbootMode();
        }

        private void btnGetDevices_Click(object sender, RoutedEventArgs e)
        {
            var devices = getDevices();
            if (devices.Count == 0)
                return;
            cbDevices.ItemsSource = devices;
            cbDevices.SelectedIndex = 0;
            this.deviceId = devices.First().Split("\t").First();
        }

        private List<String> getDevices()
        {
            var devices = runAdbCommand("devices");
            devices.RemoveAt(0);
            //devices.Add(devices.First());
            return devices;
        }

        private void cbDevices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.deviceId = cbDevices.Text.Split("\t").First();
            getDeviceInfo();
        }

        private void btnPushApk_Click(object sender, RoutedEventArgs e)
        {
            
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".apk";
            dlg.Filter = "APK Dosyaları (*.apk)|*.apk|All files (*.*)|*.*";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                string copyPath = tbApkPathToPush.Text;

                runAdbCommand($"push {filename} {copyPath}/{dlg.SafeFileName}");
            }
        }

        private void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".apk";
            dlg.Filter = "APK Dosyaları (*.apk)|*.apk";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;

                runAdbCommand($"install {filename}");
            }
        }

        private void btnResetPartition_Click(object sender, RoutedEventArgs e)
        {

        }
        
        private void resetPartition()
        {
            runAdbCommand("shell dd if=/dev/zero of=/dev/block/bootdevice/by-name/modemst1");
            runAdbCommand("shell dd if=/dev/zero of=/dev/block/bootdevice/by-name/modemst2");
            runAdbCommand("shell dd if=/dev/zero of=/dev/block/bootdevice/by-name/fsg");

        }

        private void btnEraseFrp_Click(object sender, RoutedEventArgs e)
        {
            eraseFrp();
        }

        private void quitBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void minimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void btnFlashRecovery_Click(object sender, RoutedEventArgs e)
        {
            //process.OutputDataReceived += (sender ,args)

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".img";
            dlg.Filter = "IMG Dosyaları (*.img)|*.img";
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;

                runFbCommand($"recovery {filename}");
            }
        }

        private void eraseFrp()
        {
            runFbCommand("erase frp");

        }

        
    }
}
