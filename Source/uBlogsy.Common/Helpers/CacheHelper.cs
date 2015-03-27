using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;

namespace uBlogsy.Common.Helpers
{
    public class CacheHelper
    {
        /// <summary>
        /// Adds to HttpContext.Current.Items.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
		public static void AddToRequestCache(string key, object value)
        {
            HttpContext.Current.Items.Add(key, value);
        }


        /// <summary>
        /// Gets from HttpContext.Current.Items
        /// </summary>
        /// <param name="key"></param>
        /// <returns>gets from HttpContext.Current.Items</returns>
        public static object GetFromRequestCache(string key)
        {
            return HttpContext.Current.Items[key];
        }

        /// <summary>
        /// Addds an item to cache.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timespan"></param>
        public static void AddToCache(string key, object value, TimeSpan timespan)
        {
            HttpRuntime.Cache.Insert(key,
                value,
                null,                      
                Cache.NoAbsoluteExpiration,
                timespan,                   
                CacheItemPriority.Normal,
                null
            );
        }

    }
}
