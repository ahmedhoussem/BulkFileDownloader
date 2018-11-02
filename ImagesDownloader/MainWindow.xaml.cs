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

namespace ImagesDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        const string URL = @"C:\Users\Bloodthirst\Desktop\Python\DownloadedFiles\image";


        //The URLs Extracted from the file
        List<string> lines = new List<string>();

        string Folder = String.Empty;

        public MainWindow()
        {

            InitializeComponent();


        }

        private void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            var FD = new OpenFileDialog();

            if (FD.ShowDialog() != null )
            {
                string fileToOpen = FD.FileName;

                URLValue.Text = fileToOpen;
            }

            lines = System.IO.File.ReadAllLines(URLValue.Text).ToList();

            foreach (string line in lines)
            {
                FilesList.Items.Add(line);
            }



        }

        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            await DownloadFilesAsync();
            FilesCounter.Text = "Complete";
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "My Title";
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
                Folder = dlg.FileName;
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

                    var path = Folder + @"\" + line.Key + "." + ext;

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
                        //FilesCounter.Text = count + @"/" + lines.Count;
                    });

                    try
                    {
                        //Lauch the download
                        await client.DownloadFileTaskAsync(uri, path);
                    }
                    catch (Exception ex)
                    {
                        //An error occurent during the file downlaod : 404 response , access problems , etc ...
                    }
                }
            });
        }
    }

}
