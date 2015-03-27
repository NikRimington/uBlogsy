namespace uBlogsy.BusinessLogic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Examine;
    using Examine.Providers;
    using Examine.SearchCriteria;

    using Umbraco.Core;

    using uHelpsy.Comparers;
    using uHelpsy.Extensions;
    using uHelpsy.Helpers;

    using Umbraco.Core.Models;
    using Umbraco.Web;

    using UmbracoExamine;

    using CacheHelper = uBlogsy.Common.Helpers.CacheHelper;

    internal interface IPostService
    {
        IContent CreatePost(int documentId, string title);

        IContent EnsureCorrectPostNodeName(IContent doc);
        IContent EnsureCorrectPostTitle(IContent doc);
        IContent EnsureNavigationTitle(IContent doc);

        IEnumerable<SearchResult> GetPosts(IPublishedContent node);

        IEnumerable<SearchResult> GetPosts(IPublishedContent node, string tag, string label, string author, string searchTerm, string commenter, string year, string month, string day, out int postCount);

        IPublishedContent GetNextPost(IPublishedContent current, string tag, string label, string author, string searchTerm, string commenter, string year, string month, string day);

        IPublishedContent GetPreviousPost(IPublishedContent current, string tag, string label, string author, string searchTerm, string commenter, string year, string month, string day);

        IEnumerable<SearchResult> GetRelatedPosts(IPublishedContent node, string itemAlias, int matchCount);

        IEnumerable<IPublishedContent> GetAuthors(IPublishedContent node, bool getAll);

        IEnumerable<IPublishedContent> GetLabels(IPublishedContent node, bool getAll);

        Dictionary<string, int> GetTags(IPublishedContent node, bool getAll);
    }


    /// <summary>
    /// This class is contains methods which generally take in a IPublishedContent to do an operation/search on.
    /// </summary>
    public class PostService : IPostService
    {
        #region Singleton

        protected static volatile PostService m_Instance = new PostService();
        protected static object syncRoot = new Object();

        protected PostService() { }

        public static PostService Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (syncRoot)
                    {
                        if (m_Instance == null)
                            m_Instance = new PostService();
                    }
                }

                return m_Instance;
            }
        }

        #endregion



        /// <summary>
        /// Creates a post and returns it.
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        public IContent CreatePost(int documentId, string title)
        {
            // create the node
            var content = IContentHelper.CreateContentNode(title, "uBlogsyPost", new Dictionary<string, object>(), documentId, false);

            // this is a hack because there is a bug in Umbraco 6 which means datefolders is not being fired.
            ApplicationContext.Current.Services.ContentService.Save(content);
            return content;
        }




        /// <summary>
        /// Ensures that the node name is the same as the post title
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public IContent EnsureCorrectPostNodeName(IContent doc)
        {
            var useTitleAsNodeName = IContentHelper.GetValueFromAncestor(doc, "uBlogsyLanding", "uBlogsyGeneralUseTitleAsNodeName");

            if (useTitleAsNodeName == "1")
            {
                var title = doc.GetValue<string>("uBlogsyContentTitle");
                if (!string.IsNullOrEmpty(title) && doc.Name != title)
                {
                    // ensure node name is same as title
                    doc.Name = title;
                    ApplicationContext.Current.Services.ContentService.Save(doc, 0, false);
                }
            }

            return doc;
        }


        /// <summary>
        /// Ensures that the node name is the same as the post title
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public IContent EnsureCorrectPostTitle(IContent doc)
        {
            var title = doc.GetValue<string>("uBlogsyContentTitle");
            if (string.IsNullOrEmpty(title))
            {
                // ensure node name is same as title
                doc.SetValue("uBlogsyContentTitle", doc.Name);
                ApplicationContext.Current.Services.ContentService.Save(doc, 0, false);
            }

            return doc;
        }


        /// <summary>
        /// Ensures that the node name is the same as the post title
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public IContent EnsureNavigationTitle(IContent doc)
        {
            var navTitle = doc.GetValue<string>("uBlogsyNavigationTitle");
            var title = doc.GetValue<string>("uBlogsyContentTitle");

            if (string.IsNullOrEmpty(navTitle) && !string.IsNullOrEmpty(title))
            {
                navTitle = title;
                // ensure node name is same as title
                doc.SetValue("uBlogsyNavigationTitle", navTitle);
                ApplicationContext.Current.Services.ContentService.Save(doc, 0, false);
            }

            return doc;
        }



        /// <summary>
        /// Returns all the posts.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IEnumerable<SearchResult> GetPosts(IPublishedContent node)
        {
            string cacheKey = "GetPosts_uBlogsyPosts";

            var cached = CacheHelper.GetFromRequestCache(cacheKey) as IEnumerable<SearchResult>;
            if (cached != null) { return cached; }

            var nodes = GetPostsAsSearchResult(node);

            var sorted =
                nodes.Distinct(new ExamineSearchResultEqualityComparer()).OrderByDescending(
                    x => x.GetValue("uBlogsyPostDate"));
                //.ToIPublishedContent(true);

            // cache the result
            CacheHelper.AddToRequestCache(cacheKey, sorted);

            return sorted;
        }




        /// <summary>
        /// Gets posts by tag, label, author, or all posts.
        /// </summary>
        /// <param name="node"> </param>
        /// <param name="tag"></param>
        /// <param name="label"></param>
        /// <param name="author"></param>
        /// <param name="searchTerm"> </param>
        /// <param name="commenter"> </param>
        /// <returns></returns>
        public IEnumerable<SearchResult> GetPosts(IPublishedContent node, string tag, string label, string author, string searchTerm, string commenter, string year, string month, string day, out int postCount)
        {
            var searcher = ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"];
            var criteria = GetPostBaseCriteria(node, searcher);

            // add criteria for tags
            criteria = ExamineSearchHelper.AddSvCriteria(criteria, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableTags, tag, ",");

            // add criteria for labels
            criteria = ExamineSearchHelper.AddSvCriteria(criteria, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableLabels, label, ",");

            // add criteria for authors
            criteria = ExamineSearchHelper.AddSvCriteria(criteria, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableAuthor, author, ",");

            // add criteria for year
            criteria = ExamineSearchHelper.AddSvCriteria(criteria, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableYear, year, ",");

            // add criteria for month
            criteria = ExamineSearchHelper.AddSvCriteria(criteria, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableMonth, month, ",");

            criteria = ExamineSearchHelper.AddSvCriteria(criteria, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableDay, day, ",");

            // do search using InternalSearcher
            //var postList = new UmbracoHelper(UmbracoContext.Current).TypedSearch(criteria, searcher);

            IEnumerable<SearchResult> postList = searcher.Search(criteria).ToList();

            // now filter by commenter - does another examine search and filters results
            postList = GetPostsByCommenter(node, commenter, postList);

            // do search using term - does another examine search and combines results
            postList = GetPostsBySearchTerm(node, searchTerm, postList);
            
            // sort and return
            var sorted = postList
                            .Distinct(new ExamineSearchResultEqualityComparer())
                            .ToList()
                            .OrderByDescending(x => x.GetValue("uBlogsyPostDate"));

            postCount = sorted.Count();

            return sorted;
        }








        /// <summary>
        /// Gets the post immediately following the current one.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        public IPublishedContent GetNextPost(IPublishedContent current, string tag, string label, string author, string searchTerm, string commenter, string year, string month, string day)
        {
            int count;

            // get siblings
            var siblings = GetPosts(current, tag, label, author, searchTerm, commenter, year, month, day, out count);

            // get index of current
            var next = GetNext(siblings, current);

            return next != null ? next.ToIPublishedContent() : null;
        }






        /// <summary>
        /// Gets the post immediately preceding the current one.
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        public IPublishedContent GetPreviousPost(IPublishedContent current, string tag, string label, string author, string searchTerm, string commenter, string year, string month, string day)
        {
            int count;

            // get siblings
            var siblings = GetPosts(current, tag, label, author, searchTerm, commenter, year, month, day, out count);

            // get index of current
            var prev = GetNext(siblings.Reverse(), current);

            // return previous
            return prev != null ? prev.ToIPublishedContent() : null;
        }




        /// <summary>
        /// Gets posts which have a related tag or label
        /// </summary>
        /// <param name="node"></param>
        /// <param name="itemAlias"></param>
        /// <param name="matchCount"> </param>
        /// <returns></returns>
        public IEnumerable<SearchResult> GetRelatedPosts(IPublishedContent node, string itemAlias, int matchCount)
        {
            // get all posts
            var searcher = ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"];
            var criteria = GetPostBaseCriteria(node, searcher);
            var nodes = new List<SearchResult>();

            if (!string.IsNullOrEmpty(itemAlias))
            {
                var values = node.GetPropertyValue<string>(itemAlias);
                criteria = ExamineSearchHelper.AddSvCriteria(criteria, itemAlias == "uBlogsyPostTags" ? uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableTagIds : uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableLabelIds, values, ",");
                var res = searcher.Search(criteria);
                nodes.AddRange(res);
            }
            else
            {
                var tags = node.GetPropertyValue<string>("uBlogsyPostTags");
                var labels = node.GetPropertyValue<string>("uBlogsyPostLabels"); 

                // search by tags
                criteria = ExamineSearchHelper.AddSvCriteria(criteria, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableTagIds, tags, ",");
                var resultByTags = searcher.Search(criteria);

                // search by labels
                criteria = GetPostBaseCriteria(node, searcher);
                criteria = ExamineSearchHelper.AddSvCriteria(criteria, uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchableLabelIds, labels, ",");
                var resultByLabels = searcher.Search(criteria);

                // get both tags and labels
                nodes.AddRange(resultByTags);
                nodes.AddRange(resultByLabels);
            }

            // get distinct, and order by date
            return nodes.Distinct(new ExamineSearchResultEqualityComparer()).Where(x => x.Id != node.Id);
        }



        /// <summary>
        /// Returns an IEnumberable of all authors
        /// </summary>
        /// <param name="node"></param>
        /// <param name="getAll"> </param>
        /// <returns></returns>
        public IEnumerable<IPublishedContent> GetAuthors(IPublishedContent node, bool getAll)
        {
            var authors = GetIdsFromCsvProperty(node, "uBlogsyPostAuthor", getAll, true);

            if (!authors.Any())
            {
                // get default author from landing
                authors = DataService.Instance
                            .GetValueFromLanding(node, "uBlogsyGeneralDefaultAuthor")
                            .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }

            return authors.ToIPublishedContent(true);
        }



        /// <summary>
        /// Gets tags.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="getAll"> </param>
        /// <returns></returns>
        public Dictionary<string, int> GetTags(IPublishedContent node, bool getAll)
        {
            var tagIds = GetIdsFromCsvProperty(node, "uBlogsyPostTags", getAll, false);

            var allTags = tagIds
                            .ToIPublishedContent(true)
                            .Select(x => x.GetPropertyValue<string>("uTagsyTagName"))
                            .Where(x => !string.IsNullOrEmpty(x)).ToList();

            // create dictionary with tags and the number of times they are used
            var tagCloud = new Dictionary<string, int>();
            foreach (var tag in allTags.OrderBy(x => x))
            {
                if (tagCloud.ContainsKey(tag))
                {
                    tagCloud[tag]++;
                }
                else
                {
                    tagCloud.Add(tag, 1);
                }
            }

            return tagCloud;
        }



        /// <summary>
        /// Gets Labels.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="getAll"></param>
        /// <returns></returns>
        public IEnumerable<IPublishedContent> GetLabels(IPublishedContent node, bool getAll)
        {
            var labelIds = GetIdsFromCsvProperty(node, "uBlogsyPostLabels", getAll, true);

            return labelIds.ToIPublishedContent(true);
        }



        /// <summary>
        /// Gets ids from propertyAlias and returns nodes.
        /// When getAll is true, gets all posts from lucene, then iterates over all, gets ids from propertyAlias, and returns ids.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="propertyAlias"></param>
        /// <param name="getAll"></param>
        /// <param name="distinct"> </param>
        /// <returns></returns>
        public IEnumerable<string> GetIdsFromCsvProperty(IPublishedContent node, string propertyAlias, bool getAll, bool distinct)
        {
            var nodeIds = new List<string>();

            if (getAll)
            {
                // use examine!
                var results = this.GetPostsAsSearchResult(node);

                foreach (var r in results)
                {
                    if (r.Fields.ContainsKey(propertyAlias))
                    {
                        // take care of case where index is being rebuilt
                        nodeIds.AddRange(r.Fields[propertyAlias].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                    }
                }

                if (distinct)
                {
                    nodeIds = nodeIds.Distinct().ToList();
                }
            }
            else
            {
                if (node.GetProperty(propertyAlias) != null)
                {
                    // get from given node
                    nodeIds = node.GetPropertyValue<string>(propertyAlias, string.Empty)
                                        .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }

            return nodeIds;
        }




        /// <summary>
        /// Creates a base search criteria for getting posts.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="searcher"></param>
        /// <returns></returns>
        protected ISearchCriteria GetPostBaseCriteria(IPublishedContent node, BaseSearchProvider searcher)
        {
            var landing = DataService.Instance.GetLanding(node);

            var criteria = searcher.CreateSearchCriteria(IndexTypes.Content);
            criteria.Field("nodeTypeAlias", "uBlogsyPost")
                    .And()
                    .Field(uBlogsy.BusinessLogic.Constants.Examine.uBlogsySearchablePath, landing.Id.ToString())
                    .Not()
                    .Field("umbracoNaviHide", "1");

            return criteria;
        }





        /// <summary>
        /// Gets all posts as examine search results.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected IEnumerable<SearchResult> GetPostsAsSearchResult(IPublishedContent node)
        {
            var searcher = ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"];

            // Get posts as Examine results
            var criteria = this.GetPostBaseCriteria(node, searcher);
            var results = searcher.Search(criteria);
            return results;
        }



        /// <summary>
        /// Performs search on the given search term. - This is not the best search.
        /// </summary>
        /// <param name="node"> </param>
        /// <param name="searchTerm"></param>
        /// <param name="posts"> </param>
        /// <returns></returns>
        protected IEnumerable<SearchResult> GetPostsBySearchTerm(IPublishedContent node, string searchTerm, IEnumerable<SearchResult> posts)
        {
            if (string.IsNullOrEmpty(searchTerm)) { return posts; }

            // remove multiple spaces
            var cleanedSearchTerm = Regex.Replace(searchTerm, "\\s+", " ");

            // search using examine
            var results = ExamineManager.Instance.Search(cleanedSearchTerm, true);

            // add results to nodes list for returning
            var res = results.ToList();

            // split string when multiple words are typed
            var searchTerms = cleanedSearchTerm.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            // do examine search for each
            foreach (var term in searchTerms)
            {
                results = ExamineManager.Instance.Search(term, true);
                foreach (var r in results)
                {
                    if (!res.Any(x => x.Id == r.Id)) { res.Add(r); }
                }
            }

            // get distinct and filter by path! - TODO: we can do this with search criteria
            var landing = DataService.Instance.GetLanding(node);

            return res
                    .Where(x => posts.Any(y => x.Id == y.Id))
                    .Where(x => x.GetValue("path").StartsWith(landing.Path));
        }





        /// <summary>
        /// Gets posts where commenter == commenter.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="commenter"></param>
        /// <param name="posts"></param>
        /// <returns>
        /// </returns>
        protected IEnumerable<SearchResult> GetPostsByCommenter(IPublishedContent node, string commenter, IEnumerable<SearchResult> posts)
        {
            if (string.IsNullOrEmpty(commenter)) { return posts; }

            // use lucene to get all nodes in this tree
            var searcher = ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"];
            var landing = DataService.Instance.GetLanding(node);

            var criteria = searcher.CreateSearchCriteria(IndexTypes.Content);
            criteria.Field("nodeTypeAlias", "uCommentsyComment");
            criteria.Field(uBlogsy.BusinessLogic.Constants.Examine.uCommentsySearchablePath, landing.Id.ToString());
            criteria.Field(uBlogsy.BusinessLogic.Constants.Examine.NodeName, commenter); // search by node name!

            // do search
            var comments = searcher.Search(criteria);

            // get posts which are an ancestor of the found comments - might be expensive?
            return posts.Where(p => comments.Any(c => c.Fields["path"].StartsWith(p.Fields["path"])));
        }





        /// <summary>
        /// Gets next
        /// </summary>
        /// <param name="siblings"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        protected static SearchResult GetNext(IEnumerable<SearchResult> siblings, IPublishedContent current)
        {
            bool found = false;
            foreach (var s in siblings)
            {
                if (found)
                {
                    return s;
                }

                if (s.Id == current.Id)
                {
                    found = true;
                }
            }

            return null; // some crazy error!
        }






        /// <summary>
        /// Gets the index of the current post in the list of siblings.
        /// </summary>
        /// <param name="siblings"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        protected int GetIndexOf(List<SearchResult> siblings, IPublishedContent current)
        {
            for (int i = 0; i < siblings.Count; i++)
            {
                if (siblings[i].Id == current.Id)
                {
                    return i;
                }
            }

            return -1; // some crazy error!
        }
    }
}