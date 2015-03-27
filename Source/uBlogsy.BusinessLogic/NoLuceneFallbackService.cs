namespace uBlogsy.BusinessLogic
{
    using System.Text.RegularExpressions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Examine;

    using uBlogsy.Common.Helpers;

    using Umbraco.Core.Models;
    using Umbraco.Web;
    using uBlogsy.BusinessLogic.Comparers;

    using uHelpsy.Helpers;
    using uHelpsy.Extensions;

    internal interface INoLuceneFallbackService
    {
        IEnumerable<IPublishedContent> GetPosts(int postId, string tag, string label, string author, string searchTerm, string commenter, string year, string month, out int postCount);
        IEnumerable<IPublishedContent> GetPosts(int nodeId);
        IEnumerable<IPublishedContent> GetComments(int nodeId, bool getAll);

        IEnumerable<IPublishedContent> GetAuthors(int postId, bool getAll);
    }


    /// <summary>
    /// This class is contains methods which generally take in a IPublishedContent to do an operation/search on.
    /// </summary>
    public class NoLuceneFallbackService : INoLuceneFallbackService
    {
        #region Singleton

        protected static volatile NoLuceneFallbackService m_Instance = new NoLuceneFallbackService();
        protected static object syncRoot = new Object();

        protected NoLuceneFallbackService() { }

        public static NoLuceneFallbackService Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (syncRoot)
                    {
                        if (m_Instance == null)
                            m_Instance = new NoLuceneFallbackService();
                    }
                }

                return m_Instance;
            }
        }

        #endregion






        /// <summary>
        /// Gets posts which have a related tag or label
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="sorted"></param>
        /// <returns></returns>
        public IEnumerable<IPublishedContent> GetRelatedPosts(int postId, string itemAlias, int matchCount)
        {
            // get all posts
            IEnumerable<IPublishedContent> posts = GetPosts(postId);

            List<IPublishedContent> nodes;
            if (!string.IsNullOrEmpty(itemAlias))
            {
                nodes = GetRelatedPosts(postId, itemAlias, posts, matchCount).ToList();
            }
            else
            {
                // get both tags and labels
                IEnumerable<IPublishedContent> relatedByTags = GetRelatedPosts(postId, "uBlogsyPostTags", posts, matchCount);
                IEnumerable<IPublishedContent> relatedByLabels = GetRelatedPosts(postId, "uBlogsyPostLabels", posts, matchCount);

                nodes = new List<IPublishedContent>();
                nodes.AddRange(relatedByTags);
                nodes.AddRange(relatedByLabels);
            }

            // get distinct, and order by date
            return nodes.Distinct(new IPublishedContentNodeEqualityComparer());

        }







        /// <summary>
        /// Gets posts by tag, label, author, or all posts.
        /// </summary>
        /// <param name="nodeId"> </param>
        /// <param name="tag"></param>
        /// <param name="label"></param>
        /// <param name="author"></param>
        /// <param name="searchTerm"> </param>
        /// <param name="commenter"> </param>
        /// <param name="pageNo"> </param>
        /// <param name="itemsPerPage"> </param>
        /// <returns></returns>
        public IEnumerable<IPublishedContent> GetPosts(int nodeId, string tag, string label, string author, string searchTerm, string commenter, string year, string month, out int postCount)
        {
            // get entire list of posts
            IEnumerable<IPublishedContent> postList = GetPosts(nodeId).Where(x => x.GetProperty("umbracoNaviHide").Value != "1");

            // filter by year
            postList = FilterPostsByYear(year, postList);

            // filter by month
            postList = FilterPostsByMonth(month, postList);


            // filter by tag
            if (!string.IsNullOrEmpty(tag))
            {
                var tags = tag.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                foreach (var t in tags)
                {
                    postList = GetPostsWithPropertyValue(t, "uBlogsyPostTags", postList);
                }
            }

            // now filter by label
            if (!string.IsNullOrEmpty(label))
            {
                var labels = label.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                foreach (var c in labels)
                {
                    postList = GetPostsWithPropertyValue(c, "uBlogsyPostLabels", "uBlogsyLabelName", postList);
                }
            }

            // now filter by author
            if (!string.IsNullOrEmpty(author))
            {
                var authors = author.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                foreach (var a in authors)
                {
                    postList = GetPostsWithPropertyValue(a, "uBlogsyPostAuthor", "uBlogsyAuthorName", postList);
                }
            }

            // now filter by search term
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // do search on everything!
                postList = DoSearch(nodeId, searchTerm, postList);
            }

            // now filter by commenter
            if (!string.IsNullOrEmpty(commenter))
            {
                postList = GetPostsByCommenter(commenter, postList).ToList();
            }

            // sort and return
            var sorted = postList
                            .Where(x => x.DocumentTypeAlias == "uBlogsyPost")
                            .Distinct(new IPublishedContentNodeEqualityComparer())
                            .OrderByDescending(x => x.GetProperty("uBlogsyPostDate").Value);

            postCount = sorted.Count();

            return sorted;
        }




        /// <summary>
        /// Gets comments.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="getAll"></param>
        /// <returns></returns>
        public IEnumerable<IPublishedContent> GetComments(int nodeId, bool getAll)
        {
            List<IPublishedContent> comments;

            if (getAll)
            {
                // get posts
                var posts = GetPosts(nodeId);
                comments = new List<IPublishedContent>();

                // get comments in posts
                foreach (var post in posts)
                {
                    var postComments = post.DescendantsOrSelf("uBlogsyComment");
                    comments.AddRange(postComments);
                }
            }
            else
            {
                comments = IPublishedContentHelper.GetNode(nodeId).DescendantsOrSelf("uBlogsyComment").ToList();
            }

            return comments.OrderByDescending(x => x.GetPropertyValue("uBlogsyCommentDate"));
        }






        /// <summary>
        /// Gets posts where commenter == commenter.
        /// </summary>
        /// <param name="commenter"></param>
        /// <param name="postList"></param>
        /// <returns></returns>
        protected IEnumerable<IPublishedContent> GetPostsByCommenter(string commenter, IEnumerable<IPublishedContent> postList)
        {
            var posts = new List<IPublishedContent>();
            foreach (var post in postList)
            {
                var foundCommenter = post.Descendants("uBlogsyComment").Any(x => x.Name.ToLower() == commenter.ToLower());

                if (foundCommenter)
                {
                    posts.Add(post);
                }
            }
            return posts;
        }





        /// <summary>
        /// Returns posts which have a property with the given alias equal to the given item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="alias"></param>
        /// <param name="sorted"></param>
        /// <returns></returns>
        protected List<IPublishedContent> GetPostsWithPropertyValue(string item, string alias, string compareAlias, IEnumerable<IPublishedContent> sorted)
        {
            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
            var nodes = new List<IPublishedContent>();
            foreach (var n in sorted)
            {
                // get items 
                var itemList = new List<IPublishedContent>();
                var v = n.GetProperty(alias).Value.ToString()
                            .ToLower()
                            .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim()).Select(x => umbracoHelper.TypedContent(x))
                            .Where(x => x != null);

                itemList.AddRange(v);

                if (itemList.Any(x => x.GetPropertyValue<string>(compareAlias).ToLower() == item.ToLower()))
                {
                    nodes.Add(n);
                }
            }

            return nodes;
        }




        /// <summary>
        /// Returns posts which have a property with the given alias equal to the given item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="alias"></param>
        /// <param name="sorted"></param>
        /// <returns></returns>
        protected List<IPublishedContent> GetPostsWithPropertyValue(string item, string alias, IEnumerable<IPublishedContent> sorted)
        {
            var nodes = new List<IPublishedContent>();
            foreach (var n in sorted)
            {
                // get items 
                var itemList = new List<string>();
                var v = n.GetProperty(alias).Value.ToString()
                            .ToLower()
                            .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim());

                itemList.AddRange(v);

                if (itemList.Contains(item.ToLower()))
                {
                    nodes.Add(n);
                }
            }

            return nodes;
        }






        /// <summary>
        /// Returns all the posts.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <returns></returns>
        public IEnumerable<IPublishedContent> GetPosts(int nodeId)
        {
            string cacheKey = "NoLuceneFallBack_GetPosts_uBlogsyPosts";

            var cached = CacheHelper.GetFromRequestCache(cacheKey) as IEnumerable<IPublishedContent>;
            if (cached != null) { return cached; }

            var landing = DataService.Instance.GetLanding(IPublishedContentHelper.GetNode(nodeId));

            IEnumerable<IPublishedContent> nodes = landing.DescendentsOrSelf("uBlogsyPost", new[] { "uBlogsyContainerPage", "uBlogsyPage", "uBlogsyContainerComment", "uBlogsyComment" }).OrderByDescending(x => x.GetProperty("uBlogsyPostDate").Value);

            // cache the result
            CacheHelper.AddToRequestCache(cacheKey, nodes);

            return nodes;
        }








        /// <summary>
        /// Gets posts related by the value of a property.
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="itemAlias"></param>
        /// <param name="posts"></param>
        /// <param name="matchCount"></param>
        /// <returns></returns>
        protected IEnumerable<IPublishedContent> GetRelatedPosts(int postId, string itemAlias, IEnumerable<IPublishedContent> posts, int matchCount)
        {
            IPublishedContent current = IPublishedContentHelper.GetNode(postId);

            // get this page's items to compare
            List<string> currentItems = current.GetProperty(itemAlias).Value.ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            List<IPublishedContent> nodes = new List<IPublishedContent>();

            foreach (IPublishedContent n in posts)
            {
                if (n.Id != current.Id)
                {
                    // get items as string array
                    List<string> items = n.GetProperty(itemAlias).Value.ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

                    // check if currentItems and items have at least matchCount in common
                    bool intersects = CollectionHelper.HashSetIntersects(currentItems, items, true, matchCount);

                    if (intersects)
                    {
                        nodes.Add(n);
                    }
                }
            }
            return nodes;
        }






        /// <summary>
        /// Performs search on the given search term.
        /// </summary>
        /// <param name="searchTerm"></param>
        /// <returns></returns>
        protected IEnumerable<IPublishedContent> DoSearch(int nodeId, string searchTerm, IEnumerable<IPublishedContent> sorted)
        {
            List<IPublishedContent> nodes = new List<IPublishedContent>();

            // remove multiple spaces
            string cleanedSearchTerm = Regex.Replace(searchTerm, "\\s+", " ");

            // search using examine
            IEnumerable<SearchResult> results = ExamineManager.Instance.Search(cleanedSearchTerm, true);

            // add results to nodes list for returning
            foreach (var r in results)
            {
                nodes.Add(IPublishedContentHelper.GetNode(r.Id));
            }

            // split string when multiple words are typed
            IEnumerable<string> searchTerms = cleanedSearchTerm.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();

            // do examine search for each
            foreach (var term in searchTerms)
            {
                results = ExamineManager.Instance.Search(term, true);
                foreach (var r in results)
                {
                    if (!nodes.Any(x => x.Id == r.Id))
                    {
                        nodes.Add(IPublishedContentHelper.GetNode(r.Id));
                    }
                }

                nodes.AddRange(GetPostsWithPropertyValue(term, "uBlogsyPostTags", sorted));
                nodes.AddRange(GetPostsWithPropertyValue(term, "uBlogsyPostLabels", sorted));
                nodes.AddRange(GetPostsWithPropertyValue(term, "uBlogsyPostAuthor", sorted));
            }

            // get distinct and filter by path!
            var landing = DataService.Instance.GetLanding(IPublishedContentHelper.GetNode(nodeId));

            return nodes
                    .Distinct(new IPublishedContentNodeEqualityComparer())
                    .Where(x => x.Path.StartsWith(landing.Path));
        }






        /// <summary>
        /// Returns an IEnumberable of all authors
        /// </summary>
        /// <param name="postId"></param>
        /// <returns></returns>
        public IEnumerable<IPublishedContent> GetAuthors(int postId, bool getAll)
        {
            // get all authors
            var allAuthors = new List<string>();

            var posts = getAll ? GetPosts(postId) : new List<IPublishedContent>() { IPublishedContentHelper.GetNode(postId) };
            foreach (var n in posts)
            {
                allAuthors.AddRange(n.GetProperty("uBlogsyPostAuthor").Value.ToString().Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)); // take care of multiple author scenario
            }

            var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
            return allAuthors.Select(x => x.Trim()).Distinct().Select(x => umbracoHelper.TypedContent(x)).Where(x => x != null);
        }





        /// <summary>
        /// Gets articles for the given year.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="posts"> </param>
        /// <returns></returns>
        protected static IEnumerable<IPublishedContent> FilterPostsByYear(string year, IEnumerable<IPublishedContent> posts)
        {
            if (string.IsNullOrWhiteSpace(year)) { return posts; }

            var nodes = new List<IPublishedContent>();

            foreach (var p in posts)
            {
                var value = p.GetPropertyValue<string>("uBlogsyPostDate");
                DateTime date;
                if (DateTime.TryParse(value, out date))
                {
                    // add node if year matches, or if we are getting "other" years
                    if (date.Year.ToString() == year)
                    {
                        nodes.Add(p);
                    }
                }
            }

            return nodes;
        }


        /// <summary>
        /// Gets articles for the given month.
        /// </summary>
        /// <param name="month"></param>
        /// <param name="posts"> </param>
        /// <returns></returns>
        protected static IEnumerable<IPublishedContent> FilterPostsByMonth(string month, IEnumerable<IPublishedContent> posts)
        {
            if (string.IsNullOrEmpty(month)) { return posts; }

            var nodes = new List<IPublishedContent>();
            foreach (var p in posts)
            {
                var value = p.GetPropertyValue<string>("uBlogsyPostDate");
                DateTime date;
                if (DateTime.TryParse(value, out date))
                {
                    if (date.Month.ToString() == month)
                    {
                        nodes.Add(p);
                    }
                }
            }

            return nodes;
        }

        public Dictionary<string, int> GetTags(int nodeId, bool getAll)
        {
            var tagIds = GetIdsFromCsvProperty(IPublishedContentHelper.GetNode(nodeId), "uBlogsyPostTags", getAll, false);

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
        /// Gets labels.
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="getAll"></param>
        /// <returns></returns>
        public IEnumerable<IPublishedContent> GetLabels(int nodeId, bool getAll)
        {
            var labelIds = GetIdsFromCsvProperty(IPublishedContentHelper.GetNode(nodeId), "uBlogsyPostLabels", getAll, true);

            return labelIds.ToIPublishedContent(true);
        }



        /// <summary>
        /// Gets ids.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="propertyAlias"></param>
        /// <param name="getAll"></param>
        /// <param name="distinct"></param>
        /// <returns></returns>
        private IEnumerable<string> GetIdsFromCsvProperty(IPublishedContent node, string propertyAlias, bool getAll, bool distinct)
        {
            var nodeIds = new List<string>();

            if (getAll)
            {
                // use examine!
                var results = this.GetPosts(node.Id);

                foreach (var r in results)
                {
                    if (r.GetProperty(propertyAlias) == null) { continue; }

                    // take care of case where index is being rebuilt
                    nodeIds.AddRange(r.GetPropertyValue<string>(propertyAlias).Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                }

                if (distinct)
                {
                    nodeIds = nodeIds.Distinct().ToList();
                }
            }
            else
            {
                nodeIds = node.GetProperty(propertyAlias) == null
                              ? new List<string>()
                              : node.GetPropertyValue<string>(propertyAlias).Split(
                                  ",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return nodeIds;
        }
    }
}