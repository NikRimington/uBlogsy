namespace uBlogsy.Common.Helpers
{
    using umbraco;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class GravatarHelper
    {
        private const string GravatarBaseURL = "http://www.gravatar.com/avatar/";

        /// <summary>
        /// Gets url for gravatar image.
        /// </summary>
        /// <param name="email">Email of user.</param>
        /// <param name="size">Size of image in pixels.</param>
        /// <returns></returns>
        public static string GetUrl(string email, int size)
        {
            var hashedEmail = library.md5(email?? string.Empty);
            var gravatarUrl = string.Format("{0}{1}?size={2}", GravatarBaseURL, hashedEmail, size);
            return gravatarUrl;
        }
    }
}
