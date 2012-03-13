﻿using System.ComponentModel;
using System.Web.WebPages.Razor;

namespace Microsoft.Web.WebPages.OAuth
{
    /// <summary>
    /// Defines Start() method that gets executed when this assembly is loaded by ASP.NET
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PreApplicationStartCode
    {
        /// <summary>
        /// Register global namepace imports for this assembly 
        /// </summary>
        public static void Start()
        {
            WebPageRazorHost.AddGlobalImport("DotNetOpenAuth.AspNet");
            WebPageRazorHost.AddGlobalImport("Microsoft.Web.WebPages.OAuth");
        }
    }
}