﻿using BattleNet;
using BattleNet.D3;
using BattleNet.D3.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.ApplicationSettings;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Split Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234234

namespace HeroHelper
{
    /// <summary>
    /// A page that displays a group title, a list of items within the group, and details for
    /// the currently selected item.
    /// </summary>
    public sealed partial class MainPage : HeroHelper.Common.LayoutAwarePage
    {
        private const String RecentProfiles = "recentProfiles";
        private int _recentProfileCap = 10;

        private D3Client _d3Client;
        private ObservableCollection<Profile> _recentProfiles;
        
        public MainPage()
        {
            this.InitializeComponent();

            itemListView.AddHandler(Control.DropEvent, new DragEventHandler(itemListView_Drop), true);

            SettingsPane.GetForCurrentView().CommandsRequested += SettingsCommandsRequested;
        }

        private void SettingsCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            var privacyStatement = new SettingsCommand("privacy", "Privacy Statement", x => Windows.System.Launcher.LaunchUriAsync(
                    new Uri("http://craigmart.in/hero-helper/privacy-policy/")));

            args.Request.ApplicationCommands.Clear();
            args.Request.ApplicationCommands.Add(privacyStatement);
        }

        #region Page state management

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected async override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // TODO: Assign a bindable group to this.DefaultViewModel["Group"]
            // TODO: Assign a collection of bindable items to this.DefaultViewModel["Items"]

            PopulateRegionListBox();
            _recentProfiles = new ObservableCollection<Profile>();

            try
            {
                StorageFile sampleFile =
                            await ApplicationData.Current.LocalFolder.GetFileAsync(RecentProfiles + ".txt");

                string recentProfiles = await FileIO.ReadTextAsync(sampleFile);

                if (!String.IsNullOrEmpty(recentProfiles))
                {
                    _recentProfiles = JsonConvert.DeserializeObject<ObservableCollection<Profile>>(recentProfiles);
                }
            }
            catch (FileNotFoundException) { }

            this.DefaultViewModel["Items"] = _recentProfiles;
            
