using Newtonsoft.Json.Linq;
using System;
using System.Net;
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

        public string LastSongInfo { get; private set; }

        private CancellationTokenSource _cancellationTokenSource;

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
        }

        private async void WaitAndCheckNewSongInfo()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                await Task.Delay(2000);
                try
                {
                    WebClient client = new WebClient();
                    client.Headers.Add("User-Agent", "RadioPlayerApp");

                    string downloadedInfo = client.DownloadString(new Uri(SongInfoURL));

                    switch (SongInfoSourceType)
                    {
                        case SongInfoSourceType.Plain:
                            LastSongInfo = downloadedInfo;
                            break;
                        case SongInfoSourceType.JSON:
                            LastSongInfo = string.Empty;

                            JObject jObject = JObject.Parse(downloadedInfo);

                            for (int i = 0; i < SongInfoXPathes.Length; )  // It could be separated artist and title
                            {
                                string songInfoXPath = SongInfoXPathes[i];
                                string info = (string)jObject.SelectToken(songInfoXPath);
                                LastSongInfo += $"{info} ";

                                if (++i < SongInfoXPathes.Length)
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
                }
                catch (Exception ex)    //TODO: Rozdzielić wyjatki
                {
                    LastSongInfo = @"N\A"; 
                    NewError?.Invoke(this, new Exception($"Song info could not be downloaded or parsed. {SongInfoURL}", ex));
                }
                finally
                {
                    NewSong?.Invoke(this, new SongInfoEventArgs(LastSongInfo));
                }
            }
        }

        public override string ToString()
        {
            return $"{ Name}";
        }
    }
}