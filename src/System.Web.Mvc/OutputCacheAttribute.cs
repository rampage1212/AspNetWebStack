﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc.Properties;
using System.Web.UI;

namespace System.Web.Mvc
{
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "Unsealed so that subclassed types can set properties in the default constructor.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class OutputCacheAttribute : ActionFilterAttribute, IExceptionFilter
    {
        private const string CacheKeyPrefix = "_MvcChildActionCache_";
        private static ObjectCache _childActionCache;
        private static object _childActionFilterFinishCallbackKey = new object();
        private OutputCacheParameters _cacheSettings = new OutputCacheParameters { VaryByParam = "*" };
        private Func<ObjectCache> _childActionCacheThunk = () => ChildActionCache;
        private bool _locationWasSet;
        private bool _noStoreWasSet;

        public OutputCacheAttribute()
        {
        }

        internal OutputCacheAttribute(ObjectCache childActionCache)
        {
            _childActionCacheThunk = () => childActionCache;
        }

        public string CacheProfile
        {
            get { return _cacheSettings.CacheProfile ?? String.Empty; }
            set { _cacheSettings.CacheProfile = value; }
        }

        internal OutputCacheParameters CacheSettings
        {
            get { return _cacheSettings; }
        }

        public static ObjectCache ChildActionCache
        {
            get { return _childActionCache ?? MemoryCache.Default; }
            set { _childActionCache = value; }
        }

        private ObjectCache ChildActionCacheInternal
        {
            get { return _childActionCacheThunk(); }
        }

        public int Duration
        {
            get { return _cacheSettings.Duration; }
            set { _cacheSettings.Duration = value; }
        }

        public OutputCacheLocation Location
        {
            get { return _cacheSettings.Location; }
            set
            {
                _cacheSettings.Location = value;
                _locationWasSet = true;
            }
        }

        public bool NoStore
        {
            get { return _cacheSettings.NoStore; }
            set
            {
                _cacheSettings.NoStore = value;
                _noStoreWasSet = true;
            }
        }

        public string SqlDependency
        {
            get { return _cacheSettings.SqlDependency ?? String.Empty; }
            set { _cacheSettings.SqlDependency = value; }
        }

        public string VaryByContentEncoding
        {
            get { return _cacheSettings.VaryByContentEncoding ?? String.Empty; }
            set { _cacheSettings.VaryByContentEncoding = value; }
        }

        public string VaryByCustom
        {
            get { return _cacheSettings.VaryByCustom ?? String.Empty; }
            set { _cacheSettings.VaryByCustom = value; }
        }

        public string VaryByHeader
        {
            get { return _cacheSettings.VaryByHeader ?? String.Empty; }
            set { _cacheSettings.VaryByHeader = value; }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param", Justification = "Matches the @ OutputCache page directive. Suppressed in source because this is a special case suppression.")]
        public string VaryByParam
        {
            get { return _cacheSettings.VaryByParam ?? String.Empty; }
            set { _cacheSettings.VaryByParam = value; }
        }

        private static void ClearChildActionFilterFinishCallback(ControllerContext controllerContext)
        {
            controllerContext.HttpContext.Items.Remove(_childActionFilterFinishCallbackKey);
        }

        private static void CompleteChildAction(ControllerContext filterContext, bool wasException)
        {
            Action<bool> callback = GetChildActionFilterFinishCallback(filterContext);

            if (callback != null)
            {
                ClearChildActionFilterFinishCallback(filterContext);
                callback(wasException);
            }
        }

        private static Action<bool> GetChildActionFilterFinishCallback(ControllerContext controllerContext)
        {
            return controllerContext.HttpContext.Items[_childActionFilterFinishCallbackKey] as Action<bool>;
        }

        internal string GetChildActionUniqueId(ActionExecutingContext filterContext)
        {
            StringBuilder uniqueIdBuilder = new StringBuilder();

            // Start with a prefix, presuming that we share the cache with other users
            uniqueIdBuilder.Append(CacheKeyPrefix);

            // Unique ID of the action description
            uniqueIdBuilder.Append(filterContext.ActionDescriptor.UniqueId);

            // Unique ID from the VaryByCustom settings, if any
            uniqueIdBuilder.Append(DescriptorUtil.CreateUniqueId(VaryByCustom));
            if (!String.IsNullOrEmpty(VaryByCustom))
            {
                string varyByCustomResult = filterContext.HttpContext.ApplicationInstance.GetVaryByCustomString(HttpContext.Current, VaryByCustom);
                uniqueIdBuilder.Append(varyByCustomResult);
            }

            // Unique ID from the VaryByParam settings, if any
            uniqueIdBuilder.Append(GetUniqueIdFromActionParameters(filterContext, VaryByParam));

            // The key is typically too long to be useful, so we use a cryptographic hash
            // as the actual key (better randomization and key distribution, so small vary
            // values will generate dramtically different keys).
            using (SHA256Cng sha = new SHA256Cng())
            {
                return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(uniqueIdBuilder.ToString())));
            }
        }

