using App1.Common;
using App1.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Media;

// Die Elementvorlage für die Seite "Elementdetails" ist unter http://go.microsoft.com/fwlink/?LinkId=234232 dokumentiert.

namespace App1
{
    /// <summary>
    /// Eine Seite, auf der Details für ein einzelnes Element innerhalb einer Gruppe angezeigt werden.
    /// </summary>
    public sealed partial class ItemDetailPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        SystemMediaTransportControls systemMediaControls = null;
        MediaElement localMediaElement = null;
        List<String> localPlaylist = new List<String>();
        int localCurrentTrack = -1;
        // Used with the custom progress slider.
        private bool sliderPressed = false;


        /// <summary>
        /// NavigationHelper wird auf jeder Seite zur Unterstützung bei der Navigation verwendet und 
        /// Prozesslebensdauer-Verwaltung
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Dies kann in ein stark typisiertes Anzeigemodell geändert werden.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        public ItemDetailPage()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;
        }

        /// <summary>
        /// Füllt die Seite mit Inhalt auf, der bei der Navigation übergeben wird.  Gespeicherte Zustände werden ebenfalls
        /// bereitgestellt, wenn eine Seite aus einer vorherigen Sitzung neu erstellt wird.
        /// </summary>
        /// <param name="sender">
        /// Die Quelle des Ereignisses, normalerweise <see cref="Common.NavigationHelper"/>
        /// </param>
        /// <param name="e">Ereignisdaten, die die Navigationsparameter bereitstellen, die an
        /// <see cref="Frame.Navigate(Type, Object)"/> als diese Seite ursprünglich angefordert wurde und
        /// ein Wörterbuch des Zustands, der von dieser Seite während einer früheren
        /// beibehalten wurde.  Der Zustand ist beim ersten Aufrufen einer Seite NULL.</param>
        private async void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {

            DependencyObject rootGrid = VisualTreeHelper.GetChild(Window.Current.Content, 0);
            this.localMediaElement = (MediaElement)VisualTreeHelper.GetChild(rootGrid, 0);
            if (this.localMediaElement != null)
            {
                System.Diagnostics.Debug.WriteLine("media element found: " + this.localMediaElement.Name);
            }


            // TODO: Ein geeignetes Datenmodell für die domänenspezifische Anforderung erstellen, um die Beispieldaten auszutauschen
            var item = await SampleDataSource.GetItemAsync((String)e.NavigationParameter);
            this.DefaultViewModel["Item"] = item;
            SampleDataItem sdi = (SampleDataItem) item;
            System.Diagnostics.Debug.WriteLine("called ItemDetailsPage ... " + item.Title);
            System.Diagnostics.Debug.WriteLine("Number of tracks: " + sdi.Tracks.Count);

            if (sdi.Tracks.Count > 0)
            {

                // Get the logical root folder for all external storage devices.
                StorageFolder externalDevices = Windows.Storage.KnownFolders.RemovableDevices;
                System.Diagnostics.Debug.WriteLine("pass1");

                if (externalDevices == null)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: no external devices");
                    return;
                }


                // Get the first child folder, which represents the SD card.
                StorageFolder sdCard = (await externalDevices.GetFoldersAsync()).FirstOrDefault();
                //sdCard
                
                if (sdCard == null)
                {
                    System.Diagnostics.Debug.WriteLine("Warning: no medium found");
                    return;
                }
                System.Diagnostics.Debug.WriteLine("sdCard ... " + sdCard.Path);



                string pathPrefix = sdCard.Path + "jukebox\\";
                string fullPath = pathPrefix + sdi.Dirname.Substring(2) + '\\' + sdi.Tracks[0];
                System.Diagnostics.Debug.WriteLine("Loading ... " + fullPath);

                this.localPlaylist.Clear();
                this.localCurrentTrack = -1;
                foreach(String currentTrack in sdi.Tracks)
                {
                    String fPath = pathPrefix + sdi.Dirname.Substring(2) + '\\' + currentTrack;
                    System.Diagnostics.Debug.WriteLine("adding ... " + fPath);
                    this.localPlaylist.Add(fPath);
                }


                if (this.systemMediaControls == null)
                {
                    systemMediaControls = SystemMediaTransportControls.GetForCurrentView();
                    systemMediaControls.ButtonPressed += SystemMediaControls_ButtonPressed;
                    systemMediaControls.IsPlayEnabled = true;
                    systemMediaControls.IsPauseEnabled = true;
                    systemMediaControls.IsStopEnabled = true;
                    this.localMediaElement.CurrentStateChanged += GlobalMedia_CurrentStateChanged;
                    this.localMediaElement.MediaEnded += MediaElement_MediaEnded;
                    this.localMediaElement.MediaOpened += MediaElement_MediaOpened;

                }

                playNextTrack();
                /*

                StorageFile sFile = null;
                try
                {
                    sFile = await StorageFile.GetFileFromPathAsync(fullPath);
                }
                catch (Exception e2)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading ... " + fullPath);
                }

                // StorageFile sFile = await sdCard.getF
                if (sFile != null)
                {


                    System.Diagnostics.Debug.WriteLine("Successfull: " + fullPath);
                    IRandomAccessStream stream = await sFile.OpenAsync(FileAccessMode.Read);

                    this.localMediaElement.SetSource(stream, sFile.ContentType);
                    this.localMediaElement.Play();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed: " + fullPath);
                }
                */
            }

        }


        void GlobalMedia_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("called currentStateChanged");
            updateSystemMediaControlsStatus();
        }


        
        // currenly does not work
        void updateSystemMediaControlsStatus()
        {
            if (this.systemMediaControls != null && this.localMediaElement != null)
            {
                switch (this.localMediaElement.CurrentState)
                {
                    default:
                    case MediaElementState.Closed:
                        systemMediaControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                        break;

                    case MediaElementState.Opening:
                        systemMediaControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                        break;

                    case MediaElementState.Buffering:
                    case MediaElementState.Playing:
                        systemMediaControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                        break;

                    case MediaElementState.Paused:
                        systemMediaControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                        break;

                    case MediaElementState.Stopped:
                        systemMediaControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                        break;
                }
            }
            
        }

        void SystemMediaControls_ButtonPressed(SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    System.Diagnostics.Debug.WriteLine("play");
                    MediaElement_PlayMedia();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    System.Diagnostics.Debug.WriteLine("pause");
                    PauseMedia();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    System.Diagnostics.Debug.WriteLine("previous");
                    playPreviousTrack();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    System.Diagnostics.Debug.WriteLine("next");
                    playNextTrack();
                    break;
                default:
                    break;
            }
            updateSystemMediaControlsStatus();
        }

        async void MediaElement_PlayMedia()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.localMediaElement.Play();
           
            });
        }

        async void PauseMedia()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.localMediaElement.Pause();
            });
        }


        #region NavigationHelper-Registrierung

        /// Die in diesem Abschnitt bereitgestellten Methoden werden einfach verwendet,
        /// damit NavigationHelper auf die Navigationsmethoden der Seite reagieren kann.
        /// 
        /// Platzieren Sie seitenspezifische Logik in Ereignishandlern für  
        /// <see cref="Common.NavigationHelper.LoadState"/>
        /// sowie <see cref="Common.NavigationHelper.SaveState"/>.
        /// Der Navigationsparameter ist in der LoadState-Methode zusätzlich 
        /// zum Seitenzustand verfügbar, der während einer früheren Sitzung gesichert wurde.


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void BottomAppBar_Opened(object sender, object e)
        {

        }


        private void appBarButtonPlay_Click(object sender, RoutedEventArgs e)
        {
            if (this.localMediaElement != null)
            {
                if (this.localMediaElement.CurrentState == MediaElementState.Playing)
                {
                    this.localMediaElement.Pause();

                }
                else if (this.localMediaElement.CurrentState == MediaElementState.Paused)
                {
                    this.localMediaElement.Play();
                }
            }
        }

        private async void playNextTrack()
        {
            System.Diagnostics.Debug.WriteLine("called playNextTrack");
            if (this.localCurrentTrack < this.localPlaylist.Count-1)
            {
                this.localCurrentTrack++;

                StorageFile sFile = null;
                String fullPath = this.localPlaylist[this.localCurrentTrack];
                try
                {
                    System.Diagnostics.Debug.WriteLine("Loading ... " + fullPath);
                    sFile = await StorageFile.GetFileFromPathAsync(fullPath);
                    if (sFile != null)
                    {
                        timelineSlider.Value = 0;
                        IRandomAccessStream stream = await sFile.OpenAsync(FileAccessMode.Read);
                        this.localMediaElement.SetSource(stream, sFile.ContentType);
                        this.localMediaElement.Play();
                    }
                }
                catch (Exception e2)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading ... " + fullPath);
                }
            }
        }

        private async void playPreviousTrack()
        {
            System.Diagnostics.Debug.WriteLine("called playPreviousTrack");
            if (this.localCurrentTrack > 0)
            {
                this.localCurrentTrack--;

                StorageFile sFile = null;
                String fullPath = this.localPlaylist[this.localCurrentTrack];
                try
                {
                    System.Diagnostics.Debug.WriteLine("Loading ... " + fullPath);
                    sFile = await StorageFile.GetFileFromPathAsync(fullPath);
                    if (sFile != null)
                    {
                        timelineSlider.Value = 0;
                        IRandomAccessStream stream = await sFile.OpenAsync(FileAccessMode.Read);
                        this.localMediaElement.SetSource(stream, sFile.ContentType);
                        this.localMediaElement.Play();
                    }
                }
                catch (Exception e2)
                {
                    System.Diagnostics.Debug.WriteLine("Error loading ... " + fullPath);
                }
            }
        }

        /// <summary>
        /// Handler for the MediaEnded event of the MediaElement.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("media ended!");
            playNextTrack();
        }


        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            // Initiazlie custom timeline slider.
            System.Diagnostics.Debug.WriteLine("media length: " + this.localMediaElement.NaturalDuration.TimeSpan.TotalSeconds);

            timelineSlider.Maximum = this.localMediaElement.NaturalDuration.TimeSpan.TotalSeconds;
        }

        /// <summary>
        /// The timeline position Slider ValueChanged event handler. 
        /// When the slider value is changed, update the MediaElement.Position.
        /// </summary>
        void TimelineSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {

            if (!sliderPressed)
            {
                System.Diagnostics.Debug.WriteLine("slider value changed: " + e.NewValue);
                localMediaElement.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        /// <summary>
        /// The PointerEntered event handler for the timeline Slider.
        /// Sets the sliderpressed flag so the timer does not continue to update the Slider while the user
        /// is interacting with it.
        /// </summary>
        void TimelineSlider_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("slider entered");
            sliderPressed = true;
        }

        void TimelineSlider_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("slider released");
        }

        /// <summary>
        /// The PointerCaptureLost event handler for the timeline position Slider.
        /// Sets the MediaElement.Position to Slider.Value and unsets the sliderpressed flag 
        /// which enables the timer to continue to update the slider position.
        /// </summary>
        void TimelineSlider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("slider lost");
            sliderPressed = false;
        }

        private void AppBarButton_Previous(object sender, RoutedEventArgs e)
        {
            playPreviousTrack();
        }

        private void AppBarButton_Next(object sender, RoutedEventArgs e)
        {
            playNextTrack();
        }
    }
}