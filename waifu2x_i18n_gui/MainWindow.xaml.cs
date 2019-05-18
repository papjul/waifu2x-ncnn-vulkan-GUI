using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using System.ComponentModel; // CancelEventArgs

using Forms = System.Windows.Forms;
namespace waifu2x_ncnn_vulkan_gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            System.Environment.CurrentDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            var dirInfo = new DirectoryInfo(App.directory);
            var langlist = dirInfo.GetFiles("UILang.*.xaml");
            string[] langcodelist = new string[langlist.Length];
            for (int i = 0; i < langlist.Length; i++)
            {
                var fn_parts = langlist[i].ToString().Split('.');
                langcodelist[i] = fn_parts[1];
            }

            foreach (var langcode in langcodelist)
            {
                MenuItem mi = new MenuItem();
                mi.Tag = langcode;
                mi.Header = langcode;
                mi.Click += new RoutedEventHandler(MenuItem_Style_Click);
                menuLang.Items.Add(mi);
            }
            foreach (MenuItem item in menuLang.Items)
            {
                if (item.Tag.ToString().Equals(CultureInfo.CurrentUICulture.Name))
                {
                    item.IsChecked = true;
                }
            }
            // 設定をファイルから読み込む

            if (Properties.Settings.Default.output_dir != "null")
            { txtDstPath.Text = Properties.Settings.Default.output_dir; }

            btn200.IsChecked = true;

            if (Properties.Settings.Default.block_size == "400")
            { btn400.IsChecked = true; }
            if (Properties.Settings.Default.block_size == "300")
            { btn300.IsChecked = true; }
            if (Properties.Settings.Default.block_size == "200")
            { btn200.IsChecked = true; }
            if (Properties.Settings.Default.block_size == "100")
            { btn100.IsChecked = true; }

            //btnCUDA.IsChecked = true;
            btnDenoise0.IsChecked = true;

            if (Properties.Settings.Default.noise_level == "3")
            { btnDenoise3.IsChecked = true; }
            if (Properties.Settings.Default.noise_level == "2")
            { btnDenoise2.IsChecked = true; }
            if (Properties.Settings.Default.noise_level == "1")
            { btnDenoise1.IsChecked = true; }
            if (Properties.Settings.Default.noise_level == "0")
            { btnDenoise0.IsChecked = true; }
            
            btnModeScale.IsChecked = true;

            if (Properties.Settings.Default.mode == "scale")
            { btnModeScale.IsChecked = true; }
            if (Properties.Settings.Default.mode == "noise_scale")
            { btnModeNoiseScale.IsChecked = true; }
            if (Properties.Settings.Default.mode == "noise")
            { btnModeNoise.IsChecked = true; }
            if (Properties.Settings.Default.mode == "auto_scale")
            { btnModeAutoScale.IsChecked = true; }

            checkSoundBeep.IsChecked = Properties.Settings.Default.SoundBeep;
            checkStore_output_dir.IsChecked = Properties.Settings.Default.store_output_dir;

            slider_value.Text = Properties.Settings.Default.scale_ratio;
            slider_zoom.Value = double.Parse(Properties.Settings.Default.scale_ratio);

            //cbTTA.IsChecked = false;

        }

        // public static StringBuilder param_src= new StringBuilder("");
        public static StringBuilder param_dst = new StringBuilder("");
        public static StringBuilder param_mag = new StringBuilder("2");
        public static StringBuilder param_denoise = new StringBuilder("");
        public static StringBuilder param_denoise2 = new StringBuilder("");
        public static StringBuilder param_block = new StringBuilder("200");
        public static StringBuilder param_mode = new StringBuilder("noise_scale");
        public static String[] param_src;
        public static StringBuilder random32 = new StringBuilder("");
        public static StringBuilder Commandline = new StringBuilder("");
        public static StringBuilder waifu2x_bat = new StringBuilder("");
        // public static int FileCount = (0);

        public static bool EventHandler_Flag = false;

        //public static StringBuilder param_tta = new StringBuilder("-t 0");
        public static Process pHandle = new Process();
        public static ProcessStartInfo psinfo = new ProcessStartInfo();

        public static StringBuilder console_buffer = new StringBuilder();

        public static bool flagAbort = false;

        public static bool queueFlag = false;

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
                
            // 設定を保存

            // 前回出力したパスを記憶する
            if (checkStore_output_dir.IsChecked == true)
            {
                if (txtDstPath.Text.Trim() != "")
                {
                    Properties.Settings.Default.output_dir = txtDstPath.Text;
                } else
                {
                    Properties.Settings.Default.output_dir = "null";
                }

            }
            else
            {
                Properties.Settings.Default.output_dir = "null";
            }

            // Properties.Settings.Default.Device_ID = txtDevice.Text;


            Properties.Settings.Default.block_size = param_block.ToString();
            
            // Properties.Settings.Default.mode = param_mode.ToString().Replace("-m ", "");

            Properties.Settings.Default.SoundBeep = Convert.ToBoolean(checkSoundBeep.IsChecked);
            Properties.Settings.Default.store_output_dir = Convert.ToBoolean(checkStore_output_dir.IsChecked);
            Properties.Settings.Default.mode = param_mode.ToString().Replace("-m ", "");
            Properties.Settings.Default.noise_level = param_denoise.ToString();

            if (System.Text.RegularExpressions.Regex.IsMatch(
                slider_value.Text,
                @"^\d+(\.\d+)?$",
                System.Text.RegularExpressions.RegexOptions.ECMAScript))
            {
               Properties.Settings.Default.scale_ratio = slider_value.Text;
            } else 
            {
               Properties.Settings.Default.scale_ratio = "2";
            }

            Properties.Settings.Default.Save();

            try
            {
                KillProcessTree(pHandle);
            }
            catch (Exception) { /*Nothing*/ }
        }

        private void OnMenuHelpClick(object sender, RoutedEventArgs e)
        {
            string msg =
                "This is a multilingual graphical user-interface\n" +
                "for the waifu2x-ncnn-vulkan commandline program.\n" +
                "You need a working copy of waifu2x-ncnn-vulkan first\n" +
                "then copy everything from the GUI archive to\n" +
                "waifu2x-ncnn-vulkan folder.\n" +
                "DO NOT rename any subdirectories inside waifu2x-ncnn-vulkan folder\n" +
                "To make a translation, copy one of the bundled xaml file\n" +
                "then edit the copy with a text editor.\n" +
                "Whenever you see a language code like en-US, change it to\n" +
                "the target language code like zh-TW, ja-JP.\n" +
                "The filename needs to be changed too.";
            MessageBox.Show(msg);
        }

        private void OnMenuVersionClick(object sender, RoutedEventArgs e)
        {
            string msg =
                "Multilingual GUI for waifu2x-ncnn-vulkan\n" +
                "f11894 (2019)\n" +
                "Version 1.0.2\n" +
                "BuildDate: 16 Apr,2019\n" +
                "License: Do What the Fuck You Want License";
            MessageBox.Show(msg);
        }

        private void OnBtnSrc(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fdlg= new OpenFileDialog();
            fdlg.Multiselect = true;
            if (fdlg.ShowDialog() == true)
            {
                this.txtSrcPath.Text = string.Join("\n", fdlg.FileNames);
            }
        }

        private void OnSrcClear(object sender, RoutedEventArgs e)
        {
            this.txtSrcPath.Clear();
        }

        private void OnBtnDst(object sender, RoutedEventArgs e)
        {
            var dlg = new Forms.FolderBrowserDialog();
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.txtDstPath.Text = dlg.SelectedPath;
            }
        }

        private void OnDstClear(object sender, RoutedEventArgs e)
        {
            this.txtDstPath.Clear();
        }

        private void MenuItem_Style_Click(object sender, RoutedEventArgs e)
        {
            foreach(MenuItem item in menuLang.Items)
            {
                item.IsChecked = false;
            }
            MenuItem mi = (MenuItem)sender;
            mi.IsChecked = true;
            App.Instance.SwitchLanguage(mi.Tag.ToString());
        }

        private void On_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects= DragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private void On_SrcDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fn = (string[])e.Data.GetData(DataFormats.FileDrop);
                this.txtSrcPath.Text = string.Join("\n", fn);
                param_src = (string[])e.Data.GetData(DataFormats.FileDrop);

            }
        }

        private void On_DstDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fn = (string[])e.Data.GetData(DataFormats.FileDrop);
                this.txtDstPath.Text = fn[0];
            }
        }

        private void OnSetModeChecked(object sender, RoutedEventArgs e)
        {
            gpDenoise.IsEnabled = true;
            if (btnModeNoise.IsChecked == false)
            {
                slider_zoom.IsEnabled = true;
                slider_value.IsEnabled = true;
            }

            param_mode.Clear();
            RadioButton optsrc = sender as RadioButton;
            param_mode.Append(optsrc.Tag.ToString());
            if (btnModeScale.IsChecked == true)
            { gpDenoise.IsEnabled = false; }

            if (btnModeNoise.IsChecked == true)
            {
                slider_zoom.IsEnabled = false;
                slider_value.IsEnabled = false;
            }
        }
        private void OnDenoiseChecked(object sender, RoutedEventArgs e)
        {
            param_denoise.Clear();
            RadioButton optsrc= sender as RadioButton;
            param_denoise.Append(optsrc.Tag.ToString());
        }

        /*private void OnDeviceChecked(object sender, RoutedEventArgs e)
        {
            param_device.Clear();
            RadioButton optsrc= sender as RadioButton;
            param_device.Append(optsrc.Tag.ToString());
        }
        */

        private void OnBlockChecked(object sender, RoutedEventArgs e)
        {
            param_block.Clear();
            RadioButton optsrc= sender as RadioButton;
            param_block.Append(optsrc.Tag.ToString());
        }
        
        private void OnConsoleDataRecv(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {

                console_buffer.Append(e.Data);
                console_buffer.Append(Environment.NewLine);
                // if (queueFlag) return;
                // queueFlag = true;
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    // queueFlag = false;
                    CLIOutput.Focus();
                    this.CLIOutput.AppendText(e.Data);
                    this.CLIOutput.AppendText(Environment.NewLine);
                    CLIOutput.Select(CLIOutput.Text.Length, 0);
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
            }
            
        }
        
        private void OnProcessExit(object sender, EventArgs e)
        {
            try
            {
                pHandle.CancelOutputRead();
                pHandle.CancelErrorRead();
            }
            catch (Exception)
            {
                //No need to throw
                //throw;
            }

            pHandle.Close();
            Dispatcher.BeginInvoke(new Action(delegate
            {
                if (checkSoundBeep.IsChecked == true) if (this.btnRun.IsEnabled == false)
                { System.Media.SystemSounds.Beep.Play(); }
                
                this.btnAbort.IsEnabled = false;
                this.btnRun.IsEnabled = true;
                //this.CLIOutput.Text = console_buffer.ToString();

            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);
            flagAbort = false;
        }

        private async void OnAbort(object sender, RoutedEventArgs e)
        {
            this.btnAbort.IsEnabled = false;
            try
            {
                pHandle.CancelOutputRead();
                pHandle.CancelErrorRead();
            }
            catch (Exception) { /*Nothing*/ }

            if (!pHandle.HasExited)
            {
                try
                {
                    await Task.Run(() => KillProcessTree(pHandle));
                }
                catch (Exception) { /*Nothing*/ }

                flagAbort = true;
                this.CLIOutput.Clear();
            }
        }

        private void KillProcessTree(System.Diagnostics.Process process)
        {
            string taskkill = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "taskkill.exe");
            using (var procKiller = new System.Diagnostics.Process())
            {
                procKiller.StartInfo.FileName = taskkill;
                procKiller.StartInfo.Arguments = string.Format("/PID {0} /T /F", process.Id);
                procKiller.StartInfo.CreateNoWindow = true;
                procKiller.StartInfo.UseShellExecute = false;
                procKiller.Start();
                procKiller.WaitForExit();
            }
        }

        public void Errormessage(string x)
        { 
          System.Media.SystemSounds.Beep.Play();
          MessageBox.Show(@x);
          btnAbort.IsEnabled = false;
          btnRun.IsEnabled = true;
          return; 
        }

        private void OnRun(object sender, RoutedEventArgs e)
        {
            if (!File.Exists("waifu2x.exe"))
            {
                MessageBox.Show(@"waifu2x.exe is missing!");
                return;
            }
            // Sets Source
            // The source must be a file or folder that exists

            // logをクリアする
            this.CLIOutput.Clear();
            if (this.txtDstPath.Text.Trim() != "") if (Directory.Exists(this.txtDstPath.Text) == false)
                {
                    try
                    {
                        Directory.CreateDirectory(this.txtDstPath.Text);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(@"Failed to create output destination folder.");
                        return;
                    }
                }
            Commandline.Clear();
            int FileCount = 0;
            for (int i = 0; i < param_src.Length; i++)
            {
                FileCount++;
            }
            Commandline.Append("@echo off\r\n");
            Commandline.Append("chcp 65001 >nul\r\n");
            Commandline.Append("set \"FileCount=" + FileCount + "\"\r\n");
            Commandline.Append("set \"ProcessedCount=0\"\r\n");
            Commandline.Append("if not \"%FileCount%\"==\"1\" echo progress %ProcessedCount%/%FileCount%\r\n");
            for (int i = 0; i < param_src.Length; i++)
            {
                param_dst.Clear();
                if (this.txtDstPath.Text.Trim() != "") {
                    param_dst.Append("\"");
                    param_dst.Append(this.txtDstPath.Text);
                    param_dst.Append("\\");
                    param_dst.Append(System.IO.Path.GetFileNameWithoutExtension(param_src[i].Replace("%", "%%")));
                    param_dst.Append(".png\"");
                } else {
                    param_dst.Append("\"");
                    System.IO.DirectoryInfo hDirInfo = System.IO.Directory.GetParent(param_src[i]);
                    param_dst.Append(hDirInfo.FullName);
                    param_dst.Append("\\");
                    param_dst.Append(System.IO.Path.GetFileNameWithoutExtension(param_src[i].Replace("%", "%%")));
                    param_dst.Append("(CUnet)");
                    param_dst.Append("(");
                    param_dst.Append(param_mode.ToString().Replace("-m ", ""));
                    param_dst.Append(")");
                    if (param_mode.ToString() == "-m noise" || param_mode.ToString() == "-m noise_scale" || param_mode.ToString() == "-m auto_scale")
                    {
                        param_dst.Append("(");
                        param_dst.Append("Level");
                        param_dst.Append(param_denoise.ToString());
                        param_dst.Append(")");
                    }

                    if (param_mode.ToString() == "-m scale" || param_mode.ToString() == "-m noise_scale" || param_mode.ToString() == "-m auto_scale")
                    {
                        param_dst.Append("(x");
                        param_dst.Append(this.slider_zoom.Value.ToString());
                        param_dst.Append(")");
                    }
                    param_dst.Append(".png\"");
                }
                param_mag.Clear();
                // param_mag.Append("-s ");
                param_mag.Append(this.slider_value.Text);

                param_denoise2.Clear();
                param_denoise2.Append(param_denoise.ToString());

                // Set mode
                if (param_mode.ToString() == "-m noise")
                {
                    param_mag.Clear();
                    param_mag.Append("1");
                }
                if (param_mode.ToString() == "-m scale")
                {
                    param_denoise2.Clear();
                    param_denoise2.Append("-1");
                }
                Commandline.Append("set input_image=\"" + param_src[i].Replace("%", "%%") + "\"\r\n");
                Commandline.Append("set output_image="+ param_dst + "\r\n");
                Commandline.Append("call :waifu2x_run\r\n");
            }
            Commandline.Append("exit /b\r\n");
            Commandline.Append("\r\n");
            Commandline.Append(":waifu2x_run\r\n");
            Commandline.Append("set input_image_jpg=\r\n");
            Commandline.Append("for %%i in (%input_image%) do set \"input_image_ext=%%~xi\"\r\n");
            Commandline.Append("if /i \"%input_image_ext%\"==\".jpg\" set \"input_image_jpg=1\"\r\n");
            Commandline.Append("if /i \"%input_image_ext%\"==\".jpeg\" set \"input_image_jpg=1\"\r\n");
            Commandline.Append("set \"noise_level=" + param_denoise2 + "\"\r\n");
            Commandline.Append("if \"" + param_mode.ToString() + "\"==\"-m auto_scale\" if not \"%input_image_jpg%\"==\"1\" set \"noise_level=-1\"\r\n");
            Commandline.Append("echo " + "waifu2x.exe " + "%input_image%" + " " + "%output_image%" + " " + "%noise_level%" + " " + param_mag + " " + param_block + "\r\n");
            Commandline.Append("waifu2x.exe " + "%input_image%" + " " + "%output_image%" + " " + " %noise_level%" + " " + param_mag + " " + param_block + "\r\n");
            Commandline.Append("set /a ProcessedCount=%ProcessedCount%+1\r\n");
            Commandline.Append("if not \"%FileCount%\"==\"1\" echo progress %ProcessedCount%/%FileCount%\r\n");
            Commandline.Append("exit /b\r\n");

            Guid g = System.Guid.NewGuid();
            random32.Clear();
            random32.Append(g.ToString("N").Substring(0, 32));
            waifu2x_bat.Clear();
            waifu2x_bat.Append(System.IO.Path.GetTempPath() + "waifu2x_" + random32.ToString() + ".bat");

            System.IO.StreamWriter sw = new System.IO.StreamWriter(
            @waifu2x_bat.ToString(),
            false
            // ,
            // System.Text.Encoding.GetEncoding("utf-8")
            );
            sw.Write(Commandline);
            sw.Close();

            this.btnRun.IsEnabled = false;
            this.btnAbort.IsEnabled = true;

            // Setup ProcessStartInfo
            psinfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");
            psinfo.StandardErrorEncoding = Encoding.UTF8;
            psinfo.StandardOutputEncoding = Encoding.UTF8;
            psinfo.Arguments = "/c \"" + waifu2x_bat.ToString() + "\"";
            psinfo.RedirectStandardInput = true;
            psinfo.RedirectStandardError = true;
            psinfo.RedirectStandardOutput = true;
            psinfo.UseShellExecute = false;
            psinfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            psinfo.CreateNoWindow = true;
            psinfo.WindowStyle = ProcessWindowStyle.Hidden;
            pHandle.StartInfo = psinfo;
            pHandle.EnableRaisingEvents = true;
            if (EventHandler_Flag == false)
            {
                 pHandle.OutputDataReceived += new DataReceivedEventHandler(OnConsoleDataRecv);
                 pHandle.ErrorDataReceived += new DataReceivedEventHandler(OnConsoleDataRecv);
                 pHandle.Exited += new EventHandler(OnProcessExit);
                 EventHandler_Flag = true;
             }

            // Starts working
            console_buffer.Clear();

            try
            {
                //MessageBox.Show(full_param);
                bool pState = pHandle.Start();
            }
            catch (Exception)
            {
                try
                {
                    pHandle.Kill();
                }
                catch (Exception) { /*Nothing*/ }
                Errormessage("Failed to start waifu2x.exe.");
                //throw;
            }

            Dispatcher.BeginInvoke(new Action(delegate
            {
                CLIOutput.Focus();
                // this.CLIOutput.AppendText("waifu2x.exe " + full_param.ToString());
                // this.CLIOutput.AppendText(Environment.NewLine);
                // this.CLIOutput.AppendText(Environment.NewLine);
                CLIOutput.Select(CLIOutput.Text.Length, 0);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle, null);

            try
            {
                pHandle.BeginOutputReadLine();
            }
            catch (Exception)
            {
                this.CLIOutput.Clear();
                this.CLIOutput.Text = "BeginOutputReadLine crashed...\n";
            }

            try
            {
                pHandle.BeginErrorReadLine();
            }
            catch (Exception)
            {
                this.CLIOutput.Clear();
                this.CLIOutput.Text = "BeginErrorReadLine crashed...\n";
            }

            //pHandle.BeginErrorReadLine();
            //MessageBox.Show("Some parameters do not mix well and crashed...");

            //pHandle.WaitForExit();
            /*
            pHandle.CancelOutputRead();
            pHandle.Close();
            this.btnAbort.IsEnabled = false;
            this.btnRun.IsEnabled = true;
            this.CLIOutput.Text = console_buffer.ToString();
            */

        }
    }
}
