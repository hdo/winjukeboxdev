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


                //string pathPrefix = "D:\\private\\kinder\\";
                string pathPrefix = sdCard.Path + "jukebox\\";
                //string fullPath = pathPrefix + sdi.Tracks[0];
                string fullPath = pathPrefix + sdi.Dirname.Substring(2) + '\\' + sdi.Tracks[0];
                System.Diagnostics.Debug.WriteLine("Loading ... " + fullPath);

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

                    if (this.systemMediaControls == null)
                    {
                        systemMediaControls = SystemMediaTransportControls.GetForCurrentView();
                        systemMediaControls.ButtonPressed += SystemMediaControls_ButtonPressed;
                        systemMediaControls.IsPlayEnabled = true;
                        systemMediaControls.IsPauseEnabled = true;
                        systemMediaControls.IsStopEnabled = true;
                        //audioMediaElement.CurrentStateChanged += MediaElement_CurrentStateChanged;
                        
                    }



                    System.Diagnostics.Debug.WriteLine("Successfull: " + fullPath);
                    IRandomAccessStream stream = await sFile.OpenAsync(FileAccessMode.Read);

                    // not working
                    // see https://social.msdn.microsoft.com/Forums/en-US/f5b9cdba-5521-467d-b838-8420afc68e7f/media-events-not-firing-if-instantiated-from-function?forum=winappswithnativecode
                    // also https://zoomicon.wordpress.com/2014/11/18/gotcha-mediaelement-must-be-in-visual-tree-for-mediaopened-mediaended-to-be-fired/
                    App.GlobalMediaElement.CurrentStateChanged += GlobalMedia_CurrentStateChanged; // not working


                    App.GlobalMediaElement.SetSource(stream, sFile.ContentType);
                    App.GlobalMediaElement.Play();
                    //this.audioMediaElement.SetSource(stream, sFile.ContentType);
                    //this.audioMediaElement.Play();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed: " + fullPath);
                }
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
            if (this.systemMediaControls != null)
            {
                //switch (audioMediaElement.CurrentState)
                switch (App.GlobalMediaElement.CurrentState)
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
                default:
                    break;
            }
            //updateSystemMediaControlsStatus();
        }

        async void MediaElement_PlayMedia()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {

                //audioMediaElement.Play();
                App.GlobalMediaElement.Play();
                //this.updateSystemMediaControlsStatus();
                this.systemMediaControls.PlaybackStatus = MediaPlaybackStatus.Playing;
           
            });
        }

        async void PauseMedia()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //audioMediaElement.Pause();
                App.GlobalMediaElement.Pause();
                //this.updateSystemMediaControlsStatus();
                this.systemMediaControls.PlaybackStatus = MediaPlaybackStatus.Paused;
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
            if (App.GlobalMediaElement.CurrentState == MediaElementState.Playing)
            {
                App.GlobalMediaElement.Pause();

            } 
            else if (App.GlobalMediaElement.CurrentState == MediaElementState.Paused)
            {
                App.GlobalMediaElement.Play();

            }
        }
    }
}