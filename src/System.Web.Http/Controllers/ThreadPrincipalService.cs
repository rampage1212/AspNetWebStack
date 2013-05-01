﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Security.Principal;
using System.Threading;

namespace System.Web.Http.Controllers
{
    public class ThreadPrincipalService : IHostPrincipalService
    {
        public IPrincipal GetCurrentPrincipal(HttpRequestMessage request)
        {
            return Thread.CurrentPrincipal;
        }

        public void SetCurrentPrincipal(IPrincipal principal, HttpRequestMessage request)
        {
            Thread.CurrentPrincipal = principal;
        }
    }
}
