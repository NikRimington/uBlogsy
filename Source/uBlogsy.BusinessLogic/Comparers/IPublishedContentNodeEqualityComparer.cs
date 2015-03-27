namespace uBlogsy.BusinessLogic.Comparers
{
    using System.Collections.Generic;
    using Umbraco.Core.Models;

    public class IPublishedContentNodeEqualityComparer : IEqualityComparer<IPublishedContent>
    {
        public bool Equals(IPublishedContent x, IPublishedContent y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(IPublishedContent obj)
        {
            return obj.ToString().ToLower().GetHashCode();
        }
    }
}
