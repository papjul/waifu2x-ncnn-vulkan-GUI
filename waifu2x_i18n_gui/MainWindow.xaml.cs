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
using System.Threading;
using System.Windows.Threading;
using System.Text.RegularExpressions;

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
            if (Properties.Settings.Default.mode == "auto_scale")
            { btnModeAutoScale.IsChecked = true; }

            btnCUnet.IsChecked = true;

            if (Properties.Settings.Default.model == "models-cunet")
            { btnCUnet.IsChecked = true; }
            if (Properties.Settings.Default.model == "models-upconv_7_anime_style_art_rgb")
            { btnUpRGB.IsChecked = true; }
            if (Properties.Settings.Default.model == "models-upconv_7_photo")
            { btnUpPhoto.IsChecked = true; }
            checkTTAmode.IsChecked = Properties.Settings.Default.TTAmode;
            checkSoundBeep.IsChecked = Properties.Settings.Default.SoundBeep;
            checkStore_output_dir.IsChecked = Properties.Settings.Default.store_output_dir;
            checkOutput_no_overwirit.IsChecked = Properties.Settings.Default.output_no_overwirit;
            checkPrecision_fp32.IsChecked = Properties.Settings.Default.Precision_fp32;
            slider_value.Text = Properties.Settings.Default.scale_ratio;
            slider_zoom.Value = double.Parse(Properties.Settings.Default.scale_ratio);

        }
        System.Diagnostics.Stopwatch Stopwatch = new System.Diagnostics.Stopwatch();
        // public static StringBuilder param_src= new StringBuilder("");
        public static StringBuilder param_dst = new StringBuilder("");
        public static StringBuilder param_dst_suffix = new StringBuilder("");
        public static StringBuilder param_mag = new StringBuilder("2");
        public static StringBuilder param_denoise = new StringBuilder("");
        public static StringBuilder param_denoise2 = new StringBuilder("");
        public static StringBuilder param_model = new StringBuilder("models-cunet");
        public static StringBuilder param_block = new StringBuilder("100");
        public static StringBuilder param_mode = new StringBuilder("noise_scale");
        public static StringBuilder param_gpu_id = new StringBuilder("");
        public static StringBuilder param_thread = new StringBuilder("2");
        public static StringBuilder param_tta = new StringBuilder("");
        public static StringBuilder dast_dir = new StringBuilder("");
        public static StringBuilder binary_path = new StringBuilder("");
        public static String[] param_src;
        public static int scale_ratio;
        public DateTime starttimea;
        public static bool Cancel = false;
        public static bool Output_no_overwirit;

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
            Properties.Settings.Default.TTAmode = Convert.ToBoolean(checkTTAmode.IsChecked);
            Properties.Settings.Default.SoundBeep = Convert.ToBoolean(checkSoundBeep.IsChecked);
            Properties.Settings.Default.store_output_dir = Convert.ToBoolean(checkStore_output_dir.IsChecked);
            Properties.Settings.Default.Precision_fp32 = Convert.ToBoolean(checkPrecision_fp32.IsChecked);
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
                Properties.Settings.Default.block_size = "100";
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
                @"^\d+$",
                System.Text.RegularExpressions.RegexOptions.ECMAScript))
            {
                Properties.Settings.Default.thread = txtThread.Text;
            }
            else
            {
                Properties.Settings.Default.thread = "2";
            }

            Properties.Settings.Default.Save();

            try
            {
                foreach (var process in Process.GetProcessesByName("waifu2x-ncnn-vulkan"))
                {
                    process.Kill();
                }
            }
            catch { }
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
                "Version 2.0.0\n" +
                "BuildDate: 14 Mar,2020\n" +
                "License: MIT License";
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
                dast_dir.Clear();
            }
        }

        private void OnDstClear(object sender, RoutedEventArgs e)
        {
            this.txtDstPath.Clear();
            dast_dir.Clear();
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

        private async void On_SrcDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fn = (string[])e.Data.GetData(DataFormats.FileDrop);
                this.txtSrcPath.Clear();
                dast_dir.Clear();
                param_src = null;
                var reg = new Regex(@".+\.(jpe?g|png|bmp|gif|tiff?|webp)$");
                var list = new List<string>();
                foreach (var dropfile in fn)
                {
                    if (File.Exists(dropfile))
                    {
                        list.Add(dropfile);
                        txtSrcPath.AppendText(dropfile + "\r\n");
                    }
                    if (Directory.Exists(dropfile))
                    {
                        await Task.Run(() =>
                        {
                            var Directoryfiles = Directory.GetFiles(dropfile).Where(f => reg.IsMatch(f)).ToArray();
                            foreach (var Directoryimage in Directoryfiles)
                            {
                                list.Add(Directoryimage);
                                txtSrcPath.Dispatcher.Invoke(() => txtSrcPath.AppendText(Directoryimage + "\r\n"));
                            }
                        });
                        dast_dir.Clear();
                        dast_dir.Append(dropfile);
                        this.txtDstPath.Clear();
                        DstDirName();
                    }
                }
                param_src = list.ToArray();
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
        private void DstDirNameProxy(object sender, System.EventArgs e)
        {
            DstDirName();
        }
        private void DstDirName()
        {
            if (dast_dir.ToString().Trim() == "")
            {return;}

            txtDstPath.Clear();
            txtDstPath.AppendText(dast_dir.ToString());
            if (param_model.ToString().Replace("-m ", "") == "models-cunet")
            { txtDstPath.AppendText("(CUnet)"); }
            if (param_model.ToString().Replace("-m ", "") == "models-upconv_7_anime_style_art_rgb")
            { txtDstPath.AppendText("(UpRGB)"); }
            if (param_model.ToString().Replace("-m ", "") == "models-upconv_7_photo")
            { txtDstPath.AppendText("(UpPhoto)"); }
            txtDstPath.AppendText("(");
            txtDstPath.AppendText(param_mode.ToString().Replace("-m ", ""));
            txtDstPath.AppendText(")");
            if (param_mode.ToString() == "noise" || param_mode.ToString() == "noise_scale" || param_mode.ToString() == "auto_scale")
            {
                txtDstPath.AppendText("(");
                txtDstPath.AppendText("Level");
                txtDstPath.AppendText(param_denoise.ToString());
                txtDstPath.AppendText(")");
            }
            if (checkTTAmode.IsChecked == true)
            {
                txtDstPath.AppendText("(tta)");
            }

            if (param_mode.ToString() == "scale" || param_mode.ToString() == "noise_scale" || param_mode.ToString() == "auto_scale")
            {
                txtDstPath.AppendText("(x");
                txtDstPath.AppendText(this.slider_zoom.Value.ToString());
                txtDstPath.AppendText(")");
            }
            if (checkPrecision_fp32.IsChecked == true)
            {
                txtDstPath.AppendText("(FP32)");
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
            DstDirName();
        }
        private void OnDenoiseChecked(object sender, RoutedEventArgs e)
        {
            param_denoise.Clear();
            RadioButton optsrc= sender as RadioButton;
            param_denoise.Append(optsrc.Tag.ToString());
            DstDirName();
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
            DstDirName();
        }
        private async void OnAbort(object sender, RoutedEventArgs e)
        {
            Cancel = true;
            this.btnRun.IsEnabled = true;
            this.btnAbort.IsEnabled = false;
            try
            {
                foreach (var process in Process.GetProcessesByName("waifu2x-ncnn-vulkan"))
                {
                    process.Kill();
                }
            }
            catch { }
        }
        public void Encode(int maxConcurrency, int FileCount)
        {
            DateTime starttime = DateTime.Now;
            starttimea = starttime;
            string labelstring = FileCount.ToString();

            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(maxConcurrency))
            {
                
                string others_param = String.Join(" ",
                    param_model,
                    param_block,
                    param_tta,
                    param_gpu_id);

                pLabel.Dispatcher.Invoke(() => pLabel.Content = "0 / " + prgbar.Maximum, DispatcherPriority.Background);
                List<Task> tasks = new List<Task>();
                foreach (var input in param_src)
                {
                    concurrencySemaphore.Wait();

                    var t = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            string noise_level_temp = null;
                            if (Cancel == false)
                            {
                                string output = null;
                                if (Output_no_overwirit == true)
                                {
                                    if (param_dst.ToString().Trim() == "")
                                    {
                                        output = System.IO.Directory.GetParent(input) + "\\" + System.IO.Path.GetFileNameWithoutExtension(input) + param_dst_suffix;
                                    }
                                    else
                                    {
                                        output = param_dst + "\\" + System.IO.Path.GetFileNameWithoutExtension(input) + param_dst_suffix;
                                    }
                                    if (File.Exists(output))
                                    {
                                        prgbar.Dispatcher.Invoke(() => prgbar.Value += 1, DispatcherPriority.Background);
                                        TimeSpan timespent = DateTime.Now - starttime;
                                        pLabel.Dispatcher.Invoke(() => pLabel.Content = prgbar.Value + " / " + labelstring + " - Time Left: " + new TimeSpan(0, 0, Convert.ToInt32(Math.Round(((timespent.TotalSeconds / prgbar.Value) * (Int32.Parse(labelstring) - prgbar.Value)), MidpointRounding.ToEven))).ToString(@"hh\:mm\:ss"), DispatcherPriority.Background);
                                        return;
                                    }
                                }
                                Guid g = System.Guid.NewGuid();
                                string random32 = (g.ToString("N").Substring(0, 32));
                                noise_level_temp = param_denoise2.ToString();
                                if (!System.Text.RegularExpressions.Regex.IsMatch(System.IO.Path.GetExtension(input), @"\.jpe?g", RegexOptions.IgnoreCase)) if (param_mode.ToString() == "auto_scale")
                                {
                                    noise_level_temp = "-n -1";
                                }
                                Process process = new Process();
                                ProcessStartInfo startInfo = new ProcessStartInfo();
                                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                startInfo.UseShellExecute = true;
                                startInfo.FileName = System.Environment.GetEnvironmentVariable("ComSpec");
                                startInfo.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                                if (scale_ratio <= 2)
                                {
                                    if (param_dst.ToString().Trim() == "")
                                    {
                                        output = System.IO.Directory.GetParent(input) + "\\" + System.IO.Path.GetFileNameWithoutExtension(input) + param_dst_suffix;
                                    }
                                    else
                                    {
                                        output = param_dst + "\\" + System.IO.Path.GetFileNameWithoutExtension(input) + param_dst_suffix;
                                    }
                                    startInfo.Arguments = "/C " + binary_path + "-i \"" + input + "\" -o \"" + output + "\" " + param_mag + " " + noise_level_temp + " " + others_param;
                                    process.StartInfo = startInfo;
                                    Console.WriteLine(startInfo.Arguments);
                                    process.Start();
                                    process.WaitForExit();

                                    //Progressbar +1
                                    TimeSpan timespent = DateTime.Now - starttime;
                                    if (Cancel == false)
                                    {
                                        prgbar.Dispatcher.Invoke(() => prgbar.Value += 1, DispatcherPriority.Background);
                                        pLabel.Dispatcher.Invoke(() => pLabel.Content = prgbar.Value + " / " + labelstring + " - Time Left: " + new TimeSpan(0, 0, Convert.ToInt32(Math.Round(((timespent.TotalSeconds / prgbar.Value) * (Int32.Parse(labelstring) - prgbar.Value)), MidpointRounding.ToEven))).ToString(@"hh\:mm\:ss"), DispatcherPriority.Background);
                                    }
                                }
                                else
                                {
                                    int r;
                                    int r2;
                                    for (r = 2, r2 = 1; r <= scale_ratio; r = r * 2, r2 = r2 * 2)
                                    {
                                        string input_temp = System.IO.Path.GetTempPath() + random32 + "-" + r2 + "x.png";
                                        if (r == 2)
                                        {
                                            output = System.IO.Path.GetTempPath() + random32 + "-" + r + "x.png";
                                            startInfo.Arguments = "/C " + binary_path + " -i \"" + input + "\" -o \"" + output + "\" -s 2 " + noise_level_temp + " " + others_param;
                                        }
                                        else
                                        {
                                            output = System.IO.Path.GetTempPath() + random32 + "-" + r + "x.png";
                                            if (r == scale_ratio)
                                            {
                                                if (param_dst.ToString().Trim() == "")
                                                {
                                                    output = System.IO.Directory.GetParent(input) + "\\" + System.IO.Path.GetFileNameWithoutExtension(input) + param_dst_suffix;
                                                }
                                                else
                                                {
                                                    output = param_dst + "\\" + System.IO.Path.GetFileNameWithoutExtension(input) + param_dst_suffix;
                                                }
                                            }
                                            startInfo.Arguments = "/C " + binary_path + " -i \"" + input_temp + "\" -o \"" + output + "\" -s 2 -n -1 " + others_param;
                                        }
                                        process.StartInfo = startInfo;
                                        Console.WriteLine(startInfo.Arguments);
                                        process.Start();
                                        process.WaitForExit();
                                        new FileInfo(input_temp).Delete();
                                    }

                                    //Progressbar +1
                                    TimeSpan timespent = DateTime.Now - starttime;
                                    if (Cancel == false)
                                    {
                                        prgbar.Dispatcher.Invoke(() => prgbar.Value += 1, DispatcherPriority.Background);
                                        pLabel.Dispatcher.Invoke(() => pLabel.Content = prgbar.Value + " / " + labelstring + " - Time Left: " + new TimeSpan(0, 0, Convert.ToInt32(Math.Round(((timespent.TotalSeconds / prgbar.Value) * (Int32.Parse(labelstring) - prgbar.Value)), MidpointRounding.ToEven))).ToString(@"hh\:mm\:ss"), DispatcherPriority.Background);
                                    }
                                }
                            }
                        }

                        finally
                        {
                            concurrencySemaphore.Release();
                        }
                    });

                    tasks.Add(t);
                }
                Task.WaitAll(tasks.ToArray());
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

        async private void OnRun(object sender, RoutedEventArgs e)
        {
            Cancel = false;
            binary_path.Clear();
            if (checkPrecision_fp32.IsChecked == true)
            {
                if (!File.Exists("fp32\\waifu2x-ncnn-vulkan.exe"))
                {
                    MessageBox.Show(@"fp32\\waifu2x-ncnn-vulkan.exe is missing!");
                    return;
                }
                binary_path.Append(".\\fp32\\waifu2x-ncnn-vulkan.exe ");
            }
            else
            {
                if (!File.Exists("waifu2x-ncnn-vulkan.exe"))
                {
                    MessageBox.Show(@"waifu2x-ncnn-vulkan.exe is missing!");
                    return;
                }
                binary_path.Append(".\\waifu2x-ncnn-vulkan.exe ");
            }

            if (param_src == null)
            {
                MessageBox.Show(@"source images is not found!");
                return;
            }

            if (this.txtDstPath.Text.Trim() != "") if (!Directory.Exists(this.txtDstPath.Text))
            {
                try
                {
                    Directory.CreateDirectory(this.txtDstPath.Text);
                }
                catch
                {
                    MessageBox.Show(@"Failed to create folder!");
                    return;
                }
            }

            // Sets Source
            // The source must be a file or folder that exists

            param_tta.Clear();
            if (checkTTAmode.IsChecked == true)
            {
                param_tta.Append("-x");
            }

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
                 @"^(\d+)$",
               System.Text.RegularExpressions.RegexOptions.ECMAScript))
            {
                param_thread.Clear();
                param_thread.Append(txtThread.Text);
            }
            else
            {
                param_thread.Clear();
            }

            param_mag.Clear();
            param_mag.Append("-s ");
            param_mag.Append(this.slider_value.Text);

            param_denoise2.Clear();
            param_denoise2.Append("-n ");
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
                param_denoise2.Append("-n ");
                param_denoise2.Append("-1");
            }

            this.btnRun.IsEnabled = false;
            this.btnAbort.IsEnabled = true;

            int FileCount = 0;
            for (int i = 0; i < param_src.Length; i++)
            {
                FileCount++;
            }
            param_dst.Clear();
            param_dst.Append(this.txtDstPath.Text);

            param_dst_suffix.Clear();
            if (this.txtDstPath.Text.Trim() != "")
            {
                param_dst_suffix.Append(".png");
            }
            else
            {
                if (param_model.ToString().Replace("-m ", "") == "models-cunet")
                { param_dst_suffix.Append("(CUnet)"); }
                if (param_model.ToString().Replace("-m ", "") == "models-upconv_7_anime_style_art_rgb")
                { param_dst_suffix.Append("(UpRGB)"); }
                if (param_model.ToString().Replace("-m ", "") == "models-upconv_7_photo")
                { param_dst_suffix.Append("(UpPhoto)"); }
                param_dst_suffix.Append("(");
                param_dst_suffix.Append(param_mode.ToString().Replace("-m ", ""));
                param_dst_suffix.Append(")");
                if (param_mode.ToString() == "noise" || param_mode.ToString() == "noise_scale" || param_mode.ToString() == "auto_scale")
                {
                    param_dst_suffix.Append("(");
                    param_dst_suffix.Append("Level");
                    param_dst_suffix.Append(param_denoise.ToString());
                    param_dst_suffix.Append(")");
                }
                if (checkTTAmode.IsChecked == true)
                {
                    param_dst_suffix.Append("(tta)");
                }

                if (param_mode.ToString() == "scale" || param_mode.ToString() == "noise_scale" || param_mode.ToString() == "auto_scale")
                {
                    param_dst_suffix.Append("(x");
                    param_dst_suffix.Append(this.slider_zoom.Value.ToString());
                    param_dst_suffix.Append(")");
                }
                if (checkPrecision_fp32.IsChecked == true)
                {
                    param_dst_suffix.Append("(FP32)");
                }
                param_dst_suffix.Append(".png");
            }

            Output_no_overwirit = checkOutput_no_overwirit.IsChecked.Value;
            scale_ratio = (int)slider_zoom.Value;
            if (param_mode.ToString() == "noise")
            { scale_ratio = 1; }

            prgbar.Maximum = FileCount;
            prgbar.Value = 0;

            await Task.Run(() => Encode(int.Parse(param_thread.ToString()), FileCount));
            prgbar.Value = 0;
            pLabel.Dispatcher.Invoke(() => pLabel.Content = "Processing time: " + (DateTime.Now - starttimea).ToString(@"hh\:mm\:ss\.fff"), DispatcherPriority.Background);
            this.btnRun.IsEnabled = true;
            this.btnAbort.IsEnabled = false;

            if (checkSoundBeep.IsChecked == true)
            { System.Media.SystemSounds.Beep.Play(); }
        }


    }
}
