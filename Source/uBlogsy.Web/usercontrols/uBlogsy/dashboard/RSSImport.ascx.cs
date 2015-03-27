using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using uBlogsy.BusinessLogic;
using uBlogsy.Common.Extensions;
using uBlogsy.Common.Helpers;

using umbraco.cms.businesslogic.web;

namespace uBlogsy.Web.usercontrols.uBlogsy.dashboard
{
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;
    using Umbraco.Web;

    using uHelpsy.Helpers;

    public partial class RSSImport : System.Web.UI.UserControl
    {
        IContentService ContentService = UmbracoContext.Current.Application.Services.ContentService;



        protected void btnRssImport_Click(object sender, EventArgs e)
        {
            // should we make import a transaction?
            try
            {
                Import();
                mv.ActiveViewIndex = 1;
            }
            catch (Exception ex)
            {
                // display errors
                lblError.Text = ex.Message + "<br/><br/>StackTrace:<br/>" + ex.StackTrace;
                mv.ActiveViewIndex = 2;
            }
        }





        /// <summary>
        /// Performs import.
        /// Does call to get rss.
        /// Creates content nodes.
        /// </summary>
        protected void Import()
        {
            var reader = RssReader.CreateAndCache(txtRssUrl.Text, new TimeSpan(0, 1, 0));

            var root = ContentService.GetByLevel(1).FirstOrDefault(x => x.ContentType.Alias == "uBlogsySiteRoot");

            // get landing
            var landing = IContentHelper.GetIContentByAlias(root, "uBlogsySiteRoot", "uBlogsyLanding");
            //landing = IContentHelper.EnsureNodeExists(-1, landing, "uBlogsyLanding", "Blog", true);

            // make landing title == reader.Title
            landing.SetValue("uBlogsyContentTitle", reader.Title);
            ContentService.SaveAndPublish(landing);

            var items = reader.Items.OrderBy(x => x.Date);
            foreach (var item in items)
            {
                // create post item under a year folder
                if (!PostExists(item, landing))
                {
                    CreatePost(item, landing.Id);
                }
            }
        }





        /// <summary>
        /// Returns true of a post exists with the same name and date
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parentFolder"></param>
        /// <returns></returns>
        private bool PostExists(RssItem item, IContent parentFolder)
        {
            var descendants = ContentService.GetDescendants(parentFolder.Id).Where(x => x.ContentType.Alias == "uBlogsyPost");
            foreach (var post in descendants)
            {
                if (post.Name.Flatten() == item.Title.Flatten() && post.GetValue<DateTime>("uBlogsyPostDate") == item.Date)
                {
                    return true;
                }
            }

            return false;
        }






        /// <summary>
        /// Creates a uBlogsyPost Document 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="parentId"></param>
        /// <returns></returns>
        private IContent CreatePost(RssItem item, int parentId)
        {
            // create post item
            var postDic = new Dictionary<string, object>()
            {
                {"uBlogsyPostDate", item.Date},
                {"uBlogsyPostAuthor", TxtAuthor.Text},
                {"uBlogsyContentBody", item.Description},
                {"uBlogsyContentTitle", item.Title}
            };

            return IContentHelper.CreateContentNode(item.Title, "uBlogsyPost", postDic, parentId, false);
        }
    }
}