            if (pageState == null)
            {
                // When this is a new page, select the first item automatically unless logical page
                // navigation is being used (see the logical page navigation #region below.)
                if (!this.UsingLogicalPageNavigation() && this.itemsViewSource.View != null)
                {
                    this.itemsViewSource.View.MoveCurrentToFirst();
                }
            }
            else
            {
                // Restore the previously saved state associated with this page
                if (pageState.ContainsKey("SelectedProfile"))
                {
                    if (_recentProfiles.Count > 0)
                        itemListView.SelectedIndex = (int) pageState["SelectedProfile"];
                }
            }
        }

        private void PopulateRegionListBox()
        {
            // Populate the region list box from the Enums.
            regionComboBox.ItemsSource = Enum.GetNames(typeof(BattleNet.Region));

            // Default the selection to US.
            regionComboBox.SelectedItem = regionComboBox.Items[0];
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            if (this.itemsViewSource.View != null)
            {
                var selectedItem = this.itemsViewSource.View.CurrentItem;
                // TODO: Derive a serializable navigation parameter and assign it to
                pageState["SelectedProfile"] = this.itemsViewSource.View.CurrentPosition;
            }
        }

        #endregion

        #region Logical page navigation

        // Visual state management typically reflects the four application view states directly
        // (full screen landscape and portrait plus snapped and filled views.)  The split page is
        // designed so that the snapped and portrait view states each have two distinct sub-states:
        // either the item list or the details are displayed, but not both at the same time.
        //
        // This is all implemented with a single physical page that can represent two logical
        // pages.  The code below achieves this goal without making the user aware of the
        // distinction.

        /// <summary>
        /// Invoked to determine whether the page should act as one logical page or two.
        /// </summary>
        /// <param name="viewState">The view state for which the question is being posed, or null
        /// for the current view state.  This parameter is optional with null as the default
        /// value.</param>
        /// <returns>True when the view state in question is portrait or snapped, false
        /// otherwise.</returns>
        private bool UsingLogicalPageNavigation(ApplicationViewState? viewState = null)
        {
            if (viewState == null) viewState = ApplicationView.Value;
            return viewState == ApplicationViewState.FullScreenPortrait ||
                viewState == ApplicationViewState.Snapped;
        }

        /// <summary>
        /// Invoked when an item within the list is selected.
        /// </summary>
        /// <param name="sender">The GridView (or ListView when the application is Snapped)
        /// displaying the selected item.</param>
        /// <param name="e">Event data that describes how the selection was changed.</param>
        void ItemListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Invalidate the view state when logical page navigation is in effect, as a change
            // in selection may cause a corresponding change in the current logical page.  When
            // an item is selected this has the effect of changing from displaying the item list
            // to showing the selected item's details.  When the selection is cleared this has the
            // opposite effect.
            if (this.UsingLogicalPageNavigation()) this.InvalidateVisualState();

            Selector list = sender as Selector;
            Profile selectedItem = list.SelectedItem as Profile;
            if (selectedItem != null)
            {
                heroesGridView.ItemsSource = new ObservableCollection<ProfileHero>(selectedItem.Heroes);
            }
        }

        /// <summary>
        /// Invoked when the page's back button is pressed.
        /// </summary>
        /// <param name="sender">The back button instance.</param>
        /// <param name="e">Event data that describes how the back button was clicked.</param>
        protected override void GoBack(object sender, RoutedEventArgs e)
        {
            if (this.UsingLogicalPageNavigation() && itemListView.SelectedItem != null)
            {
                // When logical page navigation is in effect and there's a selected item that
                // item's details are currently displayed.  Clearing the selection will return to
                // the item list.  From the user's point of view this is a logical backward
                // navigation.
                this.itemListView.SelectedItem = null;
            }
            else
            {
                // When logical page navigation is not in effect, or when there is no selected
                // item, use the default back button behavior.
                base.GoBack(sender, e);
            }
        }

        /// <summary>
        /// Invoked to determine the name of the visual state that corresponds to an application
        /// view state.
        /// </summary>
        /// <param name="viewState">The view state for which the question is being posed.</param>
        /// <returns>The name of the desired visual state.  This is the same as the name of the
        /// view state except when there is a selected item in portrait and snapped views where
        /// this additional logical page is represented by adding a suffix of _Detail.</returns>
        protected override string DetermineVisualState(ApplicationViewState viewState)
        {
            // Update the back button's enabled state when the view state changes
            var logicalPageBack = this.UsingLogicalPageNavigation(viewState) && this.itemListView.SelectedItem != null;
            var physicalPageBack = this.Frame != null && this.Frame.CanGoBack;
            this.DefaultViewModel["CanGoBack"] = logicalPageBack || physicalPageBack;

            // Determine visual states for landscape layouts based not on the view state, but
            // on the width of the window.  This page has one layout that is appropriate for
            // 1366 virtual pixels or wider, and another for narrower displays or when a snapped
            // application reduces the horizontal space available to less than 1366.
            if (viewState == ApplicationViewState.Filled ||
                viewState == ApplicationViewState.FullScreenLandscape)
            {
                var windowWidth = Window.Current.Bounds.Width;
                if (windowWidth >= 1366) return "FullScreenLandscapeOrWide";
                return "FilledOrNarrow";
            }

            // When in portrait or snapped start with the default visual state name, then add a
            // suffix when viewing details instead of the list
            var defaultStateName = base.DetermineVisualState(viewState);
            return logicalPageBack ? defaultStateName + "_Detail" : defaultStateName;
        }

        #endregion

        private void viewHeroesButton_Click(object sender, RoutedEventArgs e)
        {
            ViewHeroes();
        }

        private void battleTagTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ViewHeroes();
            }
        }

        private void ViewHeroes()
        {
            string battleTag = battleTagTextBox.Text;
            battleTagTextBox.Text = "";
            Region region = (Region)Enum.Parse(typeof(Region), regionComboBox.SelectedItem.ToString(), true);

            if (!BattleNet.BattleNet.IsValidBattleTag(battleTag))
            {
                invalidBattleTagTextBlock.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                invalidBattleTagTextBlock.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                
                RefreshProfile(battleTag, region);
            }
        }

        private async void RefreshProfile(string battleTag, Region region)
        {
            _d3Client = new D3Client(region);

            battleTag = battleTag.Replace("#", "-");

            Profile profile = await _d3Client.GetProfileAsync(battleTag);

            if (profile == null || profile.Heroes == null)
            {
                var messageDialog =
                    new Windows.UI.Popups.MessageDialog(
                        String.Format("The profile: {0} on region {1} is not valid.", new object[] { battleTag.Replace("-","#"), region }),
                        "Profile doesn't exist");
                await messageDialog.ShowAsync();
            }
            else
            {
                profile.Region = region;

                // Set the Profile iamge.
                string imageName = String.Empty;
                int x;
                int y;
                int backx = 524;
                int backy = 0;
                foreach(ProfileHero profileHero in profile.Heroes)
                {
                    GetXYFromClassGender(profileHero.Class, profileHero.Gender, out x, out y);

                    if (profileHero.Hardcore)
                        backy = 205;
                    else
                        backy = 0;

                    if (profileHero.Id == profile.LastHeroPlayed)
                    {
                        profile.ProfilePortrait = new Portrait();
                        profile.ProfilePortrait.ViewRect = String.Format("{0},{1},168,130", new object[] {x, y});
                        profile.ProfilePortrait.Margin = String.Format("{0},{1},0,0", new object[] { x*-1, y*-1 });
                        profile.ProfilePortrait.BackgroundViewRect = String.Format("{0},{1},193,205", new object[] { backx, backy });
                        profile.ProfilePortrait.BackgroundMargin = String.Format("{0},{1},0,0", new object[] { backx * -1, backy * -1 });
                    }

                    profileHero.Portrait = new Portrait();
                    profileHero.Portrait.ViewRect = String.Format("{0},{1},168,130", new object[] { x, y });
                    profileHero.Portrait.Margin = String.Format("{0},{1},0,0", new object[] { x * -1, y * -1 });
                    profileHero.Portrait.BackgroundViewRect = String.Format("{0},{1},193,205", new object[] { backx, backy });
                    profileHero.Portrait.BackgroundMargin = String.Format("{0},{1},0,0", new object[] { backx * -1, backy * -1 });
                }

                profile.HeroHelperLastUpdated = DateTime.Now;
                
                // If the profile already exists in the list, remove it and add the new one.
                int index = 0;
                bool found = false;
                for (int i = 0; i < _recentProfiles.Count; i++)
                {
                    if (_recentProfiles[i].BattleTag.Equals(profile.BattleTag, StringComparison.CurrentCultureIgnoreCase))
                    {
                        found = true;
                        index = i;
                        break;
                    }
                }

                if (found)
                    _recentProfiles.RemoveAt(index);

                _recentProfiles.Insert(0, profile);

                if (_recentProfiles.Count > _recentProfileCap)
                    _recentProfiles.RemoveAt(_recentProfileCap);

                SaveRecentProfiles();

                // Just in case heroes are added or removed since last refresh.
                DeleteProfileHeroesFile(battleTag);

                // Select the first item in the list.
                itemListView.SelectedIndex = 0;
            }
        }

        private void GetXYFromClassGender(string charClass, Gender gender, out int x, out int y)
        {
            x = 168 * (int) gender;

            switch (charClass)
            {
                case "demon-hunter":
                    y = 130;
                    break;
                case "monk":
                    y = 260;
                    break;
                case "witch-doctor":
                    y = 390;
                    break;
                case "wizard":
                    y = 520;
                    break;
                case "barbarian":
                default:
                    y = 0;
                    break;

            }
        }

        private async void SaveRecentProfiles()
        {
            // Save the list of recent profiles for cache.
            string recentProfiles = JsonConvert.SerializeObject(_recentProfiles);

            StorageFile sampleFile =
                await ApplicationData.Current.LocalFolder.CreateFileAsync(RecentProfiles + ".txt",
                CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(sampleFile, recentProfiles);
        }

        private async void DeleteProfileHeroesFile(string battletag)
        {
            try
            {
                StorageFile sampleFile =
                    await ApplicationData.Current.LocalFolder.GetFileAsync(battletag.Replace("#", "-") + ".txt");
                await sampleFile.DeleteAsync();
            }
            catch (FileNotFoundException) { }
        }

        private void heroesGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Profile profile = this.itemListView.SelectedItem as Profile;
            int heroIndex = this.heroesGridView.SelectedIndex;
            ProfileHero profileHero = (ProfileHero)e.ClickedItem;

            for (int i = 0; i < profile.Heroes.Count; i++)
            {
                if (profile.Heroes[i].Id == profileHero.Id)
                    heroIndex = i;
            }

            Selector list = sender as Selector;

            SelectedHero selectedHero = new SelectedHero() { HeroIndex = heroIndex, Profile = profile };

            this.Frame.Navigate(typeof(HeroSplitPage), JsonConvert.SerializeObject(selectedHero));
        }

        private void itemListView_Drop(object sender, DragEventArgs e)
        {
            SaveRecentProfiles();
        }

        private async void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            Profile selectedProfile = this.itemListView.SelectedItem as Profile;

            if (selectedProfile != null && this.Frame != null)
            {
                var messageDialog =
                    new Windows.UI.Popups.MessageDialog(
                        String.Format("Are you sure you want to delete {0} from the Recent Profile list?", selectedProfile.BattleTag),
                        "Delete Confirmation");

                messageDialog.Commands.Add(new UICommand("Yes", (command) =>
                {
                    _recentProfiles.RemoveAt(this.itemListView.SelectedIndex);

                    if (_recentProfiles.Count == 0)
                    {
                        heroesGridView.ItemsSource = null;
                    }
                    else
                    {
                        itemListView.SelectedIndex = 0;
                    }
                }));

                messageDialog.Commands.Add(new UICommand("No", (command) =>
                {
                }));

                messageDialog.DefaultCommandIndex = 1;
                await messageDialog.ShowAsync();

                SaveRecentProfiles();

                // Just in case heroes are added or removed since last refresh.
                DeleteProfileHeroesFile(selectedProfile.BattleTag);
            }
            
            // Hide the bottom app bar.
            BottomAppBar.IsOpen = false;
        }

        private void RefreshProfileButton_Click(object sender, RoutedEventArgs e)
        {
            Profile selectedProfile = this.itemListView.SelectedItem as Profile;
            if (selectedProfile != null && this.Frame != null)
            {
                RefreshProfile(selectedProfile.BattleTag, selectedProfile.Region);
            }

            // Hide the bottom app bar.
            BottomAppBar.IsOpen = false;
        }
    }
}
