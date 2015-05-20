using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Marketplace;
using Microsoft.Phone.Tasks;
using System.Globalization;

using Windows.ApplicationModel.Store;
using Store = Windows.ApplicationModel.Store;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Windows.Threading;
using System.Diagnostics;


namespace DorasAdventures
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Url of Home page
        private string MainUri = "/index.html";
        DispatcherTimer _adRefreshTimer = new DispatcherTimer();
        private static string licenseFeatureName = "DoraAdventuresLevels";



        // Constructor
        public MainPage()
        {
            InitializeComponent();
            _adRefreshTimer.Interval = TimeSpan.FromSeconds(30);
            _adRefreshTimer.Tick += _adRefreshTimer_Tick;

            if (CurrentApp.LicenseInformation.ProductLicenses.ContainsKey(licenseFeatureName))
            {
                if (CurrentApp.LicenseInformation.ProductLicenses[licenseFeatureName].IsActive)
                {
                    setFullInterface();
                }
                else
                {
                    // the customer can't access this feature
                }
            }

            CurrentApp.LicenseInformation.LicenseChanged += LicenseInformation_LicenseChanged;
        }
        void _adRefreshTimer_Tick(object sender, EventArgs e)
        {
            //theAd.Refresh();
        }

        private void LicenseInformation_LicenseChanged()
        {
            if (CurrentApp.LicenseInformation.ProductLicenses.ContainsKey(licenseFeatureName))
            {
                if (CurrentApp.LicenseInformation.ProductLicenses[licenseFeatureName].IsActive)
                {
                    setFullInterface();
                }
                else
                {
                    // the customer can't access this feature
                }
            }

        }

        private void setFullInterface()
        {
            theAd.Visibility = System.Windows.Visibility.Collapsed;
            theAd.IsEnabled = false;
            _adRefreshTimer.Stop();

        }

        private void Browser_Loaded(object sender, RoutedEventArgs e)
        {
            Browser.IsScriptEnabled = true;
            Browser.Navigate(new Uri(MainUri, UriKind.Relative));
        }

        // Handle navigation failures.
        private void Browser_NavigationFailed(object sender, System.Windows.Navigation.NavigationFailedEventArgs e)
        {
            MessageBox.Show("Navigation to this page failed, check your internet connection");
        }

        public void OnAppActivated()
        {
            Browser.InvokeScript("eval", "if (window.C2WP8Notify) C2WP8Notify('activated');");
        }

        public void OnAppDeactivated()
        {
            Browser.InvokeScript("eval", "if (window.C2WP8Notify) C2WP8Notify('deactivated');");
        }

        private static bool _isTrial = true;
        public bool IsTrial
        {
            get
            {
                return _isTrial;
            }
        }
        public bool C2SettingIsTrial = false;

        private void CheckLicense()
        {

        }

        public class ProductItem
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string FormattedPrice { get; set; }
            public string Tag { get; set; }
            public string Purchased { get; set; }
        }

        public Dictionary<string, ProductItem> productItems = new Dictionary<string, ProductItem>();
        ListingInformation li;

        public void storeListingRecieved()
        {
            if (productItems.Count > 0)
            {
                MemoryStream stream = new MemoryStream();
                DataContractJsonSerializer sr = new DataContractJsonSerializer(productItems.GetType());

                sr.WriteObject(stream, productItems);
                stream.Position = 0;

                StreamReader reader = new StreamReader(stream);
                string jsonResult = reader.ReadToEnd();

                // Pass listing obect back as a JavaScript object

                try
                {
                    Browser.InvokeScript("eval", "window['wp_call_OnStoreListing'](" + jsonResult + ");");
                }
                catch
                {
                }
            }
        }
        private async void Browser_ScriptNotify(object sender, NotifyEventArgs e)
        {


            // Get a comma delimited string from js and convert to array
            string valueStr = e.Value;
            string[] valueArr = valueStr.Split(',');

            // Trim and convert empty strings to null
            for (int i = 0; i < valueArr.Length; i++)
            {
                valueArr[i] = valueArr[i].Trim();
                if (string.IsNullOrWhiteSpace(valueArr[i]))
                    valueArr[i] = null;
            }

            // Activate trial mode
            if (valueArr[0] == "checkLicense")
            {

                // Check if trial
                if (valueArr[1] == "true")
                {
                    C2SettingIsTrial = true;
                }
                CheckLicense();
            }

            // Game loaded
            if (valueArr[0] == "gameLoaded")
            {
                Browser.Visibility = System.Windows.Visibility.Visible;

            }


            // Quit app
            if (valueArr[0] == "quitApp")
            {
                App.Current.Terminate();
            }

            // Live Tiles (http://tinyurl.com/afvhgz8)
            // *******************************************************

            // Flipped Tile
            if (valueArr[0] == "flippedTileUpdate")
            {
                ShellTile myTile = ShellTile.ActiveTiles.First();
                if (myTile != null)
                {
                    var smallBackgroundImage = valueArr[6] == null ? null : new Uri(valueArr[6], UriKind.Relative);
                    var backgroundImage = valueArr[7] == null ? null : new Uri(valueArr[7], UriKind.Relative);
                    var backBackgroundImage = valueArr[8] == null ? null : new Uri(valueArr[8], UriKind.Relative);
                    var wideBackgroundImage = valueArr[9] == null ? null : new Uri(valueArr[9], UriKind.Relative);
                    var wideBackBackgroundImage = valueArr[10] == null ? null : new Uri(valueArr[10], UriKind.Relative);

                    FlipTileData newTileData = new FlipTileData
                    {
                        Title = valueArr[1],
                        BackTitle = valueArr[2],
                        BackContent = valueArr[3],
                        WideBackContent = valueArr[4],
                        Count = Convert.ToInt32(valueArr[5]),
                        SmallBackgroundImage = smallBackgroundImage,
                        BackgroundImage = backgroundImage,
                        BackBackgroundImage = backBackgroundImage,
                        WideBackgroundImage = wideBackgroundImage,
                        WideBackBackgroundImage = wideBackBackgroundImage
                    };
                    myTile.Update(newTileData);
                }
            }

            // Payments

            // Purchase app
            if (valueArr[0] == "purchaseApp")
            {
                MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
                _marketPlaceDetailTask.Show();
            }

            // Purchase product
            if (valueArr[0] == "purchaseProduct")
            {
                string productID = valueArr[1];

                if (!CurrentApp.LicenseInformation.ProductLicenses[productID].IsActive)
                {
                    try
                    {
                        var receipt = await CurrentApp.RequestProductPurchaseAsync(productID, true);
                        if (CurrentApp.LicenseInformation.ProductLicenses[productID].IsActive)
                        {
                            Browser.InvokeScript("eval", "window['wp_call_IAPPurchaseSuccess']('" + productID + "');");
                        }
                    }
                    catch
                    {
                        // The in-app purchase was not completed because the
                        // customer canceled it or an error occurred.
                        Browser.InvokeScript("eval", "window['wp_call_IAPPurchaseFail']();");
                    }
                }
                else
                {
                    //Already owns the product
                }
            }

            // Request store listing
            if (valueArr[0] == "requestStoreListing")
            {
                try
                {

                    li = await Store.CurrentApp.LoadListingInformationAsync();

                    foreach (string key in li.ProductListings.Keys)
                    {
                        ProductListing pListing = li.ProductListings[key];

                        productItems[pListing.ProductId] = new ProductItem
                        {
                            Name = pListing.Name,
                            Description = pListing.Description,
                            FormattedPrice = pListing.FormattedPrice,
                            Tag = pListing.Tag,
                            Purchased = CurrentApp.LicenseInformation.ProductLicenses[key].IsActive ? "True" : "False"
                        };
                    }

                    storeListingRecieved();
                }
                catch (Exception)
                {
                    // Failed to load listing information
                }
            }

            // Rate App
            if (valueArr[0] == "rateApp")
            {
                MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                marketplaceReviewTask.Show();
            }

            // Launch Marketplace Details
            if (valueArr[0] == "launchMarketplaceDetails")
            {
                string appID = valueArr[1];

                //Show an application, using the default ContentType.
                MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();

                marketplaceDetailTask.ContentIdentifier = appID;
                marketplaceDetailTask.ContentType = MarketplaceContentType.Applications;

                marketplaceDetailTask.Show();

            }
        }

        private void theAd_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            Debug.WriteLine(e.Error.ToString());
        }
    }
}