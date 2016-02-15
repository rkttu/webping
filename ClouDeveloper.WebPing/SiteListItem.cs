using System;
using System.Globalization;
using System.Net.Http;

namespace ClouDeveloper.WebPing
{
    public sealed class SiteListItem
    {
        public SiteListItem(HttpMethod method, Uri uri)
            : base()
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (!uri.IsAbsoluteUri)
                throw new ArgumentException("URI should be absolute.", "uri");

            if (!uri.Scheme.Equals(Uri.UriSchemeHttps) &&
                !uri.Scheme.Equals(Uri.UriSchemeHttp))
                throw new NotSupportedException("Only HTTPS and HTTP uri supported.");

            if (method == null)
                method = HttpMethod.Get;

            this.Method = method;
            this.Uri = uri;
        }

        public HttpMethod Method { get; private set; }
        public Uri Uri { get; private set; }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture,
                "{0} {1}", this.Method.ToString(), this.Uri.AbsoluteUri);
        }
    }
}
