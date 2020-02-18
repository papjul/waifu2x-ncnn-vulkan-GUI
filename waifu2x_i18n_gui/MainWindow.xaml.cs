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

            if (System.Text.RegularExpressions.Regex.IsMatch(
                Properties.Settings.Default.gpu_id,
                @"^\d+$",
                System.Text.RegularExpressions.RegexOptions.ECMAScript))
            {
                txtGPU_ID.Text = Properties.Settings.Default.gpu_id;
            }

            txtBlocksize.Text = Properties.Settings.Default.block_size;
            txtThread.Text = Properties.Settings.Default.thread;

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

            btnCUnet.IsChecked = true;

            if (Properties.Settings.Default.model == "models-cunet")
            { btnCUnet.IsChecked = true; }
            if (Properties.Settings.Default.model == "models-upconv_7_anime_style_art_rgb")
            { btnUpRGB.IsChecked = true; }
            if (Properties.Settings.Default.model == "models-upconv_7_photo")
            { btnUpPhoto.IsChecked = true; }

            checkSoundBeep.IsChecked = Properties.Settings.Default.SoundBeep;
            checkStore_output_dir.IsChecked = Properties.Settings.Default.store_output_dir;
            checkOutput_no_overwirit.IsChecked = Properties.Settings.Default.output_no_overwirit;
            checkPrecision_fp32.IsChecked = Properties.Settings.Default.Precision_fp32;
            checkPrevent_double_extensions.IsChecked = Properties.Settings.Default.Prevent_double_extensions;
            slider_value.Text = Properties.Settings.Default.scale_ratio;
            slider_zoom.Value = double.Parse(Properties.Settings.Default.scale_ratio);

            //cbTTA.IsChecked = false;

        }

        // public static StringBuilder param_src= new StringBuilder("");
        public static StringBuilder param_dst = new StringBuilder("");
        public static StringBuilder param_mag = new StringBuilder("2");
        public static StringBuilder param_denoise = new StringBuilder("");
        public static StringBuilder param_denoise2 = new StringBuilder("");
        public static StringBuilder param_model = new StringBuilder("models-cunet");
        public static StringBuilder param_block = new StringBuilder("200");
        public static StringBuilder param_mode = new StringBuilder("noise_scale");
        public static StringBuilder param_gpu_id = new StringBuilder("");
        public static StringBuilder param_thread = new StringBuilder("1:2:2");
        public static String[] param_src;
        public static StringBuilder random32 = new StringBuilder("");
        public static StringBuilder binary_path = new StringBuilder("");
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

            Properties.Settings.Default.output_no_overwirit = Convert.ToBoolean(checkOutput_no_overwirit.IsChecked);
            Properties.Settings.Default.model = param_model.ToString().Replace("-m ", "");
            // Properties.Settings.Default.mode = param_mode.ToString().Replace("-m ", "");

            Properties.Settings.Default.SoundBeep = Convert.ToBoolean(checkSoundBeep.IsChecked);
            Properties.Settings.Default.store_output_dir = Convert.ToBoolean(checkStore_output_dir.IsChecked);
            Properties.Settings.Default.Precision_fp32 = Convert.ToBoolean(checkPrecision_fp32.IsChecked);
            Properties.Settings.Default.Prevent_double_extensions = Convert.ToBoolean(checkPrevent_double_extensions.IsChecked);
            Properties.Settings.Default.mode = param_mode.ToString();
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

            if (System.Text.RegularExpressions.Regex.IsMatch(
                txtBlocksize.Text,
                @"^\d+$",
                System.Text.RegularExpressions.RegexOptions.ECMAScript))
            {
                Properties.Settings.Default.block_size = txtBlocksize.Text;
            }
            else
            {
                Properties.Settings.Default.block_size = "400";
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(
                txtGPU_ID.Text,
                @"^\d+$",
                System.Text.RegularExpressions.RegexOptions.ECMAScript))
            {
                Properties.Settings.Default.gpu_id = txtGPU_ID.Text;
            }
            else
            {
                Properties.Settings.Default.gpu_id = "Unspecified";
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(
                txtThread.Text,
                @"^\d+:\d+:\d+$",
                System.Text.RegularExpressions.RegexOptions.ECMAScript))
            {
                Properties.Settings.Default.thread = txtThread.Text;
            }
            else
            {
                Properties.Settings.Default.thread = "1:2:2";
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
                "f11894 (2020)\n" +
                "Version 1.1.0\n" +
                "BuildDate: 12 Feb,2020\n" +
                "License: Do What the Fuck You Want License";
            MessageBox.Show(msg);
        }

        private void OnBtnSrc(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fdlg= new OpenFileDialog();
            fdlg.Multiselect = true;
            fdlg.Filter = "Graphic Files(*.png;*.jpg;*jpeg;*.gif;*.bmp;*.tif;*.tiff;*.webp)|*.png;*.jpg;*jpeg;*.gif;*.bmp;*.tif;*.tiff;*.webp|All Files(*.*)|*.*";
            if (fdlg.ShowDialog() == true)
            {
                this.txtSrcPath.Text = string.Join("\n", fdlg.FileNames);
                param_src = fdlg.FileNames;
            }
        }

        private void OnSrcClear(object sender, RoutedEventArgs e)
        {
            this.txtSrcPath.Clear();
            param_src = null;
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
            param_gpu_id.Clear();
            RadioButton optsrc= sender as RadioButton;
            param_gpu_id.Append(optsrc.Tag.ToString());
        }
        */

        private void OnModelChecked(object sender, RoutedEventArgs e)
        {
            param_model.Clear();
            RadioButton optsrc = sender as RadioButton;
            param_model.Append("-m ");
            param_model.Append(optsrc.Tag.ToString());
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
            if (!File.Exists("waifu2x-ncnn-vulkan.exe"))
            {
                MessageBox.Show(@"waifu2x-ncnn-vulkan.exe is missing!");
                return;
            }

            if (param_src == null)
            {
                MessageBox.Show(@"source images is not found!");
                return;
            }

            // Sets Source
            // The source must be a file or folder that exists
            if (System.Text.RegularExpressions.Regex.IsMatch(
                txtGPU_ID.Text,
                @"^(\d+)$",
                System.Text.RegularExpressions.RegexOptions.ECMAScript))
            {
                param_gpu_id.Clear();
                param_gpu_id.Append("-g ");
                param_gpu_id.Append(txtGPU_ID.Text);
            }
            else
            {
                param_gpu_id.Clear();
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(
                txtBlocksize.Text,
                @"^(\d+)$",
                System.Text.RegularExpressions.RegexOptions.ECMAScript))
            {
                if (int.Parse(txtBlocksize.Text) % 4 != 0)
                {
                    MessageBox.Show(@"Block size must be a multiple of 4");
                    return;
                }
                else
                {
                    param_block.Clear();
                    param_block.Append("-t ");
                    param_block.Append(txtBlocksize.Text);
                }
            }
            else
            {
                param_block.Clear();
            }

            if (((int)slider_zoom.Value & ((int)slider_zoom.Value - 1)) != 0)
            {
                MessageBox.Show(@"Magnification must be a power of two.");
                return;
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(
                txtThread.Text,
                 @"^(\d+:\d+:\d+)$",
               System.Text.RegularExpressions.RegexOptions.ECMAScript))
            {
                param_thread.Clear();
                param_thread.Append("-j ");
                param_thread.Append(txtThread.Text);
            }
            else
            {
                param_thread.Clear();
            }

            binary_path.Clear();
            if (checkPrecision_fp32.IsChecked == true)
            {
                binary_path.Append(".\\fp32\\waifu2x-ncnn-vulkan.exe ");
            }
            else
            {
                binary_path.Append(".\\waifu2x-ncnn-vulkan.exe ");
            }

            param_mag.Clear();
            param_mag.Append("-s ");
            param_mag.Append(this.slider_value.Text);

            param_denoise2.Clear();
            param_denoise2.Append(param_denoise.ToString());

            // Set mode
            if (param_mode.ToString() == "noise")
            {
                param_mag.Clear();
                param_mag.Append("-s ");
                param_mag.Append("1");
            }
            if (param_mode.ToString() == "scale")
            {
                param_denoise2.Clear();
                param_denoise2.Append("-1");
            }

            string full_param = String.Join(" ",
                "-v ",
                "-i \"%~1\"",
                "-o \"%~2\"",
                param_mag.ToString(),
                "-n %Noise_level%",
                param_block.ToString(),
                param_model.ToString(),
                param_gpu_id.ToString(),
                param_thread.ToString());

            string multiple_full_param = String.Join(" ",
                "-v ",
                "-i %Temporary_input%",
                "-o %Temporary_output%",
                "-s 2",
                "-n %Temporary_noise_level%",
                param_block.ToString(),
                param_model.ToString(),
                param_gpu_id.ToString(),
                param_thread.ToString());

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

            Guid g = System.Guid.NewGuid();
            random32.Clear();
            random32.Append(g.ToString("N").Substring(0, 32));

            Commandline.Append("@echo off\r\n");
            Commandline.Append("chcp 65001 >nul\r\n");
            Commandline.Append("if \"%~1\"==\"sub_rename\" goto sub_rename\r\n");
            Commandline.Append("set \"FileCount=" + FileCount + "\"\r\n");
            Commandline.Append("set \"ProcessedCount=0\"\r\n");
            Commandline.Append("set \"mode=" + param_mode.ToString() + "\"\r\n");
            Commandline.Append("set \"Scale_ratio=" + this.slider_zoom.Value.ToString() + "\"\r\n");
            Commandline.Append("set \"Noise_level=" + param_denoise2 + "\"\r\n");
            Commandline.Append("set \"random32=" + random32.ToString() +"\"\r\n");
            Commandline.Append("if \"%mode%\"==\"noise\" set Scale_ratio=1\r\n");
            Commandline.Append("set \"Output_no_overwirit=" + checkOutput_no_overwirit.IsChecked.ToString() + "\"\r\n");
            Commandline.Append("set \"Prevent_double_extensions=" + checkPrevent_double_extensions.IsChecked.ToString() + "\"\r\n");
            Commandline.Append("if not \"%FileCount%\"==\"1\" echo progress %ProcessedCount%/%FileCount%\r\n");
            for (int i = 0; i < param_src.Length; i++)
            {
                param_dst.Clear();
                if (this.txtDstPath.Text.Trim() != "") {
                    param_dst.Append("\"");
                    param_dst.Append(this.txtDstPath.Text);
                    param_dst.Append("\\");
                    param_dst.Append(System.IO.Path.GetFileNameWithoutExtension(param_src[i].Replace("%", "%%%%")));
                    if (File.Exists(param_src[i]))
                        {
                            param_dst.Append(".png\"");
                        }
                    else if (Directory.Exists(param_src[i]))
                        {
                            param_dst.Append("\"");
                        }
                } else {
                    param_dst.Append("\"");
                    System.IO.DirectoryInfo hDirInfo = System.IO.Directory.GetParent(param_src[i]);
                    param_dst.Append(hDirInfo.FullName);
                    param_dst.Append("\\");
                    param_dst.Append(System.IO.Path.GetFileNameWithoutExtension(param_src[i].Replace("%", "%%%%")));
                    if (param_model.ToString().Replace("-m ", "") == "models-cunet")
                       { param_dst.Append("(CUnet)"); }
                    if (param_model.ToString().Replace("-m ", "") == "models-upconv_7_anime_style_art_rgb")
                       { param_dst.Append("(UpRGB)"); }
                    if (param_model.ToString().Replace("-m ", "") == "models-upconv_7_photo")
                       { param_dst.Append("(UpPhoto)"); }
                    param_dst.Append("(");
                    param_dst.Append(param_mode.ToString().Replace("-m ", ""));
                    param_dst.Append(")");
                    if (param_mode.ToString() == "noise" || param_mode.ToString() == "noise_scale")
                    {
                        param_dst.Append("(");
                        param_dst.Append("Level");
                        param_dst.Append(param_denoise.ToString());
                        param_dst.Append(")");
                    }

                    if (param_mode.ToString() == "scale" || param_mode.ToString() == "noise_scale")
                    {
                        param_dst.Append("(x");
                        param_dst.Append(this.slider_zoom.Value.ToString());
                        param_dst.Append(")");
                    }
                    if (File.Exists(param_src[i]))
                       {
                          param_dst.Append(".png\"");
                       }
                       else if (Directory.Exists(param_src[i]))
                       {
                        param_dst.Append("\"");
                       }
                }
                Commandline.Append("call :waifu2x_run " + "\"" + param_src[i].Replace("%", "%%%%") + "\" " + param_dst + "\r\n");
            }
            Commandline.Append("exit /b\r\n");
            Commandline.Append("\r\n");
            Commandline.Append(":waifu2x_run\r\n");
            Commandline.Append("if \"%Output_no_overwirit%\"==\"True\" if exist \"%~2\" goto waifu2x_run_skip\r\n");
            Commandline.Append("for %%i in (\"%~2\") do set \"Output_name=%%~ni\"\r\n");
            Commandline.Append("for %%i in (\"%~1\") do set \"Attribute=%%~ai\"\r\n");
            Commandline.Append("if %Scale_ratio% LEQ 2 if \"%Attribute:~0,1%\"==\"d\" if not exist \"%~2\" (\r\n");
            Commandline.Append("   echo mkdir \"%~2\"\r\n"); 
            Commandline.Append("   mkdir \"%~2\"\r\n"); 
            Commandline.Append(")\r\n");
            Commandline.Append("set Temporary_input=\"%~1\"\r\n");
            Commandline.Append("if %Scale_ratio% GTR 2 (\r\n");
            Commandline.Append("    for %%i in (2,4,8,16,32,64,128,256) do call :sub_multiple_magnify %%i\r\n");
            Commandline.Append(") else (\r\n");
            Commandline.Append("    echo " + binary_path + full_param + "\r\n");
            Commandline.Append("    " + binary_path + full_param + "\r\n");
            Commandline.Append(")\r\n");
            Commandline.Append("if %Scale_ratio% LEQ 2 if \"%Attribute:~0,1%\"==\"d\" if \"%Prevent_double_extensions%\"==\"True\" (\r\n");
            Commandline.Append("    pushd \"%~2\"\r\n");
            Commandline.Append("    PowerShell \"Get-ChildItem *.png | Rename-Item -NewName { $_.Name -replace '(\\.png|\\.jpe?g|\\.bmp|\\.gif|\\.tiff?|\\.webp)\\.png$','.png' }\"\r\n");
            Commandline.Append("    popd\r\n");
            Commandline.Append(")\r\n");
            Commandline.Append("if %Scale_ratio% GTR 2 (\r\n");
            Commandline.Append("    move /y %Temporary_output% \"%~2\" >nul 2>&1\r\n");
            Commandline.Append("    rd /s /q \"%TEMP%\\waifu2x_%random32%\\\"\r\n");
            Commandline.Append(")\r\n");
            Commandline.Append(":waifu2x_run_skip\r\n");
            Commandline.Append("set /a ProcessedCount=%ProcessedCount%+1\r\n");
            Commandline.Append("if not \"%FileCount%\"==\"1\" echo progress %ProcessedCount%/%FileCount%\r\n");
            Commandline.Append("exit /b\r\n");
            Commandline.Append("\r\n");
            Commandline.Append(":sub_multiple_magnify\r\n");
            Commandline.Append("if %~1 GTR %Scale_ratio% exit /b\r\n");
            Commandline.Append("set Temporary_output=\"%TEMP%\\waifu2x_%random32%\\%Output_name%_x%~1.png\"\r\n");
            Commandline.Append("if \"%Attribute:~0,1%\"==\"d\" set Temporary_output=\"%TEMP%\\waifu2x_%random32%\\%Output_name%_x%~1\"\r\n");
            Commandline.Append("if \"%Attribute:~0,1%\"==\"d\" mkdir %Temporary_output%\r\n");
            Commandline.Append("if not \"%Attribute:~0,1%\"==\"d\" if not exist \"%TEMP%\\waifu2x_%random32%\\\" mkdir \"%TEMP%\\waifu2x_%random32%\\\"\r\n");
            Commandline.Append("set Temporary_noise_level=-1\r\n");
            Commandline.Append("if \"%~1\"==\"2\" if not \"%Noise_level%\"==\"-1\" set \"Temporary_noise_level=%Noise_level%\"\r\n");
            Commandline.Append("echo " + binary_path + multiple_full_param + "\r\n");
            Commandline.Append(binary_path + multiple_full_param + "\r\n");
            Commandline.Append("if \"%Attribute:~0,1%\"==\"d\" if \"%Prevent_double_extensions%\"==\"True\" (\r\n");
            Commandline.Append("    pushd %Temporary_output%\r\n");
            Commandline.Append("    PowerShell \"Get-ChildItem *.png | Rename-Item -NewName { $_.Name -replace '(\\.png|\\.jpe?g|\\.bmp|\\.gif|\\.tiff?|\\.webp)\\.png$','.png' }\"\r\n");
            Commandline.Append("    popd\r\n");
            Commandline.Append(")\r\n");
            Commandline.Append("set Temporary_input=%Temporary_output%\r\n");
            Commandline.Append("exit /b\r\n");
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