        private static string GetUniqueIdFromActionParameters(ActionExecutingContext filterContext, string varyByParam)
        {
            if (string.Equals(varyByParam, "none", StringComparison.OrdinalIgnoreCase))
            {
                return "";
            }
            var args = filterContext.ActionParameters;
            if (args.Count == 0) return ""; // nothing to do

            // Generate a unique ID of normalized key names + key values
            var result = new StringBuilder();
            if (string.Equals(varyByParam, "*", StringComparison.OrdinalIgnoreCase))
            {   // use all available key/value pairs (without caring about order, so sort the keys)
                string[] keys = new string[args.Count];
                args.Keys.CopyTo(keys, 0);
                Array.Sort(keys, StringComparer.OrdinalIgnoreCase);
                for(int i = 0; i < keys.Length; i++)
                {
                    var key = keys[i];
                    DescriptorUtil.AppendUniqueId(result, key.ToUpperInvariant());
                    DescriptorUtil.AppendUniqueId(result, args[key]);
                }
            }
            else
            {   // use only the key/value pairs specified in the varyByParam string; lazily create a sorted
                // dictionary to represent the selected keys (normalizes the order at the same time)
                SortedList<string, object> keyValues = null;
                foreach (var pair in args)
                {
                    if (ContainsToken(varyByParam, pair.Key))
                    {
                        if(keyValues == null) keyValues = new SortedList<string, object>(args.Count, StringComparer.OrdinalIgnoreCase);
                        keyValues[pair.Key] = pair.Value;
                    }
                }
                if (keyValues != null) // something to do
                {
                    foreach (var pair in keyValues)
                    {
                        DescriptorUtil.AppendUniqueId(result, pair.Key.ToUpperInvariant());
                        DescriptorUtil.AppendUniqueId(result, pair.Value);
                    }
                }
            }
            return result.ToString();
        }

