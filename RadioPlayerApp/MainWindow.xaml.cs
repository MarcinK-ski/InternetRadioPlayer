using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RadioPlayerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Brush _defaultErrorTabColor;
        public List<Radio> Radios { get; set; } = new List<Radio>();

        public ImageSource DefaultLogoSource { get; }
        public Radio CurrentRadio { get; set; }

        private bool _isRadioPaused;
        public bool IsRadioPaused
        {
            get
            {
                return _isRadioPaused;
            }
            private set
            {
                _isRadioPaused = value;
                if (_isRadioPaused)
                {
                    DisableOrEnableButtons(PlayerAction.Pause);
                }
                else
                {
                    DisableOrEnableButtons(PlayerAction.Play);
                }
            }
        }

        private bool _isRadioInitialized;
        public bool IsRadioInitialized 
        { 
            get
            {
                return _isRadioInitialized;
            }
            private set
            {
                _isRadioInitialized = value;
                if (_isRadioInitialized)
                {
                    DisableOrEnableButtons(PlayerAction.Play);
                    CurrentRadio.NewError += NewErrorInRadios;
                }
                else
                {
                    DisableOrEnableButtons(PlayerAction.Stop);
                    CurrentRadio.NewError -= NewErrorInRadios;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _defaultErrorTabColor = errorsTab.Background;
            mediaPlayer.LoadedBehavior = MediaState.Manual;
            volumeSlider.Value = mediaPlayer.Volume;

            DefaultLogoSource = logo.Source;

            radioSelector.ItemsSource = AppConfig.radiosConfig;
            radioSelector.SelectedIndex = 0;

            EnableTimeUpdating();
        }

        public async void EnableTimeUpdating()
        {
            while (true)
            {
                await Task.Delay(500);
                time.Content = mediaPlayer.Position.ToString(@"hh\:mm\:ss");
            }
        }

        private void DisableOrEnableButtons(PlayerAction playerAction)
        {
            switch (playerAction)
            {
                case PlayerAction.Play:
                    radioSelector.IsEnabled = false;
                    playButton.IsEnabled = false;
                    pauseButton.IsEnabled = true;
                    stopButton.IsEnabled = true;
                    break;
                case PlayerAction.Pause:
                    radioSelector.IsEnabled = false;
                    playButton.IsEnabled = true;
                    pauseButton.IsEnabled = false;
                    stopButton.IsEnabled = true;
                    break;
                case PlayerAction.Stop:
                    radioSelector.IsEnabled = true;
                    playButton.IsEnabled = true;
                    pauseButton.IsEnabled = false;
                    stopButton.IsEnabled = false;
                    break;
            }
        }

        private void SetNewLogoImage(ImageSource imageSource)
        {
            logo.Source = imageSource;
            imageNotLoadedLabel.Visibility = Visibility.Hidden;
        }

        private void AddNewError(Exception ex, string details = "")
        {
            errorsTextBox.Text += $"\n{details}\n{ex}";
            errorsTab.Background = Brushes.Red;
        }

        #region RadioHandlingEventsMethods
        private void NewErrorInRadios(object sender, Exception exception)
        {
            AddNewError(exception, "In RADIOS");
        }

        private void CurrentSongBasedOnIncomingEvent(object sender, SongInfoEventArgs e)
        {
            song.Content = e.SongInfo;
        }
        #endregion

        #region ButtonsClickEvents
        private void PlayButtonClick(object sender, RoutedEventArgs e)
        {
            if (!IsRadioInitialized)
            {
                CurrentRadio = radioSelector.SelectedItem as Radio;

                if (CurrentRadio != null)
                {
                    mediaPlayer.Source = new Uri(CurrentRadio.SourceURL);

                    ImageSource currentRadioLogo = new BitmapImage(new Uri(CurrentRadio.LogoURL));
                    SetNewLogoImage(currentRadioLogo);

                    CurrentRadio.StartListeningToNewSongInfo();
                    CurrentRadio.NewSong += CurrentSongBasedOnIncomingEvent;


                    IsRadioInitialized = true;
                    IsRadioPaused = true;
                }
            }

            if (IsRadioPaused)
            {
                mediaPlayer.Play();
                IsRadioPaused = false;
            }
        }

        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            if (IsRadioInitialized)
            {
                mediaPlayer.Stop();
                mediaPlayer.Close();
                mediaPlayer.ClearValue(MediaElement.SourceProperty);
                CurrentRadio.StopListeningToNewSongInfo();
                CurrentRadio.NewSong -= CurrentSongBasedOnIncomingEvent;
                song.Content = "- \\ -";
                SetNewLogoImage(DefaultLogoSource);

                IsRadioInitialized = false;
            }
        }

        private void PauseButtonClick(object sender, RoutedEventArgs e)
        {
            if (IsRadioInitialized && !IsRadioPaused)
            {
                mediaPlayer.Pause();
                IsRadioPaused = true;
            }
        }
        #endregion

        #region UIUserManipulationEvents
        private void VolumeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = volumeSlider.Value;
        }

        private void SelectedTabChanged(object sender, SelectionChangedEventArgs e)
        {
            if (errorsTab.IsSelected)
            {
                errorsTab.Background = _defaultErrorTabColor;
            }
        }
        #endregion

        #region MediaLoadingFailsEvents
        private void LogoImageLoadingFailed(object sender, ExceptionRoutedEventArgs e)
        {
            AddNewError(e.ErrorException, "Image loading failed");
            imageNotLoadedLabel.Visibility = Visibility.Visible;
        }

        private void AudioLoadingFailed(object sender, ExceptionRoutedEventArgs e)
        {
            AddNewError(e.ErrorException, "On PLAY");
            if (IsRadioInitialized)
            {
                IsRadioInitialized = false;
            }
        }
        #endregion
    }
}
