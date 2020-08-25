using System;

namespace RadioPlayerApp
{
    public class SongInfoEventArgs : EventArgs
    {
        public string SongInfo { get; private set; }

        public SongInfoEventArgs(string song)
        {
            SongInfo = song;
        }
    }
}