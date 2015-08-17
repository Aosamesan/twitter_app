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
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using OAuth;
using System.Net;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Threading;

namespace TwitterApp
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private string DateFormat { get; }

        private TwitterAuth twitterAuth;
        private TweetCollection collection;
        private RetweetStatusCollection retweetCollection;

        public string SelectedItemRetweetCountString
        {
            get
            {
                if (TweetListView.SelectedItem != null)
                {
                    Tweet item = TweetListView.SelectedItem as Tweet;

                    return $"RT : {item.RetweetCount}";
                }
                else
                {
                    return "Unselected";
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DateFormat
                = "ddd MMM dd HH:mm:ss zzzz yyyy";


            twitterAuth = Resources["TwitterAuthKey"] as TwitterAuth;
            collection = Resources["TweetCollectionKey"] as TweetCollection;
            retweetCollection = Resources["RetweetStatusCollectionKey"] as RetweetStatusCollection;

            this.Loaded += async (sender, e) => await twitterAuth.Init(this);
            TweetListView.SelectionChanged +=
                (s, e) => RetweetCountBlock.Text = SelectedItemRetweetCountString;
        }

        public string TweetDateConvert(string s) => Tweet.TweetDateConvert(s);
        public DateTime TweetDateConvert(string s, IFormatProvider fp) => Tweet.TweetDateConvert(s, fp);
        

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ShowGetTweetDialog();
        }

        public void ShowGetTweetDialog()
        {
            int count;
            string screenName;

            var settings = new MetroDialogSettings()
            {
                AnimateHide = true,
                AnimateShow = true
            };

            var dlg = new GetTweetDialog();
            dlg.OKButton.Click += async (s, e) =>
            {
                count = Convert.ToInt32(dlg.CountBox.Value);
                screenName = dlg.ScreenNameBox.Text;

                if (!string.IsNullOrWhiteSpace(screenName))
                {
                    dynamic json = await twitterAuth.GetJson($"https://api.twitter.com/1.1/statuses/user_timeline.json?screen_name={screenName}&count={count}", "GET");

                    if (json == null)
                        return;

                    collection.Clear();
                    RetweetCountBlock.Text = "Unselected";
                    TweetListView.SelectedItem = null;
                    foreach (var obj in json)
                    {
                        Tweet t = new Tweet(obj);
                        collection.Add(t);
                    }
                    if(TweetListView.Items.Count > 0)
                        TweetListView.ScrollIntoView(TweetListView.Items[0]);
                }

                await this.HideMetroDialogAsync(dlg, settings);
            };
            dlg.CancelButton.Click += (s, e) =>
            {
                this.HideMetroDialogAsync(dlg, settings);
            };

            DialogManager.ShowMetroDialogAsync(this, dlg, settings);
        }

        private async void TweetListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Tweet t = (sender as ListView).SelectedItem as Tweet;

            if (t?.ID != null)
            {
                dynamic retweetObjs = await twitterAuth.GetJson($"https://api.twitter.com/1.1/statuses/retweets/{t.ID}.json?count=30", "GET");

                if (retweetObjs != null)
                {
                    retweetCollection.Clear();
                    foreach (var v in retweetObjs)
                    {
                        RetweetStatus rs = new RetweetStatus(v);
                        retweetCollection.Add(rs);
                    }
                    if (retweetCollection.Count > 0)
                        RetweetStatusListView.ScrollIntoView(retweetCollection[0]);
                }
                else
                {
                    MessageBox.Show("retweetObjs is null");
                }
            }
            else
                MessageBox.Show("t.IsRetweeted is null or false");

        }
    }

    internal class TwitterAuth : INotifyPropertyChanged
    {
        private bool isInit;
        public static string ConsumerKey { get; }
        public static string ConsumerSecret { get; }
        private static string RequestTokenURL { get; }
        private static string RequestPinURL { get; }
        private static string AccessTokenURL { get; }
        private MainWindow parentWindow;
        public BitmapImage ProfileImage
        {
            get; private set;
        }
        public bool IsInit
        {
            get
            {
                return isInit;
            }
            private set
            {
                isInit = value;
                OnPropertyChanged("IsInit");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private Manager authMgr;

        static TwitterAuth()
        {
            ConsumerKey = "ConsumerKey";
            ConsumerSecret = "ConsumerSecret";

            RequestTokenURL = "https://api.twitter.com/oauth/request_token";
            RequestPinURL = "https://api.twitter.com/oauth/authorize?oauth_token=";
            AccessTokenURL = "https://api.twitter.com/oauth/access_token";
        }

        public TwitterAuth()
        {
            IsInit = false;
        }

        protected void OnPropertyChanged(string n)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public async Task<bool?> Init(MetroWindow win)
        {
            try
            {
                parentWindow = win as MainWindow;

                authMgr = new Manager();
                authMgr["consumer_key"] = ConsumerKey;
                authMgr["consumer_secret"] = ConsumerSecret;
                authMgr.AcquireRequestToken(RequestTokenURL, "POST");
                MetroDialogSettings settings = new MetroDialogSettings()
                {
                    AnimateHide = true,
                    AnimateShow = true,
                    AffirmativeButtonText = "확인",
                    NegativeButtonText = "종료"
                };

                MessageDialogResult result = await DialogManager.ShowMessageAsync(parentWindow, "인증", "인증을 위해 웹브라우저를 통해 트위터에 로그인합니다.",
                    MessageDialogStyle.AffirmativeAndNegative, settings);

                if (result == MessageDialogResult.Affirmative)
                {
                    System.Diagnostics.Process.Start($"{RequestPinURL}{authMgr["token"]}");
                    settings = new MetroDialogSettings()
                    {
                        AnimateHide = true,
                        AnimateShow = true,
                        AffirmativeButtonText = "인증",
                        NegativeButtonText = "취소 및 종료"
                    };
                    
                    string pin = await DialogManager.ShowInputAsync(parentWindow, "PIN", "웹브라우저 상에 표시된 PIN을 입력합니다.", settings);

                    try
                    {
                        authMgr.AcquireAccessToken(AccessTokenURL, "POST", pin);
                    }
                    catch(Exception ex)
                    {
                        parentWindow.Close();
                    }

                    IsInit = true;

                    // Get Screen Name
                    dynamic obj = await GetJson("https://api.twitter.com/1.1/account/settings.json", "GET");
                    string name = obj.screen_name;

                    // GetProfile
                    dynamic json = await GetJson($"https://api.twitter.com/1.1/users/show.json?screen_name={name}", "GET");
                    parentWindow.UserNameBlock.Text = json.name;
                    parentWindow.ScreenNameBlock.Text = $"@{json.screen_name}";
                    parentWindow.ProfileDescBlock.Text = json.description;
                    string imageUrl = json.profile_image_url;
                    ProfileImage = new BitmapImage();
                    ProfileImage.BeginInit();
                    ProfileImage.UriSource = new Uri(imageUrl.Replace("_normal", ""));
                    ProfileImage.DownloadCompleted +=
                        (s, ev) => OnPropertyChanged("ProfileImage");
                    ProfileImage.EndInit();
                }
                else
                {
                    parentWindow.Close();
                }

                return IsInit;
            }
            catch (Exception e)
            {
                await DialogManager.ShowMessageAsync(parentWindow, "Error", $"= Message{e.Message}\n\n= Stack Trace\n{e.StackTrace}");
                IsInit = false;
                return false;
            }
        }

        public async Task<object> GetJson(string url, string method)
        {
            try
            {
                var authHeader = authMgr.GenerateAuthzHeader(url, method);
                HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
                request.Method = method;
                request.PreAuthenticate = true;
                request.AllowWriteStreamBuffering = true;
                request.Headers.Add("Authorization", authHeader);

                var response = await request.GetResponseAsync() as HttpWebResponse;

                using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(response.CharacterSet)))
                {
                    string msg = await sr.ReadToEndAsync();

                    return JsonConvert.DeserializeObject(msg);
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }

    public class BooleanVisibilityReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                bool b = (bool)value;

                if (b)
                    return Visibility.Hidden;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                Visibility v = (Visibility)value;

                if (v == Visibility.Hidden)
                    return true;
            }

            return false;
        }
    }

    public class BooleanVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                bool b = (bool)value;

                if (b)
                    return Visibility.Visible;
            }
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                Visibility v = (Visibility)value;

                if (v == Visibility.Hidden)
                    return false;
            }

            return true;
        }
    }
}
