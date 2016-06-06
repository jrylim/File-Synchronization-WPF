using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Dropbox.Api;

using ExactOnline.Client.Models;
using ExactOnline.Client.Sdk.Controllers;
using ExactOnline.Client.OAuth;

namespace File_Synchronization_WPF
{
    /// <summary>
    /// Interaction logic for Authenticate.xaml
    /// </summary>
    public partial class Authenticate : Window
    {
        #region Variable Declaration

        public enum AuthenticateTypeList
        {
            Dropbox
            , ExactOnline
        }

        public AuthenticateTypeList AuthType { get; set; }

        public string eo_ClientId { get; set; }
        public string eo_ClientSecret { get; set; }
        public string eo_CallbackUrl { get; set; }
        public string eo_EndPoint { get; set; }
        public string eo_AccessToken { get; private set; }
        private UserAuthorization eo_Authorization;
        public ExactOnlineClient eo_Client { get; set; }

        public string db_AppKey { get; set; }
        public string db_AccessToken { get; private set; }
        public DropboxClient db_Client { get; set; }
        public string db_UserId { get; private set; }

        public bool Result { get; private set; }

        private const string db_RedirectUri = "https://localhost/authorize";
        private string db_Oauth2State;

        #endregion Variable Declaration

        public Authenticate()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Dropbox              

                if (AuthType == AuthenticateTypeList.Dropbox)
                {
                    this.Title = "Dropbox Authentication";
                    this.db_Oauth2State = Guid.NewGuid().ToString("N");
                    var authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Token, db_AppKey, new Uri(db_RedirectUri), state: db_Oauth2State);
                    this.webBrowser.Navigate(authorizeUri);
                }

                #endregion Dropbox

                #region Exact Online

                if (AuthType == AuthenticateTypeList.ExactOnline)
                {
                    this.Title = "Exact Online Authentication";
                    eo_Authorization = new UserAuthorization();

                    try
                    {
                        UserAuthorizations.Authorize(eo_Authorization, eo_EndPoint, eo_ClientId, eo_ClientSecret, new Uri(eo_CallbackUrl));
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Invalid Exact Online's Client Secret!");
                        this.Result = false;
                        this.Close();
                        return;
                    }

                    eo_AccessToken = eo_Authorization.AccessToken;

                    if (eo_AccessToken == null)
                    {
                        this.Result = false;
                        this.Close();
                        return;
                    }
                    else
                    {
                        this.Result = true;

                        try
                        {
                            eo_Client = new ExactOnlineClient(eo_EndPoint, GetAccessToken);
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Invalid Exact Online's End Point!");
                            this.Result = false;
                            this.Close();
                            return;
                        }

                        this.Close();
                        return;
                    }
                }

                #endregion Exact Online
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                this.Result = false;
                this.Close();
            }
        }

        private void webBrowser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            try
            {
                this.Result = false;

                #region Dropbox

                if (AuthType == AuthenticateTypeList.Dropbox)
                {
                    if (!e.Uri.ToString().StartsWith(db_RedirectUri, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    OAuth2Response result = DropboxOAuth2Helper.ParseTokenFragment(e.Uri);
                    if (result.State != this.db_Oauth2State)
                    {
                        return;
                    }

                    this.db_AccessToken = result.AccessToken;
                    this.db_UserId = result.Uid;
                    this.Result = true;

                    var httpClient = new HttpClient()
                    {
                        Timeout = TimeSpan.FromMinutes(20)
                    };

                    var config = new DropboxClientConfig("SimpleTestApp")
                    {
                        HttpClient = httpClient
                    };

                    db_Client = new DropboxClient(this.db_AccessToken, config);

                    this.Close();
                }

                #endregion Dropbox
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                this.Result = false;
                this.Close();
            }
        }

        private string GetAccessToken()
        {
            return eo_AccessToken;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Result = false;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                this.Result = false;
                this.Close();
            }
        }

    }
}
