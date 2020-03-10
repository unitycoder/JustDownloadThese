using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace JustDownloadThese
{

    public struct Downloadable
    {
        public string Url { get; set; }
    }

    public partial class MainWindow : Window
    {
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

            Console.WriteLine(srcRows.Length);

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

                    // if already exists, skip
                    if (skipIfExists == true)
                    {
                        if (File.Exists(outfile)) continue;
                    }

                    Console.WriteLine("downloading " + i + "/" + files.Count + " : " + files[i].Url + " into " + outfile);

                    client.DownloadFile(files[i].Url, outfile);
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

    } // class
} // namespace
