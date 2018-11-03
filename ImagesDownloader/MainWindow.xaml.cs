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
using System.Web;
using System.Net;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace ImagesDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window , INotifyPropertyChanged
    {
        #region Properties to bind for MVVM


        readonly object ThreadLockMutex = new object();

        private int _Counter;

        public int Counter
        {
            get { return _Counter; }
            set { _Counter = value; OnPropertyRaised("Counter"); OnPropertyRaised("Progress"); }
        }

        public string Progress
        {
            get
            {
                if(lines.Count == 0)
                {
                    return "Waiting ...";
                }
                if(_Counter == lines.Count)
                {
                    return "Completed !";
                }

                return _Counter + " / " + lines.Count ;
            }
        }

        //The URLs Extracted from the file
        private ObservableCollection<string> _lines = new ObservableCollection<string>();

        public ObservableCollection<string> lines
        {
            get { return _lines; }
            set { _lines = value; OnPropertyRaised("lines"); }
        }

        public bool CanDownload
        {
            get { return (String.Empty != TextFilePath && String.Empty != DownloadFolderPath); }
        }


        //Download folder path
        private string _DownloadFolderPath = String.Empty;

        public string DownloadFolderPath
        {
            get { return _DownloadFolderPath; }
            set { _DownloadFolderPath = value; OnPropertyRaised("DownloadFolderPath"); OnPropertyRaised("CanDownload");  }
        }


        //Text file containing URLs
        private string _TextFilePath = String.Empty;


        public string TextFilePath
        {
            get { return _TextFilePath; }
            set { _TextFilePath = value; OnPropertyRaised("TextFilePath"); OnPropertyRaised("CanDownload"); }
        }


        #endregion

        #region MVVM region
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyRaised(string propertyname)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        #endregion

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();


        }

        private void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            var FD = new OpenFileDialog();

            if (FD.ShowDialog() != null )
            {
                string fileToOpen = FD.FileName;

                if(fileToOpen != String.Empty)
                    TextFilePath = fileToOpen;
            }

            lines.Clear();

            foreach (string line in System.IO.File.ReadAllLines(TextFilePath).ToList())
            {
                lines.Add(line);
            }



        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(DownloadFilesAsync);
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Select Download Folder ...";
            dlg.IsFolderPicker = true;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = false;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DownloadFolderPath = dlg.FileName;
                // Do something with selected folder string
            }
        }

        private async Task DownloadFilesAsync()
        {
            //Convert the lines from list to to dictionary to keep the names indexed

            var dict = lines.ToDictionary(x => lines.IndexOf(x), x => x);

            //Execute in parallel to save time and use the bandwdith to our advantage

            Parallel.ForEach(dict, async (line) =>
            {

                using (var client = new WebClient())
                {
                    var uri = new Uri(line.Value);

                    var ext = line.Value.Split('.').Last();

                    var path = DownloadFolderPath + @"\" + line.Key + "." + ext;

                    //set up the on downlaod complete event
                    client.DownloadFileCompleted += ((obj, evtArgs) =>
                    {

                        //This is just a section i added to handle corrupted files which are usually less than 5Kb in size
                        try
                        {
                            var length = new System.IO.FileInfo(path).Length;

                            if (length < 5000)
                            {
                                System.IO.File.Delete(path);
                            }
                        }

                        catch
                        {


                        }

                        //Increment file counter
                        lock(ThreadLockMutex)
                        {
                            Counter++;
                        }
                        
                    });

                    try
                    {
                        //Lauch the download
                        await client.DownloadFileTaskAsync(uri, path);
                    }
                    catch (Exception ex)
                    {
                        //An error occured during the file downlaod : 404 response , access problems , etc ...
                    }
                }
            });
        }
    }

}
