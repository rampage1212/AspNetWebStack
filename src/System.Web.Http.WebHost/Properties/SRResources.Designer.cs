﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18010
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace System.Web.Http.WebHost.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SRResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SRResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("System.Web.Http.WebHost.Properties.SRResources", typeof(SRResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This operation is not supported by &apos;{0}&apos;..
        /// </summary>
        internal static string RouteCollectionNotSupported {
            get {
                return ResourceManager.GetString("RouteCollectionNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The index cannot be less than 0 or equal to or larger than the number of items in the collection..
        /// </summary>
        internal static string RouteCollectionOutOfRange {
            get {
                return ResourceManager.GetString("RouteCollectionOutOfRange", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This operation is only supported by directly calling it on &apos;{0}&apos;..
        /// </summary>
        internal static string RouteCollectionUseDirectly {
            get {
                return ResourceManager.GetString("RouteCollectionUseDirectly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;{0}&apos; type failed to serialize the response body..
        /// </summary>
        internal static string Serialize_Response_Failed {
            get {
                return ResourceManager.GetString("Serialize_Response_Failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The &apos;{0}&apos; type failed to serialize the response body for content type &apos;{1}&apos;..
        /// </summary>
        internal static string Serialize_Response_Failed_MediaType {
            get {
                return ResourceManager.GetString("Serialize_Response_Failed_MediaType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This file is automatically generated. Please do not modify the contents of this file..
        /// </summary>
        internal static string TypeCache_DoNotModify {
            get {
                return ResourceManager.GetString("TypeCache_DoNotModify", resourceCulture);
            }
        }
    }
}
