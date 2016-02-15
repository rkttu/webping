using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace ClouDeveloper.WebPing
{
    public static class ConfigurationAccessor
    {
        public static readonly TimeSpan MinimumHttpTimeout = TimeSpan.FromSeconds(30d);
        public static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromSeconds(100d);
        public static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(1d);
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(30d);
        public static readonly TimeSpan MinimumWaitTimeout = TimeSpan.FromSeconds(30d);
        public static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromMinutes(1d);

        private static readonly Regex SiteListRegex = new Regex(
            @"(?<verb>DELETE|GET|HEAD|OPTIONS|POST|PUT|TRACE)\s+(?<url>.+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex CustomHeaderPrefixRegex = new Regex(
            @"(?<prefix>^header):*(?<header>.+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IgnoreCertificationError
        {
            get
            {
                string value = ConfigurationManager.AppSettings["ignoreCertificationError"];
                Trace.TraceInformation("ignoreCertificationError config: {0}", value);

                bool result = String.Equals(value, Boolean.TrueString, StringComparison.OrdinalIgnoreCase);
                Trace.TraceInformation("ignoreCertificationError value: {0}", value);

                return result;
            }
        }

        public static bool ChangeCurrentDirectoryToExeFilePath
        {
            get
            {
                string value = ConfigurationManager.AppSettings["changeCurrentDirectoryToExeFilePath"];
                Trace.TraceInformation("changeCurrentDirectoryToExeFilePath config: {0}", value);

                bool result = String.Equals(value, Boolean.TrueString, StringComparison.OrdinalIgnoreCase);
                Trace.TraceInformation("changeCurrentDirectoryToExeFilePath value: {0}", value);

                return result;
            }
        }

        public static string SiteListFilePath
        {
            get
            {
                string value = ConfigurationManager.AppSettings["siteListFilePath"];
                Trace.TraceInformation("siteListFilePath config: {0}", value);

                string result = Path.GetFullPath(value);
                Trace.TraceInformation("Raw SiteListFilePath property: {0}", result);

                result = Environment.ExpandEnvironmentVariables(result);
                Trace.TraceInformation("Expanded SiteListFilePath property: {0}", result);

                return result;
            }
        }

        public static TimeSpan HttpTimeout
        {
            get
            {
                TimeSpan temp = DefaultHttpTimeout;
                string value = ConfigurationManager.AppSettings["httpTimeoutTimeSpan"];
                Trace.TraceInformation("httpTimeoutTimeSpan config: {0}", value);

                bool parseResult = TimeSpan.TryParse(value, out temp);

                if (!parseResult)
                {
                    Trace.TraceWarning("Cannot parse the time span value; Using DefaultHttpTimeout value {0} instead", DefaultHttpTimeout);
                    return DefaultHttpTimeout;
                }
                else
                {
                    if (MinimumHttpTimeout > temp)
                    {
                        Trace.TraceWarning("Specified time span amount is too small; Using MinimumHttpTimeout value {0} instead", MinimumHttpTimeout);
                        temp = MinimumHttpTimeout;
                    }

                    return temp;
                }
            }
        }

        public static TimeSpan IntervalTimeSpan
        {
            get
            {
                TimeSpan temp = DefaultInterval;
                string value = ConfigurationManager.AppSettings["intervalTimeSpan"];
                Trace.TraceInformation("intervalTimeSpan config: {0}", value);

                bool parseResult = TimeSpan.TryParse(value, out temp);

                if (!parseResult)
                {
                    Trace.TraceWarning("Cannot parse the time span value; Using DefaultInterval value {0} instead", DefaultInterval);
                    return DefaultInterval;
                }
                else
                {
                    if (MinimumInterval > temp)
                    {
                        Trace.TraceWarning("Specified time span amount is too small; Using MinimumInterval value {0} instead", MinimumInterval);
                        temp = MinimumInterval;
                    }

                    return temp;
                }
            }
        }

        public static TimeSpan WaitTimeout
        {
            get
            {
                TimeSpan temp = DefaultWaitTimeout;
                string value = ConfigurationManager.AppSettings["waitTimeoutTimeSpan"];
                Trace.TraceInformation("waitTimeoutTimeSpan config: {0}", value);

                bool parseResult = TimeSpan.TryParse(value, out temp);

                if (!parseResult)
                {
                    Trace.TraceWarning("Cannot parse the time span value; Using DefaultWaitTimeout value {0} instead", DefaultWaitTimeout);
                    return DefaultWaitTimeout;
                }
                else
                {
                    if (MinimumWaitTimeout > temp)
                    {
                        Trace.TraceWarning("Specified time span amount is too small; Using MinimumWaitTimeout value {0} instead", MinimumWaitTimeout);
                        temp = MinimumWaitTimeout;
                    }

                    return temp;
                }
            }
        }

        public static IEnumerable<SiteListItem> GetSiteListFromSiteListFilePath()
        {
            List<SiteListItem> items = new List<SiteListItem>();

            if (!File.Exists(SiteListFilePath))
                return items.AsReadOnly();

            string[] lines = null;

            try { lines = File.ReadAllLines(SiteListFilePath, Encoding.UTF8); }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }

            if (lines == null || lines.Length < 1)
                return items.AsReadOnly();

            PropertyInfo[] staticProperties = typeof(HttpMethod)
                .GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Where(x => x.PropertyType.Equals(typeof(HttpMethod)))
                .ToArray();

            foreach (string eachLine in lines)
            {
                if (String.IsNullOrWhiteSpace(eachLine))
                    continue;
                if (eachLine.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                    continue;

                HttpMethod method = null;
                Uri url = null;

                if (eachLine.StartsWith(Uri.UriSchemeHttps) ||
                    eachLine.StartsWith(Uri.UriSchemeHttp))
                {
                    method = HttpMethod.Get;
                    bool result = Uri.TryCreate(eachLine, UriKind.Absolute, out url);
                    if (!result)
                    {
                        Trace.TraceWarning("Cannot parse the URI: {0}", eachLine);
                        continue;
                    }
                }
                else
                {
                    Match match = SiteListRegex.Match(eachLine);
                    if (!match.Success)
                    {
                        Trace.TraceWarning("Invalid syntax: {0}", eachLine);
                        continue;
                    }

                    string temp = match.Groups["verb"].Value;
                    
                    switch (temp.ToUpperInvariant().Trim())
                    {
                        case "DELETE":
                            method = HttpMethod.Delete;
                            break;
                        case "GET":
                            method = HttpMethod.Get;
                            break;
                        case "HEAD":
                            method = HttpMethod.Head;
                            break;
                        case "OPTIONS":
                            method = HttpMethod.Options;
                            break;
                        case "POST":
                            method = HttpMethod.Post;
                            break;
                        case "PUT":
                            method = HttpMethod.Put;
                            break;
                        case "TRACE":
                            method = HttpMethod.Trace;
                            break;
                        default:
                            Trace.TraceWarning("Unsupported verb: {0}", temp);
                            continue;
                    }
                    temp = match.Groups["url"].Value;
                    bool result = Uri.TryCreate(temp, UriKind.Absolute, out url);
                    if (!result)
                    {
                        Trace.TraceWarning("Cannot parse the URI: {0}", temp);
                        continue;
                    }
                }

                items.Add(new SiteListItem(method, url));
            }
            return items.AsReadOnly();
        }

        public static IEnumerable<KeyValuePair<string, string>> GetCustomHeaders()
        {
            List<KeyValuePair<string, string>> items = new List<KeyValuePair<string, string>>();

            foreach (string eachKey in ConfigurationManager.AppSettings.AllKeys)
            {
                Match m = CustomHeaderPrefixRegex.Match(eachKey);

                if (!m.Success)
                    continue;

                string headerName = m.Groups["header"].Value;
                string headerValue = ConfigurationManager.AppSettings[eachKey];
                items.Add(new KeyValuePair<string, string>(headerName, headerValue));
            }

            return items.AsReadOnly();
        }
    }
}
