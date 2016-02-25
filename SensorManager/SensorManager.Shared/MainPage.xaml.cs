using Microsoft.Band;
using Microsoft.Band.Tiles;
using Microsoft.Band.Tiles.Pages;
using SensorManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace SensorManager
{
    public sealed partial class MainPage : Page
    {
        // define the element Ids for our tile’s page 
        private enum TilePageElementId : short
        {
            Jukebox_PlayRandom = 1,
            Jukebox_Pause,
            Jukebox_Stop
        }

        private const string SERVICE_URL = "";

        private IBandClient bandClient;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                var pairedBands = await BandClientManager.Instance.GetBandsAsync();
                bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]);

                await this.CreateTilesAsync();
            }
            catch { }

            base.OnNavigatedTo(e);
        }

        private async Task CreateJukeboxTileAsync()
        {
            // create the small and tile icons from writable bitmaps.
            // Small icons are 24x24 pixels.
            var writeableSmallBmp = BitmapFactory.New(24, 24);
            using (writeableSmallBmp.GetBitmapContext())
            {
                // Load an image from the calling Assembly's resources via the relative path
                writeableSmallBmp = await BitmapFactory.New(1, 1).FromContent(new Uri("ms-appx:///Assets/Sensor24x24.png"));
            }
            var smallIcon = writeableSmallBmp.ToBandIcon();

            // Tile icons are 46x46 pixels for Microsoft Band 1, and 48x48 pixels for Microsoft 
            // Band 2.
            var writeableBmp = BitmapFactory.New(46, 46);
            using (writeableBmp.GetBitmapContext())
            {
                // Load an image from the calling Assembly's resources via the relative path
                writeableBmp = await BitmapFactory.New(1, 1).FromContent(new Uri("ms-appx:///Assets/Sensor46x46.png"));
            }
            var tileIcon = writeableBmp.ToBandIcon();

            // create a new Guid for the tile
            var tileGuid = Guid.NewGuid();

            // create a new tile with a new Guid
            var tile = new BandTile(tileGuid)
            {
                IsBadgingEnabled = true,
                Name = "Sensor Manager",
                SmallIcon = smallIcon,
                TileIcon = tileIcon
            };

            // create a filled rectangle to provide the background for a button 
            var panel = new FilledPanel
            {
                Rect = new PageRect(0, 0, 245, 102)
            };

            // add buttons to our layout 
            panel.Elements.Add(new TextButton
            {
                ElementId = (short)TilePageElementId.Jukebox_PlayRandom,
                Rect = new PageRect(20, 0, 70, 102),
                PressedColor = Colors.Blue.ToBandColor()
            });

            panel.Elements.Add(new TextButton
            {
                ElementId = (short)TilePageElementId.Jukebox_Pause,
                Rect = new PageRect(120, 0, 100, 45),
                PressedColor = Colors.Blue.ToBandColor()
            });

            panel.Elements.Add(new TextButton
            {
                ElementId = (short)TilePageElementId.Jukebox_Stop,
                Rect = new PageRect(120, 55, 100, 45),
                PressedColor = Colors.Blue.ToBandColor()
            });

            // create the page layout 
            var layout = new PageLayout(panel);

            // add the tile to the Band
            tile.PageLayouts.Add(layout);
            if (await bandClient.TileManager.AddTileAsync(tile))
            {
                // create the content to assign to the page 
                var pageContent = new PageData(Guid.NewGuid(),
                    0, // index of our (only) layout                 
                    new TextButtonData((short)TilePageElementId.Jukebox_PlayRandom, "Play"),
                    new TextButtonData((short)TilePageElementId.Jukebox_Pause, "Pause"),
                    new TextButtonData((short)TilePageElementId.Jukebox_Stop, "Stop"));

                await bandClient.TileManager.SetPagesAsync(tileGuid, pageContent);
            }
        }

        private async Task CreateTilesAsync()
        {
            var tiles = await bandClient.TileManager.GetTilesAsync();
            foreach (var t in tiles)
                await bandClient.TileManager.RemoveTileAsync(t);

            await this.CreateJukeboxTileAsync();

            // Subscribe to events
            bandClient.TileManager.TileButtonPressed += TileManager_TileButtonPressed;

            // Start listening for events 
            await bandClient.TileManager.StartReadingsAsync();
        }

        private async void TileManager_TileButtonPressed(object sender, BandTileEventArgs<IBandTileButtonPressedEvent> e)
        {
            switch ((TilePageElementId)e.TileEvent.ElementId)
            {
                case TilePageElementId.Jukebox_PlayRandom:
                    await this.PlayRandomMusicAsync();
                    break;

                case TilePageElementId.Jukebox_Pause:
                    await this.PauseMusicAsync();
                    break;

                case TilePageElementId.Jukebox_Stop:
                    await this.StopMusicAsync();
                    break;
            }
        }

        #region Jukebox

        private async void playMusicButton_Click(object sender, RoutedEventArgs e)
            => await this.PlayRandomMusicAsync();

        private async void pauseMusicButton_Click(object sender, RoutedEventArgs e)
            => await this.PauseMusicAsync();

        private async void stopMusicButton_Click(object sender, RoutedEventArgs e)
            => await this.StopMusicAsync();

        private Task PlayRandomMusicAsync() => this.SendJukeboxCommandAsync("random");

        private Task PauseMusicAsync() => this.SendJukeboxCommandAsync("pause");

        private Task StopMusicAsync() => this.SendJukeboxCommandAsync("stop");

        private async Task SendJukeboxCommandAsync(string command)
        {
            using (var client = new RestClient(SERVICE_URL))
            {
                try
                {
                    await client.PostAsync($"jukebox/{command}");
                }
                catch (Exception ex)
                {
                    var dialog = new MessageDialog($"Error sending commnad: {ex.Message}");
                    await dialog.ShowAsync();
                }
            }
        }

        #endregion
    }
}