        // check a delimited string i.e. "a;bcd; e" contains the given part, without actually splitting it -
        // by searching for the entire token, then checking whether the before/after is EOF, delimiter or white-space
        public static bool ContainsToken(string value, string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            if (string.IsNullOrEmpty(value)) return false;

            const char delimiter = ';'; // could be parameterized easily enough

            int lastIndex = -1, idx, endIndex = value.Length - token.Length, tokenLength = token.Length;
            while ((idx = value.IndexOf(token, lastIndex + 1, StringComparison.OrdinalIgnoreCase)) > lastIndex)
            {
                lastIndex = idx;
                char c;
                if (
                    (idx == 0 || ((c = value[idx - 1]) == delimiter) || char.IsWhiteSpace(c))
                    &&
                    (idx == endIndex || ((c = value[idx + tokenLength]) == delimiter) || char.IsWhiteSpace(c))
                    )
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsChildActionCacheActive(ControllerContext controllerContext)
        {
            return GetChildActionFilterFinishCallback(controllerContext) != null;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            // Complete the request if the child action threw an exception
            if (filterContext.IsChildAction && filterContext.Exception != null)
            {
                CompleteChildAction(filterContext, wasException: true);
            }
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (filterContext.IsChildAction)
            {
                // Skip validation and caching if there's no caching on the server for this action. It's ok to 
                // explicitly disable caching for a child action, but it will be ignored if the parent action
                // is using caching.
                if (IsServerSideCacheDisabled())
                {
                    return;
                }

                ValidateChildActionConfiguration();

                // Already actively being captured? (i.e., cached child action inside of cached child action)
                // Realistically, this needs write substitution to do properly (including things like authentication)
                if (GetChildActionFilterFinishCallback(filterContext) != null)
                {
                    throw new InvalidOperationException(MvcResources.OutputCacheAttribute_CannotNestChildCache);
                }

                // Already cached?
                string uniqueId = GetChildActionUniqueId(filterContext);
                string cachedValue = ChildActionCacheInternal.Get(uniqueId) as string;
                if (cachedValue != null)
                {
                    filterContext.Result = new ContentResult() { Content = cachedValue };
                    return;
                }

                // Swap in a new TextWriter so we can capture the output
                StringWriter cachingWriter = new StringWriter(CultureInfo.InvariantCulture);
                TextWriter originalWriter = filterContext.HttpContext.Response.Output;
                filterContext.HttpContext.Response.Output = cachingWriter;

                // Set a finish callback to clean up
                SetChildActionFilterFinishCallback(filterContext, wasException =>
                {
                    // Restore original writer
                    filterContext.HttpContext.Response.Output = originalWriter;

                    // Grab output and write it
                    string capturedText = cachingWriter.ToString();
                    filterContext.HttpContext.Response.Write(capturedText);

                    // Only cache output if this wasn't an error
                    if (!wasException)
                    {
                        ChildActionCacheInternal.Add(uniqueId, capturedText, DateTimeOffset.UtcNow.AddSeconds(Duration));
                    }
                });
            }
        }

        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (filterContext.IsChildAction)
            {
                CompleteChildAction(filterContext, wasException: true);
            }
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (!filterContext.IsChildAction)
            {
                // we need to call ProcessRequest() since there's no other way to set the Page.Response intrinsic
                using (OutputCachedPage page = new OutputCachedPage(_cacheSettings))
                {
                    page.ProcessRequest(HttpContext.Current);
                }
            }
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext == null)
            {
                throw new ArgumentNullException("filterContext");
            }

            if (filterContext.IsChildAction)
            {
                CompleteChildAction(filterContext, wasException: filterContext.Exception != null);
            }
        }

        private static void SetChildActionFilterFinishCallback(ControllerContext controllerContext, Action<bool> callback)
        {
            controllerContext.HttpContext.Items[_childActionFilterFinishCallbackKey] = callback;
        }

        private void ValidateChildActionConfiguration()
        {
            if (!String.IsNullOrWhiteSpace(CacheProfile) ||
                !String.IsNullOrWhiteSpace(SqlDependency) ||
                !String.IsNullOrWhiteSpace(VaryByContentEncoding) ||
                !String.IsNullOrWhiteSpace(VaryByHeader) ||
                _locationWasSet || _noStoreWasSet)
            {
                throw new InvalidOperationException(MvcResources.OutputCacheAttribute_ChildAction_UnsupportedSetting);
            }

            if (Duration <= 0)
            {
                throw new InvalidOperationException(MvcResources.OutputCacheAttribute_InvalidDuration);
            }

            if (String.IsNullOrWhiteSpace(VaryByParam))
            {
                throw new InvalidOperationException(MvcResources.OutputCacheAttribute_InvalidVaryByParam);
            }
        }

        private bool IsServerSideCacheDisabled()
        {
            switch (Location)
            {
                case OutputCacheLocation.None:
                case OutputCacheLocation.Client:
                case OutputCacheLocation.Downstream:
                    return true;

                default: 
                    return false;
            }
        }

        [SuppressMessage("ASP.NET.Security", "CA5328:ValidateRequestShouldBeEnabled", Justification = "Instances of this type are not created in response to direct user input.")]
        private sealed class OutputCachedPage : Page
        {
            private OutputCacheParameters _cacheSettings;

            public OutputCachedPage(OutputCacheParameters cacheSettings)
            {
                // Tracing requires Page IDs to be unique.
                ID = Guid.NewGuid().ToString();
                _cacheSettings = cacheSettings;
            }

            protected override void FrameworkInitialize()
            {
                // when you put the <%@ OutputCache %> directive on a page, the generated code calls InitOutputCache() from here
                base.FrameworkInitialize();
                InitOutputCache(_cacheSettings);
            }
        }
    }
}
