using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Newtonsoft;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Windows.Media.Imaging;

namespace TwitterApp
{
    public class Tweet : INotifyPropertyChanged
    {
        public static Dictionary<string, BitmapImage> ProfileImageDict
        {
            get;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public dynamic JSonObject { get; }
        public string TweetContent
        {
            get { return JSonObject.text; }
        }

        public string UserName
        {
            get { return JSonObject.user.name; }
        }

        public string ScreenName
        {
            get { return $"@{JSonObject.user.screen_name}"; }
        }

        public string ID
        {
            get { return JSonObject.id_str; }
        }

        public string Time
        {
            get
            {
                string datestr = JSonObject.created_at;
                DateTime dt = TweetDateConvert(datestr, CultureInfo.InvariantCulture);

                return dt.ToString("yyyy MMM월 d일 ddd요일 HH:mm:ss");
            }
        }

        public bool IsRetweeted
        {
            get
            {
                return Convert.ToBoolean(JSonObject.retweeted);
            }
        }

        public Tweet RetweetStatus
        {
            get
            {
                return new Tweet(JSonObject.retweeted_status);
            }
        }

        public int RetweetCount
        {
            get { return JSonObject.retweet_count; }
        }

        public int FavoriteCount
        {
            get { return JSonObject.favorite_count; }
        }

        public string InfoString
        {
            get { return $"ID : {ID}, RT : {RetweetCount}, Fav : {FavoriteCount}"; }
        }

        public BitmapImage ProfileImage
        {
            get
            {
                string profileUrl = JSonObject.user.profile_image_url;
                profileUrl = profileUrl.Replace("_normal", "");

                if (!ProfileImageDict.ContainsKey(profileUrl))
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(profileUrl);
                    image.DownloadCompleted += (s, e) =>
                    {
                        OnPropertyChanged("ProfileImage");
                        if (!Tweet.ProfileImageDict.ContainsKey(profileUrl))
                        {
                            Tweet.ProfileImageDict.Add(profileUrl, image);
                        }
                    };
                    image.EndInit();

                    ProfileImageDict.Add(profileUrl, image);
                }

                return ProfileImageDict[profileUrl];
            }
        }

        public Tweet(dynamic json)
        {
            JSonObject = json;

            if (json.retweeted_status != null)
            {
                JSonObject = json.retweeted_status;
            }
        }

        static Tweet()
        {
            ProfileImageDict = new Dictionary<string, BitmapImage>();
        }

        public static string TweetDateConvert(string tweetDate)
        {
            Regex reTweetDate =
                new Regex(@"([a-zA-Z]{3})\s([a-zA-Z]{3})\s([0-9]{2})\s([0-9]{2}:[0-9]{2}:[0-9]{2})\s([+-])\s?([0-9]{4})\s([0-9]{4})");
            GroupCollection g = reTweetDate.Match(tweetDate).Groups;

            return $"{g[1].Value}, {g[3].Value} {g[2].Value} {g[7].Value} {g[4].Value} {g[5].Value + g[6].Value}";
        }

        public static DateTime TweetDateConvert(string tweetDate, IFormatProvider ci)
        {
            return DateTime.Parse(TweetDateConvert(tweetDate), ci);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class TweetCollection : ObservableCollection<Tweet> { }

    public class RetweetStatus : INotifyPropertyChanged
    {
        private dynamic JSonObject { get; }

        public string Time
        {
            get
            {
                string datestr = JSonObject.created_at;
                DateTime dt = Tweet.TweetDateConvert(datestr, CultureInfo.InvariantCulture);

                return dt.ToString("yyyy MMM월 d일 ddd요일 HH:mm:ss");
            }
        }
        public string UserName
        {
            get { return JSonObject.user.name; }
        }

        public string ScreenName
        {
            get { return $"@{JSonObject.user.screen_name}"; }
        }

        public string UserID
        {
            get { return JSonObject.user.id_str; }
        }

        public RetweetStatus(dynamic obj)
        {
            JSonObject = obj;
        }

        public string ProfileDescription
        {
            get { return JSonObject.user.description; }
        }

        public BitmapImage ProfileImage
        {
            get
            {
                string profileUrl = JSonObject.user.profile_image_url;
                profileUrl = profileUrl.Replace("_normal", "");

                if (!Tweet.ProfileImageDict.ContainsKey(profileUrl))
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(profileUrl);
                    image.DownloadCompleted += (s, e) =>
                    {
                        OnPropertyChanged("ProfileImage");
                        if (!Tweet.ProfileImageDict.ContainsKey(profileUrl))
                        {
                            Tweet.ProfileImageDict.Add(profileUrl, image);
                        }
                    };
                    image.EndInit();


                    Tweet.ProfileImageDict.Add(profileUrl, image);
                }

                return Tweet.ProfileImageDict[profileUrl];
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public override string ToString()
        {
            return $"UserName : {UserName}\nUser ID : {UserID}\nScreenName : {ScreenName}\nDate : {Time}";
        }
    }

    public class RetweetStatusCollection : ObservableCollection<RetweetStatus> { }
}
