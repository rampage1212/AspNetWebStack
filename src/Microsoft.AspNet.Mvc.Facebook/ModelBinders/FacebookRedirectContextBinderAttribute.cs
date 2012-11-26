﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Web.Mvc;

namespace Microsoft.AspNet.Mvc.Facebook.ModelBinders
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal class FacebookRedirectContextBinderAttribute : CustomModelBinderAttribute
    {
        public override IModelBinder GetBinder()
        {
            return new FacebookRedirectContextModelBinder(GlobalFacebookConfiguration.Configuration);
        }
    }
}
