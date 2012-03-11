﻿using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace System.Web.Http
{
    public class RequireAdminAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext context)
        {
            // do authorization based on the principle.
            IPrincipal principal = context.ControllerContext.Request.GetUserPrincipal() as IPrincipal;
            if (principal == null || !principal.IsInRole("Administrators"))
            {
                context.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        }
    }
}