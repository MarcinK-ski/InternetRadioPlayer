using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RadioPlayerApp
{
    public class Radio
    {
        public delegate void SongInfoEventHandler(object sender, SongInfoEventArgs e);
        public event SongInfoEventHandler NewSong;

        public delegate void ErrorEventHandler(object sender, Exception exception);
        public event ErrorEventHandler NewError;

        public string Name { get; set; }

        public string SourceURL { get; set; }

        public string LogoURL { get; set; }

        public string SongInfoURL { get; set; }

        public SongInfoSourceType SongInfoSourceType { get; set; }

        public string[] SongInfoXPathes { get; set; }

        private string _lastSongInfo;
        public string LastSongInfo 
        {
            get
            {
                return _lastSongInfo;
            }
            private set
            {
                if (_lastSongInfo == value)
                {
                    IsSongChanged = false;
                }
                else
                {
                    _lastSongInfo = value;
                    IsSongChanged = true;
                }
            }
        }

        public bool IsSongChanged { get; private set; }

        private Exception _lastException;

        private CancellationTokenSource _cancellationTokenSource;

        private HttpClient _client;

        public Radio()
        {
        }

        public void StartListeningToNewSongInfo()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            WaitAndCheckNewSongInfo();
        }

        public void StopListeningToNewSongInfo()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            LastSongInfo = string.Empty;
            _lastException = null;
        }

        private async void WaitAndCheckNewSongInfo()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (_client == null)
                    {
                        _client = new HttpClient();
                        _client.DefaultRequestHeaders.Add("User-Agent", "RadioPlayerApp");
                    }

                    Uri songURI = new Uri(SongInfoURL);
                    string downloadedInfo = await _client.GetStringAsync(songURI);

                    switch (SongInfoSourceType)
                    {
                        case SongInfoSourceType.Plain:
                            LastSongInfo = downloadedInfo;
                            break;
                        case SongInfoSourceType.JSON:
                            LastSongInfo = string.Empty;

                            JObject jObject = JObject.Parse(downloadedInfo);

                            for (int i = 0; i < SongInfoXPathes.Length;)  //  Artist and title could be separated in JSON
                            {
                                string songInfoXPath = SongInfoXPathes[i];
                                string info = (string)jObject.SelectToken(songInfoXPath);
                                LastSongInfo += $"{info} ";

                                if (++i < SongInfoXPathes.Length && !string.IsNullOrWhiteSpace(info))
                                {
                                    LastSongInfo += "- ";
                                }
                            }
                            break;
                        case SongInfoSourceType.XML:
                            throw new NotImplementedException("XML is not implemented yet");
                            break;
                        default:
                            throw new NotImplementedException($"{SongInfoSourceType} is not implemented yet");
                            break;
                    }

                    _lastException = null;
                }
                catch (Exception ex)    //TODO: Rozdzielić wyjatki
                {
                    LastSongInfo = @"N\A"; 

                    if (!SameAsLastException(ex))
                    {
                        _lastException = ex;
                        NewError?.Invoke(this, new Exception($"Song info could not be downloaded or parsed. {SongInfoURL}", ex));
                    }
                }
                finally
                {
                    if (IsSongChanged)
                    {
                        NewSong?.Invoke(this, new SongInfoEventArgs(LastSongInfo));
                    }

                    await Task.Delay(2000);
                }
            }
        }

        private bool SameAsLastException(Exception ex)
        {
            return ex.GetType() == _lastException?.GetType()
                && ex.Message == _lastException?.Message;
        }

        public override string ToString()
        {
            return $"{ Name}";
        }
    }
}