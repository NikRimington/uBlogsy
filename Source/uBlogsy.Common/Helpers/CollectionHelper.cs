using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uBlogsy.Common.Helpers
{
    public class CollectionHelper
    {

        #region HashSetIntersects

        /// <summary>
        /// Does intersect using hash sets.
        /// </summary>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <param name="toLower"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        public static bool HashSetIntersects(IEnumerable<string> list1, IEnumerable<string> list2, bool toLower, int factor)
        {
            var set1 = new HashSet<string>(list1);
            var set2 = new HashSet<string>(list2);

            if (toLower)
            {
                return set1.Select(x => x.ToLower()).Intersect(set2.Select(x => x.ToLower())).Count() >= factor;
            }

            return set1.Intersect(set2).Count() >= factor;
        }

        #endregion
    }
}
