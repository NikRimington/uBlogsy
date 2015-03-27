namespace uBlogsy.BusinessLogic.EventHandlers
{
    using System;
    using Examine;
    using Umbraco.Core;
    using uHelpsy.Helpers;

    public class UmbracoExamineEvents : IApplicationEventHandler
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
                //... do stuff since we are installed and configured, otherwise don't do stuff
                ExamineManager.Instance.IndexProviderCollection["ExternalIndexer"].GatheringNodeData += this.UmbracoExamineEvents_GatheringNodeData;
            }
        }



        /// <summary>
        /// Adds custom fields to internal index.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void UmbracoExamineEvents_GatheringNodeData(object sender, IndexingNodeDataEventArgs e)
        {
            if (e.Fields["nodeTypeAlias"] == "uBlogsyPost")
            {
                // add path
                e.Fields.Add(uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchablePath, e.Fields["path"].Replace(",", " "));

                // get value
                var date = ExamineIndexHelper.GetValueFromFieldOrProperty(e, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableMonth, "uBlogsyPostDate");


                // year
                e.Fields.Add(uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableYear, DateTime.Parse(date).Year.ToString());

                // month
                e.Fields.Add(uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableMonth, DateTime.Parse(date).Month.ToString());

                // day
                e.Fields.Add(uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableDay, DateTime.Parse(date).Day.ToString());


                // label 
                ExamineIndexHelper.AddIndexByPropertyInSelectedNodes(e, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableLabels, "uBlogsyPostLabels", "uBlogsyLabelName");
                ExamineIndexHelper.AddIdsFromCsvProperty(e, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableLabelIds, "uBlogsyPostLabels");

                // author name
                ExamineIndexHelper.AddIndexByPropertyInSelectedNodes(e, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableAuthor, "uBlogsyPostAuthor", "uBlogsyAuthorName");
                ExamineIndexHelper.AddIdsFromCsvProperty(e, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableAuthorIds, "uBlogsyPostAuthor");

                // tags
                ExamineIndexHelper.AddIndexByPropertyInSelectedNodes(e, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableTags, "uBlogsyPostTags", "uTagsyTagName");
                ExamineIndexHelper.AddIdsFromCsvProperty(e, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableTagIds, "uBlogsyPostTags");
            }
        }
    }
}