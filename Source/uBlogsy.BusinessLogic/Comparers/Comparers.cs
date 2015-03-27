namespace uBlogsy.BusinessLogic.Comparers
{
    using System;
    using System.Collections.Generic;
    using Umbraco.Core.Models;


    /// <summary>
    /// Comparer for post dates.
    /// </summary>
    public class PostDateComparer : IComparer<IContent>
    {
        public int Compare(IContent x, IContent y)
        {
            var d1 = x.GetValue<DateTime>("uBlogsyPostDate");
            var d2 = y.GetValue<DateTime>("uBlogsyPostDate");

            if (d1 < d2) { return -1; }
            if (d1 == d2) { return 0; }

            return 1;
        }
    }
}
