﻿using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Entities;
using OfficeDevPnP.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using LanguageTemplateHash = System.Collections.Generic.Dictionary<string,
                                                                    System.Collections.Generic.List<string>>;

namespace Microsoft.SharePoint.Client
{

    /// <summary>
    /// Class that deals with branding features
    /// </summary>
    public static class BrandingExtensions
    {
        const string AvailablePageLayouts = "__PageLayouts";
        const string DefaultPageLayout = "__DefaultPageLayout";
        const string AvailableWebTemplates = "__WebTemplates";

        /// <summary>
        /// Deploy new theme to site collection. To be used with root web in site collection
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="colorFilePath">Color file for the theme</param>
        /// <param name="fontFilePath">Font file for the theme</param>
        /// <param name="backgroundImagePath">Background image for the team</param>
        /// <param name="masterPageName">Master page name for the theme. Only name of the master page needed, no full path to catalog</param>
        public static void DeployThemeToWeb(this Web web, string themeName, string colorFilePath, string fontFilePath, string backgroundImagePath, string masterPageName)
        {
            // Let's also add entry to the Theme catalog. This is not actually required, but provides visibility for the theme option, if manually changed
            DeployThemeToWebImplementation(web, web, themeName, colorFilePath, fontFilePath, backgroundImagePath, masterPageName);
        }

        /// <summary>
        /// Deploy new theme to specific site and ensure that theme exists in the site collection. Should be used if theme is deployed to sub site in site collection.
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="colorFilePath">Color file for the theme</param>
        /// <param name="fontFilePath">Font file for the theme</param>
        /// <param name="backgroundImagePath">Background image for the team</param>
        /// <param name="masterPageName">Master page name for the theme. Only name of the master page needed, no full path to catalog</param>
        public static void DeployThemeToSubWeb(this Web web, Web rootWeb, string themeName, string colorFilePath, string fontFilePath, string backgroundImagePath, string masterPageName)
        {
            // Let's also add entry to the Theme catalog. This is not actually required, but provides visibility for the theme option, if manually changed
            DeployThemeToWebImplementation(web, rootWeb, themeName, colorFilePath, fontFilePath, backgroundImagePath, masterPageName);
        }        

        private static void DeployThemeToWebImplementation(Web web, Web rootWeb, string themeName, string colorFilePath, string fontFilePath, string backgroundImagePath, string masterPageName)
        {
            LoggingUtility.Internal.TraceInformation((int)EventId.DeployTheme, "Deploying theme '{0}' to '{1}'", themeName, web.Context.Url);

            // Deploy files one by one to proper location
            if (!string.IsNullOrEmpty(colorFilePath) && System.IO.File.Exists(colorFilePath))
            {
                rootWeb.DeployFileToThemeFolderSite(colorFilePath);
            }
            if (!string.IsNullOrEmpty(fontFilePath) && System.IO.File.Exists(fontFilePath))
            {
                rootWeb.DeployFileToThemeFolderSite(fontFilePath);
            }
            if (!string.IsNullOrEmpty(backgroundImagePath) && System.IO.File.Exists(backgroundImagePath))
            {
                rootWeb.DeployFileToThemeFolderSite(backgroundImagePath);
            }

            // Let's also add entry to the Theme catalog. This is not actually required, but provides visibility for the theme option, if manually changed
            web.AddNewThemeOptionToSiteImplementation(rootWeb, themeName, colorFilePath, fontFilePath, backgroundImagePath, masterPageName);
        }

        /// <summary>
        /// Checks to see if the theme already exists.
        /// </summary>
        /// <param name="web">Site to be processed</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <returns>True if theme exists, false otherwise</returns>
        public static bool ThemeEntryExists(this Web web, string themeName)
        {
            // Let's get instance to the composite look gallery
            List themesList = web.GetCatalog(124);
            web.Context.Load(themesList);
            web.Context.ExecuteQuery();

            return web.ThemeEntryExists(themeName, themesList);
        }

