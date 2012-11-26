﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Facebook;

namespace Microsoft.AspNet.Mvc.Facebook.Providers
{
    public interface IFacebookClientProvider
    {
        FacebookClient CreateClient();
    }
}
