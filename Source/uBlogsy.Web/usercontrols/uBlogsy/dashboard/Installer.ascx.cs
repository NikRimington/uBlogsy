namespace uBlogsy.Web.usercontrols.uBlogsy.Dashboard
{
    using System;
    using System.Threading;
    using System.Web;
    using System.Web.UI;


    public partial class Installer : UserControl
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            // in umbraco 7 we get this exception:
            // Type is not resolved for member 'Umbraco.Core.Security.UmbracoBackOfficeIdentity,Umbraco.Core, Version=1.0.5095.27251, Culture=neutral, PublicKeyToken=null'.
            // we don't care, since all we want is an app restart
            try
            {
                HttpRuntime.UnloadAppDomain();
            }
            catch
            {
            }
        }


        protected void Install_Click(object sender, EventArgs e)
        {            
            try
            {
                // in umbraco 7 we have to do this twice to get umbraco to pick up utagsy :/
                HttpRuntime.UnloadAppDomain();
            }
            catch { }
            finally
            {
                MvInstall.ActiveViewIndex = 1;
                //Response.Redirect(Request.Url.AbsolutePath, true);
            }
        }
    }
}