        /// <summary>
        /// Checks to see if the theme already exists
        /// </summary>
        /// <param name="web">Site to be processed</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="themeGalleryList">SharePoint theme gallery list</param>
        /// <returns>True if theme exists, false otherwise</returns>
        public static bool ThemeEntryExists(this Web web, string themeName, List themeGalleryList)
        {
            CamlQuery query = new CamlQuery();
            string camlString = @"
                <View>
                    <Query>                
                        <Where>
                            <Eq>
                                <FieldRef Name='Name' />
                                <Value Type='Text'>{0}</Value>
                            </Eq>
                        </Where>
                     </Query>
                </View>";
            // Let's update the theme name accordingly
            camlString = string.Format(camlString, themeName);
            query.ViewXml = camlString;
            var found = themeGalleryList.GetItems(query);
            web.Context.Load(found);
            web.Context.ExecuteQuery();
            if (found.Count > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add new theme option to the sub site.
        /// </summary>
        /// <param name="web">Actual site where theme is applied.</param>
        /// <param name="rootWeb">Root site of the site collection. Needed for resolving the right relative path for the files</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="colorFilePath">Color file for the theme</param>
        /// <param name="fontFilePath">Font file for the theme</param>
        /// <param name="backgroundImagePath">Background image for the team</param>
        /// <param name="masterPageName">Master page name for the theme. Only name of the master page needed, no full path to catalog</param>
        public static void AddNewThemeOptionToSubWeb(this Web web, Web rootWeb, string themeName, string colorFilePath, string fontFilePath, string backgroundPath, string masterPageName)
        {
            AddNewThemeOptionToSiteImplementation(web, rootWeb, themeName, colorFilePath, fontFilePath, backgroundPath, masterPageName);
        }

        /// <summary>
        /// Add new theme option to the site.
        /// </summary>
        /// <param name="web">Actual site where theme is applied.</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="colorFilePath">Color file for the theme</param>
        /// <param name="fontFilePath">Font file for the theme</param>
        /// <param name="backgroundImagePath">Background image for the team</param>
        /// <param name="masterPageName">Master page name for the theme. Only name of the master page needed, no full path to catalog</param>
        public static void AddNewThemeOptionToWeb(this Web web, string themeName, string colorFilePath, string fontFilePath, string backgroundPath, string masterPageName)
        {
            AddNewThemeOptionToSiteImplementation(web, web, themeName, colorFilePath, fontFilePath, backgroundPath, masterPageName);
        }

        /// <summary>
        /// Add new theme option to the site.
        /// </summary>
        /// <param name="web">Actual site where theme is applied.</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="colorFilePath">Color file for the theme</param>
        /// <param name="fontFilePath">Font file for the theme</param>
        /// <param name="backgroundImagePath">Background image for the team</param>
        /// <param name="masterPageName">Master page name for the theme. Only name of the master page needed, no full path to catalog</param>
        [Obsolete("Please use the AddNewThemeOptionToWeb method")]
        public static void AddNewThemeOptionToSite(this Web web, string themeName, string colorFilePath, string fontFilePath, string backgroundPath, string masterPageName)
        {
            AddNewThemeOptionToSiteImplementation(web, web, themeName, colorFilePath, fontFilePath, backgroundPath, masterPageName);
        }
        /// <summary>
        /// Add new theme option to the site.
        /// </summary>
        /// <param name="web">Actual site where theme is applied.</param>
        /// <param name="rootWeb">Root site of the site collection. Needed for resolving the right relative path for the files</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="themeName">Name for the new theme</param>
        /// <param name="colorFilePath">Color file for the theme</param>
        /// <param name="fontFilePath">Font file for the theme</param>
        /// <param name="backgroundImagePath">Background image for the team</param>
        /// <param name="masterPageName">Master page name for the theme. Only name of the master page needed, no full path to catalog</param>
        private static void AddNewThemeOptionToSiteImplementation(this Web web, Web rootWeb, string themeName, string colorFilePath, string fontFilePath, string backgroundPath, string masterPageName)
        {
            // Let's get instance to the composite look gallery of specific site
            List themesOverviewList = web.GetCatalog(124);
            web.Context.Load(themesOverviewList);
            web.Context.ExecuteQuery();
            // Is the item already in the list?
            if (!web.ThemeEntryExists(themeName, themesOverviewList))
            {
                // Let's ensure that we have root site loaded for setting URLs properly
                Utility.EnsureWeb(rootWeb.Context, rootWeb, "ServerRelativeUrl");
                Utility.EnsureWeb(web.Context, web, "ServerRelativeUrl");

                // Let's create new theme entry. Notice that theme selection is not available from UI in personal sites, so this is just for consistency sake
                ListItemCreationInformation itemInfo = new ListItemCreationInformation();
                Microsoft.SharePoint.Client.ListItem item = themesOverviewList.AddItem(itemInfo);
                item["Name"] = themeName;
                item["Title"] = themeName;
                if (!string.IsNullOrEmpty(colorFilePath) && System.IO.File.Exists(colorFilePath))
                {
                    item["ThemeUrl"] = UrlUtility.Combine(rootWeb.ServerRelativeUrl, string.Format("/_catalogs/theme/15/{0}", Path.GetFileName(colorFilePath)));
                }
                if (!string.IsNullOrEmpty(fontFilePath) && System.IO.File.Exists(fontFilePath))
                {
                    item["FontSchemeUrl"] = UrlUtility.Combine(rootWeb.ServerRelativeUrl, string.Format("/_catalogs/theme/15/{0}", Path.GetFileName(fontFilePath)));
                }
                if (!string.IsNullOrEmpty(backgroundPath) && System.IO.File.Exists(backgroundPath))
                {
                    item["ImageUrl"] = UrlUtility.Combine(rootWeb.ServerRelativeUrl, string.Format("/_catalogs/theme/15/{0}", Path.GetFileName(backgroundPath)));
                }
                // we use seattle master if anything else is not set
                if (string.IsNullOrEmpty(masterPageName))
                {
                    item["MasterPageUrl"] = UrlUtility.Combine(web.ServerRelativeUrl, "/_catalogs/masterpage/seattle.master");
                }
                else
                {
                    item["MasterPageUrl"] = UrlUtility.Combine(web.ServerRelativeUrl, string.Format("/_catalogs/masterpage/{0}", Path.GetFileName(masterPageName)));
                }

                item["DisplayOrder"] = 11;
                item.Update();
                web.Context.ExecuteQuery();
            }

        }

        /// <summary>
        /// Set theme for a site. To be used with root web.
        /// </summary>
        /// <param name="web">Set theme for the root web of a site collection</param>
        /// <param name="themeName">Name of the new theme</param>
        public static void SetThemeToWeb(this Web web, string themeName)
        {
            SetThemeToWebImplementation(web, web, themeName);
        }

        /// <summary>
        /// Set theme for a site. To be used with sub sites.
        /// </summary>
        /// <param name="web">Set theme for a sub site</param>
        /// <param name="rootWeb">Root web, needed as the theme is stored in the root web</param>
        /// <param name="themeName">Name of the new theme</param>
        public static void SetThemeToSubWeb(this Web web, Web rootWeb, string themeName)
        {
            SetThemeToWebImplementation(web, rootWeb, themeName);
        }

        private static void SetThemeToWebImplementation(this Web web, Web rootWeb, string themeName)
        {
            LoggingUtility.Internal.TraceInformation((int)EventId.SetTheme, "Setting theme '{0}' for '{1}'", themeName, web.Context.Url);

            // Let's get instance to the composite look gallery
            List themeList = rootWeb.GetCatalog(124);
            rootWeb.Context.Load(themeList);
            rootWeb.Context.ExecuteQuery();

            // Double checking that theme exists
            if (rootWeb.ThemeEntryExists(themeName, themeList))
            {
                CamlQuery query = new CamlQuery();
                string camlString = @"
                <View>
                    <Query>                
                        <Where>
                            <Eq>
                                <FieldRef Name='Name' />
                                <Value Type='Text'>{0}</Value>
                            </Eq>
                        </Where>
                     </Query>
                </View>";
                
                // Let's update the theme name accordingly
                camlString = string.Format(camlString, themeName);
                query.ViewXml = camlString;
                var found = themeList.GetItems(query);
                rootWeb.Context.Load(found);
                rootWeb.Context.ExecuteQuery();
                if (found.Count > 0)
                {
                    ListItem themeEntry = found[0];
                    //Set the properties for applying custom theme which was just uploaded
                    string spColorURL = null;
                    if (themeEntry["ThemeUrl"] != null && themeEntry["ThemeUrl"].ToString().Length > 0)
                    {
                        spColorURL = UrlUtility.MakeRelativeUrl((themeEntry["ThemeUrl"] as FieldUrlValue).Url);
                    }
                    string spFontURL = null;
                    if (themeEntry["FontSchemeUrl"] != null && themeEntry["FontSchemeUrl"].ToString().Length > 0)
                    {
                        spFontURL = UrlUtility.MakeRelativeUrl((themeEntry["FontSchemeUrl"] as FieldUrlValue).Url);
                    }
                    string backGroundImage = null;
                    if (themeEntry["ImageUrl"] != null && themeEntry["ImageUrl"].ToString().Length > 0)
                    {
                        backGroundImage = UrlUtility.MakeRelativeUrl((themeEntry["ImageUrl"] as FieldUrlValue).Url);
                    }

                    LoggingUtility.Internal.TraceVerbose("Apply theme '{0}', '{1}', '{2}'.", spColorURL, spFontURL, backGroundImage);
                    // Set theme for demonstration
                    // TODO: Why is shareGenerated false? If deploying to root an inheriting, then maybe use shareGenerated = true.
                    web.ApplyTheme(spColorURL,
                                        spFontURL,
                                        backGroundImage,
                                        shareGenerated: false);

                    // Let's also update master page, if needed
                    if (themeEntry["MasterPageUrl"] != null && themeEntry["MasterPageUrl"].ToString().Length > 0)
                    {
                        var masterUrl = UrlUtility.MakeRelativeUrl((themeEntry["MasterPageUrl"] as FieldUrlValue).Url);
                        LoggingUtility.Internal.TraceVerbose("Set masterpage '{0}'.", masterUrl);
                        web.MasterUrl = masterUrl;
                    }

                    web.Context.ExecuteQuery();
                }
            }
        }

        //TODO: to be replaced by new site logo CSOM once we've the April 2014 CU
        //Note: does seem to broken on the current SPO implementation (20/03/2014) as there's no _themes folder anymore in the root web
        public static void SetSiteLogo(this Web web, string fullPathToLogo)
        {
            if (string.IsNullOrEmpty(fullPathToLogo) || !System.IO.File.Exists(fullPathToLogo))
            {
                return;
            }

            // Not natively supported, but we can update the themed site icon. If initial theme was just applied, image is at
            // _themes/0/siteIcon-2129F729.themedpng
            Folder rootFolder = web.RootFolder;
            Folder themeFolder = rootFolder.ResolveSubFolder("_themes");
            Folder themeAssetsFolder = themeFolder.ResolveSubFolder("0");

            // Use CSOM to upload the file in
            FileCreationInformation newFile = new FileCreationInformation();
            newFile.Content = System.IO.File.ReadAllBytes(fullPathToLogo);
            newFile.Url = themeAssetsFolder.ServerRelativeUrl + "/siteIcon-2129F729.themedpng";
            newFile.Overwrite = true;
            Microsoft.SharePoint.Client.File uploadFile = themeAssetsFolder.Files.Add(newFile);
            web.Context.Load(uploadFile);
            web.Context.ExecuteQuery();
        }

        public static void DeployFileToThemeFolderSite(this Web web, string sourceFileAddress, string themeFolderVersion = "15") {
            // Get the path to the file which we are about to deploy
            var fileBytes = System.IO.File.ReadAllBytes(sourceFileAddress);
            var fileName = Path.GetFileName(sourceFileAddress);

            DeployFileToThemeFolderSite(web, fileBytes, fileName, themeFolderVersion);
        }

        public static void DeployFileToThemeFolderSite(this Web web, byte[] fileBytes, string fileName, string themeFolderVersion = "15")
        {
            LoggingUtility.Internal.TraceVerbose("Deploying file '{0}' to '{1}' folder '{2}'.", fileName, web.Context.Url, themeFolderVersion);

            // Get the path to the file which we are about to deploy
            List themesList = web.GetCatalog(123);

            // get the theme list
            web.Context.Load(themesList);
            web.Context.ExecuteQuery();
            
            Folder rootFolder = themesList.RootFolder;
            FolderCollection rootFolders = rootFolder.Folders;
            web.Context.Load(rootFolder);
            web.Context.Load(rootFolders, f => f.Where(folder => folder.Name == themeFolderVersion));
            web.Context.ExecuteQuery();

            Folder folder15 = rootFolders.FirstOrDefault();

            // Use CSOM to upload the file in
            FileCreationInformation newFile = new FileCreationInformation();
            newFile.Content = fileBytes;
            newFile.Url = UrlUtility.Combine(folder15.ServerRelativeUrl, fileName);
            newFile.Overwrite = true;

            Microsoft.SharePoint.Client.File uploadFile = folder15.Files.Add(newFile);
            web.Context.Load(uploadFile);
            web.Context.ExecuteQuery();
        }



        /// <summary>
        /// Can be used to deploy page layouts to master page gallery. 
        /// <remarks>Should be only used with root web of site collection where publishing features are enabled.</remarks>
        /// </summary>
        /// <param name="web">Web as the root site of the publishing site collection</param>
        /// <param name="sourceFilePath">Full path to the file which will be uploaded</param>
        /// <param name="title">Title for the page layout</param>
        /// <param name="description">Description for the page layout</param>
        /// <param name="associatedContentTypeID">Associated content type ID</param>
        public static void DeployPageLayout(this Web web, string sourceFilePath, string title, string description, string associatedContentTypeID)
        {
            // Get the path to the file which we are about to deploy
            List masterPageGallery = web.GetCatalog(116);
            Folder rootFolder = masterPageGallery.RootFolder;
            web.Context.Load(masterPageGallery);
            web.Context.Load(rootFolder);
            web.Context.ExecuteQuery();

            string fileName = Path.GetFileName(sourceFilePath);
            var fileBytes = System.IO.File.ReadAllBytes(sourceFilePath);

            // Use CSOM to upload the file in
            FileCreationInformation newFile = new FileCreationInformation();
            newFile.Content = fileBytes;
            newFile.Url = UrlUtility.Combine(rootFolder.ServerRelativeUrl, fileName);
            newFile.Overwrite = true;

            Microsoft.SharePoint.Client.File uploadFile = rootFolder.Files.Add(newFile);
            web.Context.Load(uploadFile);
            web.Context.ExecuteQuery();

            // Check out the file if needed
            if (masterPageGallery.ForceCheckout || masterPageGallery.EnableVersioning)
            {
                if (uploadFile.CheckOutType == CheckOutType.None)
                {
                    uploadFile.CheckOut();
                }
            }

            // Get content type for ID to assign associated content type information
            ContentType associatedCt = web.GetContentTypeById(associatedContentTypeID);

            var listItem = uploadFile.ListItemAllFields;
            listItem["Title"] = title;
            listItem["MasterPageDescription"] = description;
            // set the item as page layout
            listItem["ContentTypeId"] = "0x01010007FF3E057FA8AB4AA42FCB67B453FFC100E214EEE741181F4E9F7ACC43278EE811";
            // Set the associated content type ID property
            listItem["PublishingAssociatedContentType"] = ";#" + associatedCt.Name + ";#" + associatedCt.Id + ";#";
            listItem["UIVersion"] = Convert.ToString(15);
            listItem.Update();

            // Check in the page layout if needed
            if (masterPageGallery.ForceCheckout || masterPageGallery.EnableVersioning)
            {
                uploadFile.CheckIn("", CheckinType.MajorCheckIn);
                listItem.File.Publish("");
            }
            web.Context.ExecuteQuery();

        }

        public static void DeployMasterPage(this Web web, string sourceFilePath, string title, string description, string uiVersion = "15", string defaultCSSFile = "")
        {
            // Get the path to the file which we are about to deploy
            List masterPageGallery = web.GetCatalog(116);
            Folder rootFolder = masterPageGallery.RootFolder;
            web.Context.Load(masterPageGallery);
            web.Context.Load(rootFolder);
            web.Context.ExecuteQuery();

            // Get the file name from the provided path
            string fileName = Path.GetFileName(sourceFilePath);
            var fileBytes = System.IO.File.ReadAllBytes(sourceFilePath);

            // Use CSOM to upload the file in
            FileCreationInformation newFile = new FileCreationInformation();
            newFile.Content = fileBytes;
            newFile.Url = UrlUtility.Combine(rootFolder.ServerRelativeUrl, fileName);
            newFile.Overwrite = true;

            Microsoft.SharePoint.Client.File uploadFile = rootFolder.Files.Add(newFile);
            web.Context.Load(uploadFile);
            web.Context.ExecuteQuery();


            var listItem = uploadFile.ListItemAllFields;
            if (masterPageGallery.ForceCheckout || masterPageGallery.EnableVersioning)
            {
                if (uploadFile.CheckOutType == CheckOutType.None)
                {
                    uploadFile.CheckOut();
                }
            }

            listItem["Title"] = title;
            listItem["MasterPageDescription"] = description;
            // Set content type as master page
            listItem["ContentTypeId"] = "0x01010500B45822D4B60B7B40A2BFCC0995839404";
            listItem["UIVersion"] = uiVersion;
            listItem.Update();
            if (masterPageGallery.ForceCheckout || masterPageGallery.EnableVersioning)
            {
                uploadFile.CheckIn("", CheckinType.MajorCheckIn);
                listItem.File.Publish("");
            }
            web.Context.Load(listItem);
            web.Context.ExecuteQuery();

        }

        /// <summary>
        /// Can be used to set master page and custom master page in single command
        /// </summary>
        /// <param name="web"></param>
        /// <param name="masterPageName"></param>
        /// <param name="customMasterPageName"></param>
        public static void SetMasterPagesForSiteByName(this Web web, string masterPageName, string customMasterPageName)
        {
            web.SetMasterPageForSiteByName(masterPageName);
            web.SetCustomMasterPageForSiteByName(customMasterPageName);
        }

        /// <summary>
        /// Can be used to set master page and custom master page in single command
        /// </summary>
        /// <param name="web"></param>
        /// <param name="masterPageName"></param>
        /// <param name="customMasterPageName"></param>
        public static void SetMasterPagesForSiteByUrl(this Web web, string masterPageName, string customMasterPageName)
        {
            web.SetMasterPageForSiteByUrl(masterPageName);
            web.SetCustomMasterPageForSiteByUrl(customMasterPageName);
        }

        /// <summary>
        /// Master page is set by using master page name. Master page is set from the current web.
        /// </summary>
        /// <param name="web">Current web</param>
        /// <param name="masterPageName">Name of the master page. Path is resolved from this.</param>
        public static void SetMasterPageForSiteByName(this Web web, string masterPageName)
        {
            string masterPageUrl = GetRelativeUrlForMasterByName(web, masterPageName);
            if (!string.IsNullOrEmpty(masterPageUrl))
            {
                SetMasterPageForSiteByUrl(web, masterPageUrl);
            }

        }

        /// <summary>
        /// Master page is set by using master page name. Master page is set from the current web.
        /// </summary>
        /// <param name="web">Current web</param>
        /// <param name="masterPageName">Name of the master page. Path is resolved from this.</param>
        public static void SetCustomMasterPageForSiteByName(this Web web, string masterPageName)
        {
            string masterPageUrl = GetRelativeUrlForMasterByName(web, masterPageName);
            if (!string.IsNullOrEmpty(masterPageUrl))
            {
                SetCustomMasterPageForSiteByUrl(web, masterPageUrl);
            }
        }

        public static string GetRelativeUrlForMasterByName(this Web web, string masterPageName)
        {
            List masterPageGallery = web.GetCatalog(116);
            CamlQuery query = new CamlQuery();
            query.ViewXml = "<View><Query><Where><Contains><FieldRef Name='FileRef'/><Value Type='Text'>.master</Value></Contains></Where></Query></View>";
            ListItemCollection galleryItems = masterPageGallery.GetItems(query);
            web.Context.Load(masterPageGallery);
            web.Context.Load(galleryItems);
            web.Context.ExecuteQuery();
            foreach (var item in galleryItems)
            {
                if (item["FileRef"].ToString().ToLowerInvariant().Contains(masterPageName.ToLowerInvariant()))
                {
                    return item["FileRef"].ToString().ToLowerInvariant();
                }
            }
            return string.Empty;
        }

        public static ListItem GetPageLayoutListItemByName(this Web web, string pageLayoutName)
        {
            List masterPageGallery = web.GetCatalog(116);
            CamlQuery query = new CamlQuery();
            query.ViewXml = "<View><Query><Where><Contains><FieldRef Name='FileRef'/><Value Type='Text'>.aspx</Value></Contains></Where></Query></View>";
            ListItemCollection galleryItems = masterPageGallery.GetItems(query);
            web.Context.Load(masterPageGallery);
            web.Context.Load(galleryItems);
            web.Context.ExecuteQuery();
            foreach (var item in galleryItems)
            {
                if (item["FileRef"].ToString().ToLowerInvariant().Contains(pageLayoutName.ToLowerInvariant()))
                {
                    return item;
                }
            }
            return null;
        }


        /// <summary>
        /// Set master page by using given URL as parameter. Suitable for example in cases where you want sub sites to reference root site master page gallery. This is typical with publishing sites.
        /// </summary>
        /// <param name="web">Context web</param>
        /// <param name="masterPageName">URL to the master page.</param>
        public static void SetMasterPageForSiteByUrl(this Web web, string masterPageUrl)
        {
            web.MasterUrl = masterPageUrl;
            web.Update();
            web.Context.ExecuteQuery();
        }

        /// <summary>
        /// Set Custom master page by using given URL as parameter. Suitable for example in cases where you want sub sites to reference root site master page gallery. This is typical with publishing sites.
        /// </summary>
        /// <param name="web">Context web</param>
        /// <param name="masterPageName">URL to the master page.</param>
        public static void SetCustomMasterPageForSiteByUrl(this Web web, string masterPageUrl)
        {
            web.CustomMasterUrl = masterPageUrl;
            web.Update();
            web.Context.ExecuteQuery();
        }

        /// <summary>
        /// Sets specific page layout the default page layout for the particular site
        /// </summary>
        /// <param name="web"></param>
        /// <param name="rootWeb"></param>
        /// <param name="pageLayoutName"></param>
        public static void SetDefaultPageLayoutForSite(this Web web, Web rootWeb, string pageLayoutName)
        {
            // Save to property bag as the default page layout for the site
            XmlDocument xd = new XmlDocument();
            web.SetPropertyBagValue(DefaultPageLayout, CreateXmlNodeFromPageLayout(xd, web, rootWeb, pageLayoutName).OuterXml);
        }

        private static XmlNode CreateXmlNodeFromPageLayout(XmlDocument xd, Web web, Web rootWeb, string pageLayoutName)
        {
            ListItem pageLayout = rootWeb.GetPageLayoutListItemByName(pageLayoutName);

            // Parse the right styled xml for the layout - <layout guid="944ea6be-f287-42c6-aa11-3fd75ab1ee9e" url="_catalogs/masterpage/ArticleLeft.aspx" />
            XmlNode xmlNode = xd.CreateElement("layout");
            XmlAttribute xmlAttribute = xd.CreateAttribute("guid");
            xmlAttribute.Value = pageLayout["UniqueId"].ToString();
            XmlAttribute xmlAttribute2 = xd.CreateAttribute("url");
            // Get relative URL to the particular site collection
            xmlAttribute2.Value = SolveSiteRelativeUrl(rootWeb, pageLayout["FileRef"].ToString());
            xmlNode.Attributes.SetNamedItem(xmlAttribute);
            xmlNode.Attributes.SetNamedItem(xmlAttribute2);
            return xmlNode;
        }

        [Obsolete("Use SolveSiteRelativeUrl")]
        private static string SolveSiteRelateveUrl(Web web, string url)
        {
            return SolveSiteRelativeUrl(web, url);
        }
        private static string SolveSiteRelativeUrl(Web web, string url)
        {
            Utility.EnsureWeb(web.Context, web, "ServerRelativeUrl");
            string newUrl = url.Substring(web.ServerRelativeUrl.Length);
            if (newUrl.Length > 0 && newUrl[0] == '/')
            {
                newUrl = newUrl.Substring(1);
            }
            return newUrl;
        }

        /// <summary>
        /// Can be used to set the site to inherit the default page layout option from parent. Cannot be used for root site of the site collection
        /// </summary>
        /// <param name="web"></param>
        public static void SetSiteToInheritPageLayouts(this Web web)
        {
            web.SetPropertyBagValue(DefaultPageLayout, "__inherit");
        }

        /// <summary>
        /// Can be used to remote filters from the available page layouts
        /// </summary>
        /// <param name="web"></param>
        public static void ClearAvailablePageLayouts(this Web web)
        {
            web.SetPropertyBagValue(AvailablePageLayouts, "");
        }


        public static void SetAvailablePageLayouts(this Web web, Web rootWeb, List<string> pageLayouts)
        {
            XmlDocument xd = new XmlDocument();
            XmlNode xmlNode = xd.CreateElement("pagelayouts");
            xd.AppendChild(xmlNode);
            foreach (var item in pageLayouts)
            {
                xmlNode.AppendChild(CreateXmlNodeFromPageLayout(xd, web, rootWeb, item));
            }
            web.SetPropertyBagValue(AvailablePageLayouts, xmlNode.OuterXml);
        }

        public static void SetAvailableWebTemplates(this Web web, List<WebTemplateEntity> availableTemplates)
        {
            string propertyValue = string.Empty;

            LanguageTemplateHash languages = new LanguageTemplateHash();
            foreach (var item in availableTemplates)
            {
                AddTemplateToCollection(languages, item);
            }

            if (availableTemplates.Count > 0)
            {
                XmlDocument xd = new XmlDocument();
                XmlNode xmlNode = xd.CreateElement("webtemplates");
                xd.AppendChild(xmlNode);
                foreach (var language in languages)
                {
                    XmlNode xmlLcidNode = xmlNode.AppendChild(xd.CreateElement("lcid"));
                    XmlAttribute xmlAttribute = xd.CreateAttribute("id");
                    xmlAttribute.Value = language.Key;
                    xmlLcidNode.Attributes.SetNamedItem(xmlAttribute);

                    foreach (string item in language.Value)
                    {
                        XmlNode xmlWTNode = xmlLcidNode.AppendChild(xd.CreateElement("webtemplate"));
                        XmlAttribute xmlAttributeName = xd.CreateAttribute("name");
                        xmlAttributeName.Value = item;
                        xmlWTNode.Attributes.SetNamedItem(xmlAttributeName);
                    }
                }
                propertyValue = xmlNode.OuterXml;
            }
            //Save the xml entry to property bag
            web.SetPropertyBagValue(AvailableWebTemplates, propertyValue);
            //Set that templates are not inherited
            web.SetPropertyBagValue("__InheritWebTemplates", "False");
        }

        /// <summary>
        /// Can be used to remote filters from the available web template
        /// </summary>
        /// <param name="web"></param>
        public static void ClearAvailableWebTemplates(this Web web)
        {
            web.SetPropertyBagValue(AvailableWebTemplates, "");
        }

        private static void AddTemplateToCollection(LanguageTemplateHash languages, WebTemplateEntity item)
        {
            string key = string.Empty;
            if (string.IsNullOrEmpty(item.LanguageCode))
            {
                key = "all";
            }
            else
            {
                key = item.LanguageCode;
            }

            if (!languages.ContainsKey(key))
            {
                languages[key] = new List<string>();
            }
            (languages[key] as List<string>).Add(item.TemplateName);
        }
    }
}