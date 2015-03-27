using System;
using System.Linq;
using System.Web.UI.WebControls;
using uBlogsy.BusinessLogic;

namespace uBlogsy.Web.usercontrols.uBlogsy.dashboard
{
    using Umbraco.Core.Services;
    using Umbraco.Web;

    public partial class CreatePost : System.Web.UI.UserControl
    {
        ContentService ContentService = (ContentService)UmbracoContext.Current.Application.Services.ContentService;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!IsPostBack)
            {
                InitBlogRoots();
            }
        }



        /// <summary>
        /// Sets drop down list with landing roots.
        /// </summary>
        private void InitBlogRoots()
        {
            // get roots
            var roots = ContentService.GetByLevel(1);

            foreach (var root in roots)
            {
                if (root.ContentType.Alias == "uBlogsyLanding")
                {
                    ddlRoots.Items.Add(new ListItem(root.Name, root.Id.ToString()));
                }
                else
                {
                    // get landings that may be in this root
                    var landings = ContentService.GetDescendants(root.Id).Where(x => x.ContentType.Alias == "uBlogsyLanding");

                    foreach (var landing in landings)
                    {
                        // add landing to ddl
                        ddlRoots.Items.Add(new ListItem(landing.Name, landing.Id.ToString()));
                    }
                }
            }

            ddlRoots.DataBind();
        }



        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            var landing = ContentService.GetById(int.Parse(ddlRoots.SelectedValue));

            // when there are multiple roots we need to pass in the root!
            var post = PostService.Instance.CreatePost(landing.Id, !string.IsNullOrWhiteSpace(TxtTitle.Text) ? TxtTitle.Text : "New Post");
            //Response.Redirect("~/umbraco/#/content/content/edit/" + post.Id, true);
            HfRedirectUrl.Value = "/umbraco/#/content/content/edit/" + post.Id;
            //return;
        }
    }
}