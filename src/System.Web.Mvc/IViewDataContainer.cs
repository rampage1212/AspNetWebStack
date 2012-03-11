﻿using System.Diagnostics.CodeAnalysis;

namespace System.Web.Mvc
{
    public interface IViewDataContainer
    {
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is the mechanism by which the ViewPage / ViewUserControl get their ViewDataDictionary objects.")]
        ViewDataDictionary ViewData { get; set; }
    }
}
