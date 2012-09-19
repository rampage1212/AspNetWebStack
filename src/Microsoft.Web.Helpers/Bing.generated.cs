﻿using Resources;

#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.235
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Web.Helpers
{
    using System;
    using System.Collections.Generic;
    
    #line 3 "Bing.cshtml"
    using System.Globalization;
    
    #line default
    #line hidden
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    
    #line 4 "Bing.cshtml"
    using System.Web;
    
    #line default
    #line hidden
    using System.Web.Helpers;
    using System.Web.Security;
    using System.Web.UI;
    using System.Web.WebPages;
    using System.Web.WebPages.Html;
    
    #line 5 "Bing.cshtml"
    using System.Web.WebPages.Scope;
    
    #line default
    #line hidden
    
    #line 6 "Bing.cshtml"
    using Microsoft.Internal.Web.Utils;
    
    #line default
    #line hidden
    
    public class Bing : System.Web.WebPages.HelperPage
    {
        
        #line 8 "Bing.cshtml"

    private const string DefaultBoxWidth = "322px";
    internal static readonly object _siteTitleKey = new object();
    internal static readonly object _siteUrlKey = new object();


    public static string SiteTitle {
        get {
            return ScopeStorage.CurrentScope[_siteTitleKey] as string;
        }

        set {
            if (value == null) {
                throw new ArgumentNullException("SiteTitle");
            }
            ScopeStorage.CurrentScope[_siteTitleKey] = value;
        }
    }

    public static string SiteUrl {
        get {
           return ScopeStorage.CurrentScope[_siteUrlKey] as string;
        }

        set {
            if (value == null) {
                throw new ArgumentNullException("SiteUrl");
            }
            ScopeStorage.CurrentScope[_siteUrlKey] = value;
        }
    }
    
    private static int GetCodePageFromRequest(HttpContextBase httpContext) {
        return httpContext.Response.ContentEncoding.CodePage;
    }

    private static string GetSiteUrl(IDictionary<object, object> scopeStorage, string siteUrl) {
        object result;
        if (siteUrl.IsEmpty() && scopeStorage.TryGetValue(_siteUrlKey, out result)){
            siteUrl = result as string;
        }
        return siteUrl;
    }

    private static string GetSiteTitle(IDictionary<object, object> scopeStorage, string siteTitle) {
        object result;
        if (siteTitle.IsEmpty() && scopeStorage.TryGetValue(_siteTitleKey, out result)) {
            siteTitle = result as string;
        }
        return siteTitle;
    }

        #line default
        #line hidden

public static System.Web.WebPages.HelperResult SearchBox(string boxWidth = DefaultBoxWidth, string siteUrl = null, string siteTitle = null) {
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {



#line 61 "Bing.cshtml"
                                                                                                      
    
#line default
#line hidden


#line 62 "Bing.cshtml"
WriteTo(@__razor_helper_writer, _SearchBox(boxWidth, siteUrl, siteTitle, new HttpContextWrapper(HttpContext.Current), ScopeStorage.CurrentScope));

#line default
#line hidden


#line 62 "Bing.cshtml"
                                                                                                                     
 
#line default
#line hidden

});

 }


internal static System.Web.WebPages.HelperResult _SearchBox(string boxWidth, string siteUrl, string siteTitle, HttpContextBase context, IDictionary<object, object> scopeStorage) {
return new System.Web.WebPages.HelperResult(__razor_helper_writer => {



#line 65 "Bing.cshtml"
                                                                                                                                          
    siteTitle = GetSiteTitle(scopeStorage, siteTitle);
    siteUrl = GetSiteUrl(scopeStorage, siteUrl);
    string searchSite = String.IsNullOrEmpty(siteTitle) ?  HelpersToolkitResources.BingSearch_DefaultSiteSearchText : siteTitle;


#line default
#line hidden

WriteLiteralTo(@__razor_helper_writer, "    <form action=\"http://www.bing.com/search\" class=\"BingSearch\" method=\"get\" tar" +
"get=\"_blank\">\r\n    <input name=\"FORM\" type=\"hidden\" value=\"FREESS\" />\r\n    <inpu" +
"t name=\"cp\" type=\"hidden\" value=\"");



#line 72 "Bing.cshtml"
           WriteTo(@__razor_helper_writer, GetCodePageFromRequest(context));

#line default
#line hidden

WriteLiteralTo(@__razor_helper_writer, "\" />\r\n    <table cellpadding=\"0\" cellspacing=\"0\" style=\"width:");



#line 73 "Bing.cshtml"
                         WriteTo(@__razor_helper_writer, boxWidth);

#line default
#line hidden

WriteLiteralTo(@__razor_helper_writer, @";"">
    <tr style=""height: 32px"">
        <td style=""width: 100%; border:solid 1px #ccc; border-right-style:none; padding-left:10px; padding-right:10px; vertical-align:middle;"">
            <input name=""q"" style=""background-image:url(http://www.bing.com/siteowner/s/siteowner/searchbox_background_k.png); background-position:right; background-repeat:no-repeat; font-family:Arial; font-size:14px; color:#000; width:100%; border:none 0 transparent;"" title=""Search Bing"" type=""text"" />
        </td>
        <td style=""border:solid 1px #ccc; border-left-style:none; padding-left:0px; padding-right:3px;"">
            <input alt=""Search"" src=""http://www.bing.com/siteowner/s/siteowner/searchbutton_normal_k.gif"" style=""border:none 0 transparent; height:24px; width:24px; vertical-align:top;"" type=""image"" />
        </td>
    </tr>
");



#line 82 "Bing.cshtml"
     if (!String.IsNullOrEmpty(siteUrl)) {

#line default
#line hidden

WriteLiteralTo(@__razor_helper_writer, "    <tr>\r\n        <td colspan=\"2\" style=\"font-size: small\">\r\n            <label><" +
"input checked=\"checked\" name=\"q1\" type=\"radio\" value=\"site:");



#line 85 "Bing.cshtml"
                                                WriteTo(@__razor_helper_writer, siteUrl);

#line default
#line hidden

WriteLiteralTo(@__razor_helper_writer, "\" />");



#line 85 "Bing.cshtml"
                                                            WriteTo(@__razor_helper_writer, searchSite);

#line default
#line hidden

WriteLiteralTo(@__razor_helper_writer, "</label>&nbsp;<label><input name=\"q1\" type=\"radio\" value=\"\" />");



#line 85 "Bing.cshtml"
                                                                                                                                     WriteTo(@__razor_helper_writer, HelpersToolkitResources.BingSearch_DefaultWebSearchText);

#line default
#line hidden

WriteLiteralTo(@__razor_helper_writer, "</label>\r\n         </td>\r\n    </tr>\r\n");



#line 88 "Bing.cshtml"
    }

#line default
#line hidden

WriteLiteralTo(@__razor_helper_writer, "    </table>\r\n    </form>\r\n");



#line 91 "Bing.cshtml"

#line default
#line hidden

});

}


        public Bing()
        {
        }
    }
}
#pragma warning restore 1591
