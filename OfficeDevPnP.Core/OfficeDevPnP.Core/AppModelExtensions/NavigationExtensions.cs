﻿using Microsoft.SharePoint.Client;
using OfficeDevPnP.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SharePoint.Client
{
    /// <summary>
    /// This class holds navigation related methods
    /// </summary>
    public static class NavigationExtensions
    {
        #region Navigation elements  - quicklaunch and top navigation
        /// <summary>
        /// Add a node to quickLaunch or top navigation bar
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="nodeTitle">the title of node to add</param>
        /// <param name="nodeUri">the url of node to add</param>
        /// <param name="parentNodeTitle">if string.Empty, then will add this node as top level node</param>
        /// <param name="isQucikLaunch">true: add to quickLaunch; otherwise, add to top navigation bar</param>
        public static void AddNavigationNode(this Web web, string nodeTitle, Uri nodeUri, string parentNodeTitle, bool isQuickLaunch)
        {
            web.Context.Load(web, w => w.Navigation.QuickLaunch, w => w.Navigation.TopNavigationBar);
            web.Context.ExecuteQuery();
            NavigationNodeCreationInformation node = new NavigationNodeCreationInformation();
            node.AsLastNode = true;
            node.Title = nodeTitle;
            node.Url = nodeUri != null ? nodeUri.OriginalString : "";

            if (isQuickLaunch)
            {
                var quickLaunch = web.Navigation.QuickLaunch;
                if (string.IsNullOrEmpty(parentNodeTitle))
                {
                    quickLaunch.Add(node);
                }
                else
                {
                    foreach (var nodeInfo in quickLaunch)
                    {
                        if (nodeInfo.Title == parentNodeTitle)
                        {
                            nodeInfo.Children.Add(node);
                            break;
                        }
                    }
                }
            }
            else
            {
                var topLink = web.Navigation.TopNavigationBar;
                topLink.Add(node);
            }
            web.Context.ExecuteQuery();
        }

        /// <summary>
        /// Deletes a navigation node from the quickLaunch or top navigation bar
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="nodeTitle">the title of node to delete</param>
        /// <param name="parentNodeTitle">if string.Empty, then will delete this node as top level node</param>
        /// <param name="isQuickLaunch">true: delete from quickLaunch; otherwise, delete from top navigation bar</param>
        public static void DeleteNavigationNode(this Web web, string nodeTitle, string parentNodeTitle, bool isQuickLaunch)
        {
            web.Context.Load(web, w => w.Navigation.QuickLaunch, w => w.Navigation.TopNavigationBar);
            web.Context.ExecuteQuery();

            if (isQuickLaunch)
            {
                var quickLaunch = web.Navigation.QuickLaunch;
                if (string.IsNullOrEmpty(parentNodeTitle))
                {
                    foreach (var nodeInfo in quickLaunch)
                    {
                        if (nodeInfo.Title == nodeTitle)
                        {
                            nodeInfo.DeleteObject();
                            web.Context.ExecuteQuery();
                            break;
                        }
                    }
                }
                else
                {
                    bool done = false;
                    foreach (var nodeInfo in quickLaunch)
                    {
                        if (nodeInfo.Title == parentNodeTitle)
                        {
                            web.Context.Load(nodeInfo.Children);
                            web.Context.ExecuteQuery();
                            foreach (var nodeInfo2 in nodeInfo.Children)
                            {
                                if (nodeInfo2.Title == nodeTitle)
                                {
                                    nodeInfo2.DeleteObject();
                                    web.Context.ExecuteQuery();
                                    done = true;
                                    break;
                                }
                            }
                            if (done) break;
                        }
                    }
                }
            }
            else
            {
                var topLink = web.Navigation.TopNavigationBar;
                foreach (var nodeInfo in topLink)
                {
                    if (nodeInfo.Title == nodeTitle)
                    {
                        nodeInfo.DeleteObject();
                        web.Context.ExecuteQuery();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes all Quick Launch nodes
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        public static void DeleteAllQuickLaunchNodes(this Web web)
        {

            web.Context.Load(web, w => w.Navigation.QuickLaunch);
            web.Context.ExecuteQuery();

            var quickLaunch = web.Navigation.QuickLaunch;
            for (int i = quickLaunch.Count - 1; i >= 0; i--)
            {
                quickLaunch[i].DeleteObject();
            }
            web.Context.ExecuteQuery();
        }

        /// <summary>
        /// Updates the navigation inheritance setting
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="inheritNavigation">boolean indicating if navigation inheritance is needed or not</param>
        public static void UpdateNavigationInheritance(this Web web, bool inheritNavigation)
        {
            web.Navigation.UseShared = inheritNavigation;
            web.Update();
            web.Context.ExecuteQuery();
        }
        #endregion

        #region Custom actions
        /// <summary>
        /// Adds or removes a custom action from a site
        /// </summary>
        /// <param name="web">Site to be processed - can be root web or sub site</param>
        /// <param name="customAction">Information about the custom action be added or deleted</param>
        /// <example>
        /// var editAction = new CustomActionEntity()
        /// {
        ///     Title = "Edit Site Classification",
        ///     Description = "Manage business impact information for site collection or sub sites.",
        ///     Sequence = 1000,
        ///     Group = "SiteActions",
        ///     Location = "Microsoft.SharePoint.StandardMenu",
        ///     Url = EditFormUrl,
        ///     ImageUrl = EditFormImageUrl,
        ///     Rights = new BasePermissions(),
        /// };
        /// editAction.Rights.Set(PermissionKind.ManageWeb);
        /// AddCustomAction(editAction, new Uri(site.Properties.Url));
        /// </example>
        /// <returns>True if action was ok</returns>
        public static bool AddCustomAction(this Web web, CustomActionEntity customAction)
        {
            var existingActions = web.UserCustomActions;
            web.Context.Load(existingActions);
            web.Context.ExecuteQuery();

            // first delete the action with the same name (if it exists)
            var actions = existingActions.ToArray();
            foreach (var action in actions)
            {
                if (action.Description == customAction.Description &&
                    action.Location == customAction.Location)
                {
                    action.DeleteObject();
                    web.Context.ExecuteQuery();
                }
            }

            // leave as we're just removing the custom action
            if (customAction.Remove)
                return false;

            // add a new custom action
            var newAction = existingActions.Add();
            newAction.Description = customAction.Description;
            newAction.Location = customAction.Location;
            if (customAction.Location == JavaScriptExtensions.SCRIPT_LOCATION)
            {
                newAction.ScriptBlock = customAction.ScriptBlock;
            }
            else
            {
                newAction.Sequence = customAction.Sequence;
                newAction.Url = customAction.Url;
                newAction.Group = customAction.Group;
                newAction.Title = customAction.Title;
                newAction.ImageUrl = customAction.ImageUrl;
                if (customAction.Rights != null)
                {
                    newAction.Rights = customAction.Rights;
                }
            }
            newAction.Update();
            web.Context.Load(web, w => w.UserCustomActions);
            web.Context.ExecuteQuery();
            
            return true;
        }
        #endregion
    }
}
