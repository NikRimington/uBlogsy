namespace uBlogsy.BusinessLogic.EventHandlers
{
    using Umbraco.Core.Events;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;

    using Umbraco.Core;
    using Umbraco.Web;

    public class UmbracoNodeEventsForPosts : IApplicationEventHandler
    {
        public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
        }



        public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {

        }


        /// <summary>
        /// Wire up events.
        /// </summary>
        /// <param name="umbracoApplication"></param>
        /// <param name="applicationContext"></param>
        public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            if (applicationContext.IsConfigured && applicationContext.DatabaseContext.IsDatabaseConfigured)
            {
                ContentService.Saved += this.ContentService_Saved;
            }
        }



        /// <summary>
        /// Ensures that node name is the same as post title.
        /// </summary>
        void ContentService_Saved(IContentService sender, SaveEventArgs<IContent> e)
        {
            foreach (var entity in e.SavedEntities)
            {
                if (entity.ContentType.Alias == "uBlogsyPost" && entity.ParentId != -20)
                {
                    PostService.Instance.EnsureCorrectPostTitle(entity);
                    PostService.Instance.EnsureCorrectPostNodeName(entity);
                    PostService.Instance.EnsureNavigationTitle(entity);
                }
            }
        }
    }
}