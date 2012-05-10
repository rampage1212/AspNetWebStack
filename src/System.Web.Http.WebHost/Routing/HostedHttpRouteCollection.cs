﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Hosting;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using System.Web.Http.WebHost.Properties;
using System.Web.Routing;

namespace System.Web.Http.WebHost.Routing
{
    internal class HostedHttpRouteCollection : HttpRouteCollection
    {
        private readonly RouteCollection _routeCollection;

        public HostedHttpRouteCollection(RouteCollection routeCollection)
        {
            if (routeCollection == null)
            {
                throw Error.ArgumentNull("routeCollection");
            }

            _routeCollection = routeCollection;
        }

        /// <inheritdoc/>
        public override string VirtualPathRoot
        {
            get { return HostingEnvironment.ApplicationVirtualPath; }
        }

        /// <inheritdoc/>
        public override int Count
        {
            get { return _routeCollection.Count; }
        }

        /// <inheritdoc/>
        public override IHttpRoute this[string name]
        {
            get
            {
                HttpWebRoute route = _routeCollection[name] as HttpWebRoute;
                if (route != null)
                {
                    return route.HttpRoute;
                }

                throw Error.KeyNotFound();
            }
        }

        /// <inheritdoc/>
        public override IHttpRoute this[int index]
        {
            get
            {
                HttpWebRoute route = _routeCollection[index] as HttpWebRoute;
                if (route != null)
                {
                    return route.HttpRoute;
                }

                throw Error.ArgumentOutOfRange("index", index, SRResources.RouteCollectionOutOfRange);
            }
        }

        /// <inheritdoc/>
        public override IHttpRouteData GetRouteData(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw Error.ArgumentNull("request");
            }

            HttpContextBase httpContextBase;
            if (request.Properties.TryGetValue(HttpControllerHandler.HttpContextBaseKey, out httpContextBase))
            {
                RouteData routeData = _routeCollection.GetRouteData(httpContextBase);
                if (routeData != null)
                {
                    return new HostedHttpRouteData(routeData);
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public override IHttpVirtualPathData GetVirtualPath(HttpControllerContext controllerContext, string name, IDictionary<string, object> values)
        {
            if (controllerContext == null)
            {
                throw Error.ArgumentNull("controllerContext");
            }

            HttpRequestMessage request = controllerContext.Request;
            HttpContextBase httpContextBase;
            if (request.Properties.TryGetValue(HttpControllerHandler.HttpContextBaseKey, out httpContextBase))
            {
                RequestContext requestContext = new RequestContext(httpContextBase, controllerContext.RouteData.ToRouteData());
                RouteValueDictionary routeValues = values != null ? new RouteValueDictionary(values) : new RouteValueDictionary();
                VirtualPathData virtualPathData = _routeCollection.GetVirtualPath(requestContext, name, routeValues);
                if (virtualPathData != null)
                {
                    return new HostedHttpVirtualPathData(virtualPathData, controllerContext.RouteData.Route);
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public override IHttpRoute CreateRoute(string uriTemplate, IDictionary<string, object> defaults, IDictionary<string, object> constraints, IDictionary<string, object> dataTokens, HttpMessageHandler handler)
        {
            return new HostedHttpRoute(uriTemplate, defaults, constraints, dataTokens, handler);
        }

        /// <inheritdoc/>
        public override void Add(string name, IHttpRoute route)
        {
            _routeCollection.Add(name, route.ToRoute());
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            _routeCollection.Clear();
        }

        /// <inheritdoc/>
        public override bool Contains(IHttpRoute item)
        {
            foreach (RouteBase route in _routeCollection)
            {
                HttpWebRoute webRoute = route as HttpWebRoute;
                if (webRoute != null && webRoute.HttpRoute == item)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override bool ContainsKey(string name)
        {
            return _routeCollection[name] != null;
        }

        /// <inheritdoc/>
        public override void CopyTo(IHttpRoute[] array, int arrayIndex)
        {
            throw NotSupportedByHostedRouteCollection();
        }

        /// <inheritdoc/>
        public override void CopyTo(KeyValuePair<string, IHttpRoute>[] array, int arrayIndex)
        {
            throw NotSupportedByRouteCollection();
        }

        /// <inheritdoc/>
        public override void Insert(int index, string name, IHttpRoute value)
        {
            throw NotSupportedByRouteCollection();
        }

        /// <inheritdoc/>
        public override bool Remove(string name)
        {
            throw NotSupportedByRouteCollection();
        }

        /// <inheritdoc/>
        public override IEnumerator<IHttpRoute> GetEnumerator()
        {
            // Here we only care about Web API routes.
            return _routeCollection
                .OfType<HttpWebRoute>()
                .Select(httpWebRoute => httpWebRoute.HttpRoute)
                .GetEnumerator();
        }

        /// <inheritdoc/>
        public override bool TryGetValue(string name, out IHttpRoute route)
        {
            HttpWebRoute rt = _routeCollection[name] as HttpWebRoute;
            if (rt != null)
            {
                route = rt.HttpRoute;
                return true;
            }

            route = null;
            return false;
        }

        private static NotSupportedException NotSupportedByRouteCollection()
        {
            return Error.NotSupported(SRResources.RouteCollectionNotSupported, typeof(HostedHttpRouteCollection).Name);
        }

        private static NotSupportedException NotSupportedByHostedRouteCollection()
        {
            return Error.NotSupported(SRResources.RouteCollectionUseDirectly, typeof(RouteCollection).Name);
        }
    }
}
