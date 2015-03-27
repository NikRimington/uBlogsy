using System;
using uBlogsy.Common.Helpers;

namespace uBlogsy.BusinessLogic
{
    using Umbraco.Core.Models;
    using Umbraco.Web;

    using uHelpsy.Helpers;

    interface IuBlogsyService
    {
        string GetValueFromLanding(IPublishedContent node, string propertyAlias);

        string GetValueFromAncestor(IPublishedContent node, string ancestorAlias, string propertyAlias);

        IPublishedContent GetLanding(IPublishedContent node);

        IPublishedContent GetSiteRoot(IPublishedContent node, string rootNodeTypeAlias);
    }

    public class DataService : IuBlogsyService
    {
        #region Singleton

        protected static volatile DataService m_Instance = new DataService();
        protected static object syncRoot = new Object();

        protected DataService() { }

        public static DataService Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (syncRoot)
                    {
                        if (m_Instance == null)
                            m_Instance = new DataService();
                    }
                }

                return m_Instance;
            }
        }

        #endregion




        /// <summary>
        /// Returns a value from the landing node.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="propertyAlias"></param>
        /// <returns></returns>
        public string GetValueFromLanding(IPublishedContent node, string propertyAlias)
        {
            var landing = GetLanding(node);

            return !landing.GetProperty(propertyAlias).HasValue ? string.Empty : landing.GetProperty(propertyAlias).Value.ToString();
        }





        /// <summary>
        /// Returns a value from the ancestor specified by ancestorAlias.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="ancestorAlias"></param>
        /// <param name="propertyAlias"></param>
        /// <returns></returns>
        public string GetValueFromAncestor(IPublishedContent node, string ancestorAlias, string propertyAlias)
        {
            string cacheKey = "uBlogsy_GetValueFromAncestor_" + ancestorAlias;

            var root = CacheHelper.GetFromRequestCache(cacheKey) as IPublishedContent;
            if (root == null)
            {
                root = node.AncestorOrSelf(ancestorAlias);
                CacheHelper.AddToRequestCache(cacheKey, root);
            }
            
            return root.GetProperty(propertyAlias).Value.ToString();
        }




        /// <summary>
        /// Gets landing node, caches result.
        /// </summary>
        /// <param name="node">A node which is a descendant of landing.</param>
        /// <returns></returns>
        public IPublishedContent GetLanding(IPublishedContent node)
        {
            string cacheKey = "GetLanding_uBlogsyLanding";

            var cached = CacheHelper.GetFromRequestCache(cacheKey) as IPublishedContent;
            if (cached != null)
            {
                return cached;
            }

            var landing = node.AncestorOrSelf("uBlogsyLanding");

            // cache the result
            CacheHelper.AddToRequestCache(cacheKey, landing);

            return landing;
        }





        /// <summary>
        /// Gets landing node, caches result.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IPublishedContent GetSiteRoot(IPublishedContent node, string rootNodeTypeAlias)
        {
            string cacheKey = "GetSiteRoot_uBlogsySiteRoot";
            string noBlogsySiteRootcacheKey = "GetSiteRoot_No_uBlogsySiteRoot";

            // this is all a little bit hacky...

            // try to get the "no site root result" from cache
            var cachedNoSiteRoot = CacheHelper.GetFromRequestCache(noBlogsySiteRootcacheKey) as string;
            if (!string.IsNullOrEmpty(cachedNoSiteRoot) && cachedNoSiteRoot == noBlogsySiteRootcacheKey)
            {
                // we've already cached the fact that there is no root.
                return null;
            }
                
            // try to get the siteRoot from cache
            var cached = CacheHelper.GetFromRequestCache(cacheKey) as IPublishedContent;
            if (cached != null)
            {
                return cached;
            }

            // try to get the site root
            var root = node.AncestorOrSelf(rootNodeTypeAlias);

            // uBlogsySiteRoot was not found, so just return null
            if (root == null)
            {
                // cache the fact that there is no site root
                CacheHelper.AddToRequestCache(noBlogsySiteRootcacheKey, noBlogsySiteRootcacheKey);
                return null;
            }

            // cache the result
            CacheHelper.AddToRequestCache(cacheKey, root);

            return root;
        }
    }
}
