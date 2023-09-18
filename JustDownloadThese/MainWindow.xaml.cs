using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace JustDownloadThese
{

    public struct Downloadable
    {
        public string Url { get; set; }
    }

    public partial class MainWindow : Window
    {
        const string appName = "JustDownloadThese";

        List<Downloadable> files = new List<Downloadable>();

        string downloadFolder = null;
        bool skipIfExists = false;

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }

        void Start()
        {
            LoadSettings();

            skipIfExists = (bool)chkSkipIfExists.IsChecked;

            // get download folder
            SHGetKnownFolderPath(KnownFolder.Downloads, 0, IntPtr.Zero, out downloadFolder);
            txtDownloadFolder.Text = downloadFolder;
        }



        private void BtnDownload_Click(object sender, RoutedEventArgs e)
        {
            // get source data
            var src = txtSource.Text;

            // for it should be line by line data, with each row containing file download url (example: http://asdfg.asdf/file123.zip )
            var srcRows = src.Split('\n');

            //Console.WriteLine(srcRows.Length);

            // process to data
            for (int i = 0; i < srcRows.Length; i++)
            {
                var f = new Downloadable();
                f.Url = srcRows[i];
                files.Add(f);
            }

            // put in grid
            gridFiles.ItemsSource = files;

            // start downloading in background thread
            thread = new Thread(new ThreadStart(DownloadThread));
            thread.Start();
        }

        void SetRowColor(int i, System.Windows.Media.Color color)
        {
            var row = (DataGridRow)gridFiles.ItemContainerGenerator.ContainerFromIndex(i);
            var item = (Downloadable)gridFiles.Items[i];
            row.Background = new System.Windows.Media.SolidColorBrush(color);
        }

        Thread thread;
        void DownloadThread()
        {
            using (WebClient client = new WebClient())
            {
                for (int i = 0; i < files.Count; i++)
                //for (int i = 0; i < 2; i++)
                {
                    var dl = files[i];
                    if (string.IsNullOrEmpty(dl.Url)) continue;

                    string outfile = Path.Combine(downloadFolder, GetFileNameFromUrl(dl.Url));

                    // check that folder exists, create if missing any part
                    if (Directory.Exists(Path.GetDirectoryName(outfile)) == false) Directory.CreateDirectory(Path.GetDirectoryName(outfile));

                    // if file already exists, skip
                    if (skipIfExists == true)
                    {
                        if (File.Exists(outfile))
                        {
                            // invoke UI thread to change row color
                            Dispatcher.Invoke(() => SetRowColor(i, System.Windows.Media.Colors.DarkGreen));
                            continue;
                        }
                    }

                    Console.WriteLine("downloading " + i + "/" + files.Count + " : " + files[i].Url + " into " + outfile);

                    // TODO handle "System.Net.WebException: 'The operation has timed out.'"
                    try
                    {
                        Dispatcher.Invoke(() => SetRowColor(i, System.Windows.Media.Colors.Orange));
                        client.DownloadFile(files[i].Url, outfile);
                        Dispatcher.Invoke(() => SetRowColor(i, System.Windows.Media.Colors.Green));
                    }
                    catch (Exception e)
                    {
                        Dispatcher.Invoke(() => SetRowColor(i, System.Windows.Media.Colors.Red));
                        Console.WriteLine(e.Message);
                    }
                }
            }
            Console.WriteLine("Download Thread Closing..");
        }

        // https://stackoverflow.com/a/40361205/5452781
        static string GetFileNameFromUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                uri = new Uri(new Uri("http://dummy.dummy"), url);
            }

            return Path.GetFileName(uri.LocalPath);
        }


        // https://stackoverflow.com/a/7672816/5452781
        public static class KnownFolder
        {
            public static readonly Guid Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath);

        private void btnParseUrl_Click(object sender, RoutedEventArgs e)
        {
            // fetch html source from url
            var url = txtUrl.Text;
            var html = new WebClient().DownloadString(url);

            Console.WriteLine("parsing..");

            // rexeg find all urls from html
            var regex = new System.Text.RegularExpressions.Regex(@"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?");
            var matches = regex.Matches(html);

            int count = 0;

            // TODO handle full files
            for (int i = 0; i < matches.Count; i++)
            {


            }

            // find relative files from hrefs
            var hrefRegex = new System.Text.RegularExpressions.Regex(@"href=""(.*?)""");
            var hrefMatches = hrefRegex.Matches(html);


            for (int i = 0; i < hrefMatches.Count; i++)
            {
                // regex match href string with txt file extension
                var href = hrefMatches[i].Value;
                var findStr = @"href=""(.*?\###)""".Replace("###", txtFilter.Text);
                var hrefRegex2 = new System.Text.RegularExpressions.Regex(findStr);
                var hrefMatches2 = hrefRegex2.Matches(href);
                for (int k = 0; k < hrefMatches2.Count; k++)
                {
                    // take string inside ""
                    var href2 = hrefMatches2[k].Value;
                    var hrefRegex3 = new System.Text.RegularExpressions.Regex(@"""(.*?)""");
                    var hrefMatches3 = hrefRegex3.Matches(href2);

                    var filename = hrefMatches3[0].Value.Replace(@"""", "");
                    // add absolute url
                    var fileurl = url + filename;

                    Console.WriteLine(fileurl);
                    txtSource.Text += fileurl + "\n";
                    count++;
                }
            }

            lblCount.Content = count;

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSettings();
        }

        void LoadSettings()
        {
            try
            {
                txtUrl.Text = Properties.Settings.Default.txtUrl;
                txtFilter.Text = Properties.Settings.Default.txtFilter;
                txtDownloadFolder.Text = Properties.Settings.Default.txtDownloadFolder;
                chkSkipIfExists.IsChecked = Properties.Settings.Default.boolSkipExisting;
            }
            catch (ConfigurationErrorsException ex)
            {
                string filename = ((ConfigurationErrorsException)ex.InnerException).Filename;

                if (MessageBox.Show("This may be due to a Windows crash/BSOD.\n" +
                                      "Click 'Yes' to use automatic backup (if exists, otherwise settings are reset), then start application again.\n\n" +
                                      "Click 'No' to exit now (and delete user.config manually)\n\nCorrupted file: " + filename,
                                      appName + " - Corrupt user settings",
                                      MessageBoxButton.YesNo,
                                      MessageBoxImage.Error) == MessageBoxResult.Yes)
                {

                    // try to use backup
                    string backupFilename = filename + ".bak";
                    if (File.Exists(backupFilename))
                    {
                        File.Copy(backupFilename, filename, true);
                    }
                    else
                    {
                        File.Delete(filename);
                    }
                }
                // need to restart, otherwise settings not loaded
                Process.GetCurrentProcess().Kill();
            }
        }

        void SaveSettings()
        {
            Properties.Settings.Default.txtUrl = txtUrl.Text;
            Properties.Settings.Default.txtFilter = txtFilter.Text;
            Properties.Settings.Default.txtDownloadFolder = txtDownloadFolder.Text;
            Properties.Settings.Default.boolSkipExisting = (bool)chkSkipIfExists.IsChecked;

            Properties.Settings.Default.Save();
        }
    } // class
} // namespace
