﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using HyPlayer.Classes;
using HyPlayer.Controls;
using HyPlayer.HyPlayControl;
using NeteaseCloudMusicApi;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace HyPlayer.Pages
{
    /// <summary>
    ///     可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class AlbumPage : Page, IDisposable
    {
        private readonly ObservableCollection<NCSong> songs = new ObservableCollection<NCSong>();
        private NCAlbum Album;
        private List<NCArtist> artists = new List<NCArtist>();
        private int page;

        public AlbumPage()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await Task.Run(() =>
            {
                Common.Invoke(async () =>
                {
                    JObject json;
                    string albumid = "";
                    if (e.Parameter is NCAlbum)
                    {
                        Album = (NCAlbum)e.Parameter;
                        albumid = Album.id;
                    }
                    else if (e.Parameter is string)
                    {
                        albumid = e.Parameter.ToString();
                    }

                    try
                    {
                        json = await Common.ncapi.RequestAsync(CloudMusicApiProviders.Album,
                            new Dictionary<string, object> { { "id", albumid } });
                        Album = NCAlbum.CreateFromJson(json["album"]);
                        ImageRect.ImageSource =
                            new BitmapImage(
                                new Uri(Album.cover + "?param=" + StaticSource.PICSIZE_SONGLIST_DETAIL_COVER));
                        TextBoxAlbumName.Text = Album.name;

                        TextBoxAlbumName.Text = json["album"]["name"].ToString();
                        artists = json["album"]["artists"].ToArray().Select(t => new NCArtist
                        {
                            avatar = t["picUrl"].ToString(),
                            id = t["id"].ToString(),
                            name = t["name"].ToString()
                        }).ToList();
                        TextBoxAuthor.Text = string.Join(" / ", artists.Select(t => t.name));
                        TextBlockDesc.Text = (json["album"]["alias"].HasValues
                                                 ? string.Join(" / ",
                                                       json["album"]["alias"].ToArray().Select(t => t.ToString())) +
                                                   "\r\n"
                                                 : "")
                                             + json["album"]["description"];
                        var cdname = "";
                        SongsList sl = null;
                        var idx = 0;
                        foreach (var song in json["songs"].ToArray())
                        {
                            var ncSong = NCSong.CreateFromJson(song);
                            songs.Add(ncSong);
                            if (song["cd"].ToString() != cdname)
                            {
                                idx = 0;
                                cdname = song["cd"].ToString();
                                sl = new SongsList { Songs = new ObservableCollection<NCSong>() ,ListSource="al"+ Album.id};
                                SongContainer.Children.Add(new StackPanel
                                {
                                    Orientation = Orientation.Vertical,
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            Margin = new Thickness(5, 0, 0, 0),
                                            FontWeight = FontWeights.Black, FontSize = 23, Text = "Disc " + cdname
                                        },
                                        sl
                                    }
                                });
                            }
                            ncSong.Order = idx++;
                            sl.Songs.Add(ncSong);
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.ShowTeachingTip("发生错误", ex.Message);
                    }
                });
            });
        }


        private void ButtonPlayAll_OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                Common.Invoke(async () =>
                {
                    try
                    {
                        await HyPlayList.AppendNCSongs(songs, HyPlayItemType.Netease);

                        HyPlayList.SongAppendDone();

                        HyPlayList.SongMoveTo(0);
                    }
                    catch (Exception ex)
                    {
                        Common.ShowTeachingTip("发生错误", ex.Message);
                    }
                });
            });
        }


        private void NextPage_OnClickPage_OnClick(object sender, RoutedEventArgs e)
        {
            page++;
        }

        private void ButtonDownloadAll_OnClick(object sender, RoutedEventArgs e)
        {
            DownloadManager.AddDownload(songs);
        }

        private void ButtonComment_OnClick(object sender, RoutedEventArgs e)
        {
            Common.NavigatePage(typeof(Comments), "al" + Album.id);
        }

        private async void TextBoxAuthor_OnTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            if (artists.Count > 1)
                await new ArtistSelectDialog(artists).ShowAsync();
            else
                Common.NavigatePage(typeof(ArtistPage), artists[0].id);
        }

        public void Dispose()
        {
            songs.Clear();
            Album = null;
            artists = null;
            ImageRect.ImageSource = null;
            SongContainer.Children.Clear();
            GC.SuppressFinalize(this);
        }
    }
}