using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

using Dropbox.Api;
using Dropbox.Api.Files;

using ExactOnline.Client.Models;
using ExactOnline.Client.Sdk.Controllers;

namespace File_Synchronization_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Variable Declaration

        private string db_AccessToken;
        private string db_UserId;
        private DropboxClient db_Client;
        List<string> db_FolderList = new List<string>();
        List<string> db_FileList = new List<string>();

        private string eo_AccessToken;
        private ExactOnlineClient eo_Client;

        #endregion Variable Declaration

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.txtDropbox_AppKey.Focus();
            //this.txtDropbox_AppKey.Text = "e6crimo2b8fzllx";
            //this.txtExactOnline_ClientId.Text = "bed14a39-f723-4580-bc2c-f43640adfb1e";
            //this.txtExactOnline_ClientSecret.Text = "j5AU5sMkaBXP";
            //this.txtExactOnline_CallbackUrl.Text = "http://localhost/oauth2redirect";
            //this.txtExactOnline_EndPoint.Text = "https://start.exactonline.co.uk";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void btnSyncFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.grpboxDropbox.Header = "Dropbox: Not Connected";
                this.grpboxExactOnline.Header = "Exact Online: Not Connected";

                #region Dropbox

                #region Validation

                if (string.IsNullOrEmpty(this.txtDropbox_AppKey.Text))
                {
                    MessageBox.Show("Please input Dropbox's App Key!");
                    this.txtDropbox_AppKey.Focus();
                    return;
                }

                #endregion Validation

                Authenticate frmDB = new Authenticate();
                frmDB.AuthType = Authenticate.AuthenticateTypeList.Dropbox;
                frmDB.db_AppKey = this.txtDropbox_AppKey.Text; // "e6crimo2b8fzllx";
                frmDB.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                frmDB.ShowDialog();

                if (!frmDB.Result)
                {
                    MessageBox.Show("Dropbox Authentication failed!");
                    this.grpboxDropbox.Header = "Dropbox: Authentication Failed";
                    return;
                }
                else
                {
                    //MessageBox.Show("Dropbox Authentication success!");
                    this.grpboxDropbox.Header = "Dropbox: Connected";
                    db_AccessToken = frmDB.db_AccessToken;
                    db_UserId = frmDB.db_UserId;
                    db_Client = frmDB.db_Client;
                }

                #endregion Dropbox

                #region Exact Online

                #region Validation

                if (string.IsNullOrEmpty(this.txtExactOnline_ClientId.Text))
                {
                    MessageBox.Show("Please input Exact Online's Client Id!");
                    this.txtExactOnline_ClientId.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(this.txtExactOnline_ClientSecret.Text))
                {
                    MessageBox.Show("Please input Exact Online's Client Secret!");
                    this.txtExactOnline_ClientSecret.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(this.txtExactOnline_CallbackUrl.Text))
                {
                    MessageBox.Show("Please input Exact Online's Call Back URL!");
                    this.txtExactOnline_CallbackUrl.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(this.txtExactOnline_EndPoint.Text))
                {
                    MessageBox.Show("Please input Exact Online's End Point!");
                    this.txtExactOnline_EndPoint.Focus();
                    return;
                }

                #endregion Validation

                Authenticate frmEO = new Authenticate();
                frmEO.AuthType = Authenticate.AuthenticateTypeList.ExactOnline;
                frmEO.eo_ClientId = this.txtExactOnline_ClientId.Text; // "bed14a39-f723-4580-bc2c-f43640adfb1e";
                frmEO.eo_ClientSecret = this.txtExactOnline_ClientSecret.Text; // "j5AU5sMkaBXP";
                frmEO.eo_CallbackUrl = this.txtExactOnline_CallbackUrl.Text; //"http://localhost/oauth2redirect";
                frmEO.eo_EndPoint = this.txtExactOnline_EndPoint.Text; // "https://start.exactonline.co.uk";
                frmEO.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                frmEO.ShowDialog();

                if (!frmEO.Result)
                {
                    MessageBox.Show("Exact Online Authentication failed!");
                    this.grpboxExactOnline.Header = "Exact Online: Authentication Failed";
                    return;
                }
                else
                {
                    //MessageBox.Show("Exact Online Authentication success!");
                    this.grpboxExactOnline.Header = "Exact Online: Connected";
                    eo_AccessToken = frmEO.eo_AccessToken;
                    eo_Client = frmEO.eo_Client;
                }

                #endregion Exact Online

                #region File Synchronization

                db_FolderList.Clear(); //Clear the list
                db_FileList.Clear(); //Clear the list

                await Dropbox_ListFolder(db_Client, string.Empty); //Get all the folders form Dropbox

                if (db_FolderList.Count > 0)
                {
                    foreach (string folder in db_FolderList)
                    {
                        await Dropbox_ListFile(db_Client, folder); //Get all the files from specific folder at Dropbox
                    }
                }

                if (db_FileList.Count > 0)
                {
                    foreach (string filePath in db_FileList)
                    {
                        var fields = new[] { "ID, Subject, Type, Category" };
                        var documents = eo_Client.For<Document>().Select(fields).Top(5).Where("Subject+eq+'" + filePath + "'").Get();

                        if (documents.Count <= 0)
                        {
                            byte[] fileByte = await Dropbox_Download(db_Client, filePath); //Download the file from Dropbox

                            //Insert Record into Exact Online
                            Document document = new Document
                            {
                                Subject = filePath,
                                Body = Convert.ToBase64String(fileByte),
                                Category = GetCategoryId(eo_Client),
                                Type = 55, //Miscellaneous
                                DocumentDate = DateTime.Now.Date
                            };

                            var created = eo_Client.For<Document>().Insert(ref document);
                            if (!created)
                            {
                                MessageBox.Show("Upload " + filePath + " failed!");
                            }
                        }
                    }
                }

                #endregion File Synchronization

                MessageBox.Show("File synchronization process done!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private async Task Dropbox_ListFolder(DropboxClient client, string path)
        {
            var list = await client.Files.ListFolderAsync(path);

            foreach (var item in list.Entries.Where(i => i.IsFolder))
            {
                string folder = (string.IsNullOrEmpty(path) ? "" : path) + "/" + item.Name;
                db_FolderList.Add(folder);
                await Dropbox_ListFolder(db_Client, folder);
            }
        }

        private async Task Dropbox_ListFile(DropboxClient client, string path)
        {
            var list = await client.Files.ListFolderAsync(path);

            foreach (var item in list.Entries.Where(i => i.IsFile))
            {
                db_FileList.Add(path + "/" + item.Name);
            }
        }

        private async Task<byte[]> Dropbox_Download(DropboxClient client, string filePath)
        {
            byte[] file;
            using (var response = await client.Files.DownloadAsync(filePath))
            {
                file = await response.GetContentAsByteArrayAsync();
            }

            return file;
        }

        private static Guid GetCategoryId(ExactOnlineClient client)
        {
            var categories = client.For<DocumentCategory>().Select("ID").Where("Description+eq+'General'").Get();
            var category = categories.First().ID;
            return category;
        }

    }
}
