using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices; 
using System.Windows;
using System.Windows.Interop;       
using System.Windows.Media;

namespace FixWeatherGadget
{
    public partial class MainWindow : Window
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern void DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern bool DwmIsCompositionEnabled();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6 && DwmIsCompositionEnabled())
                {
                    IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle;
                    HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);

                    mainWindowSrc.CompositionTarget.BackgroundColor = Colors.Transparent;
                    this.Background = Brushes.Transparent;

                    MARGINS margins = new MARGINS()
                    {
                        cxLeftWidth = -1,
                        cxRightWidth = -1,
                        cyTopHeight = -1,
                        cyBottomHeight = -1
                    };

                    DwmExtendFrameIntoClientArea(mainWindowPtr, ref margins);
                }
            }
            catch { }
        }

        private void BtnFix_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process[] sidebarProcesses = Process.GetProcessesByName("sidebar");
                foreach (Process process in sidebarProcesses)
                {
                    process.Kill();
                    process.WaitForExit(2000);
                }

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string cacheDirectory = Path.Combine(localAppData, @"Microsoft\Windows Live\Services\Cache");

                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }

                string configFilePath = Path.Combine(cacheDirectory, "Config.xml");

                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FixWeatherGadget.Config.xml"))
                {
                    if (stream == null)
                    {
                        MessageBox.Show("Embedded resource 'FixWeatherGadget.Config.xml' not found", "Resource Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    using (StreamReader reader = new StreamReader(stream))
                    {
                        File.WriteAllText(configFilePath, reader.ReadToEnd());
                    }
                }

                DateTime targetFutureDate = new DateTime(2037, 12, 31, 23, 59, 59);
                File.SetCreationTime(configFilePath, targetFutureDate);
                File.SetLastWriteTime(configFilePath, targetFutureDate);
                File.SetLastAccessTime(configFilePath, targetFutureDate);

                string programFilesW6432 = Environment.GetEnvironmentVariable("ProgramW6432");
                string defaultProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

                string baseProgramFilesPath = !string.IsNullOrEmpty(programFilesW6432) ? programFilesW6432 : defaultProgramFiles;
                string sidebarExecutablePath = System.IO.Path.Combine(baseProgramFilesPath, @"Windows Sidebar\sidebar.exe");

                if (File.Exists(sidebarExecutablePath))
                {
                    Process.Start(sidebarExecutablePath);
                    MessageBox.Show("Fix applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("sidebar.exe was not found at: " + sidebarExecutablePath, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during the fix: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}