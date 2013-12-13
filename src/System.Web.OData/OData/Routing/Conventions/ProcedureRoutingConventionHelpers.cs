// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Http.Controllers;
using Microsoft.Data.Edm;

namespace System.Web.Http.OData.Routing.Conventions
{
    internal static class ProcedureRoutingConventionHelpers
    {
        public static string SelectAction(this IEdmFunctionImport procedure, ILookup<string, HttpActionDescriptor> actionMap, bool isCollection)
        {
            Contract.Assert(actionMap != null);

            if (procedure == null)
            {
                return null;
            }

            // The binding parameter is the first parameter by convention
            IEdmFunctionParameter bindingParameter = procedure.Parameters.FirstOrDefault();
            if (procedure.IsBindable && bindingParameter != null)
            {
                IEdmEntityType entityType = null;
                if (!isCollection)
                {
                    entityType = bindingParameter.Type.Definition as IEdmEntityType;
                }
                else
                {
                    IEdmCollectionType bindingParameterType = bindingParameter.Type.Definition as IEdmCollectionType;
                    if (bindingParameterType != null)
                    {
                        entityType = bindingParameterType.ElementType.Definition as IEdmEntityType;
                    }
                }

                if (entityType == null)
                {
                    return null;
                }

                string targetActionName = isCollection
                    ? procedure.Name + "OnCollectionOf" + entityType.Name
                    : procedure.Name + "On" + entityType.Name;
                return actionMap.FindMatchingAction(targetActionName, procedure.Name);
            }

            return null;
        }

        public static void AddKeyValueToRouteData(this HttpControllerContext controllerContext, ODataPath odataPath)
        {
            Contract.Assert(controllerContext != null);
            Contract.Assert(odataPath != null);

            KeyValuePathSegment keyValueSegment = odataPath.Segments[1] as KeyValuePathSegment;
            if (keyValueSegment != null)
            {
                controllerContext.RouteData.Values[ODataRouteConstants.Key] = keyValueSegment.Value;
            }
        }

        public static void AddFunctionParameterToRouteData(this HttpControllerContext controllerContext, FunctionPathSegment functionSegment)
        {
            Contract.Assert(controllerContext != null);
            Contract.Assert(functionSegment != null);

            foreach (KeyValuePair<string, string> nameAndValue in functionSegment.Values)
            {
                string name = nameAndValue.Key;
                object value = functionSegment.GetParameterValue(name);
                UnresolvedParameterValue unresolvedParameterValue = value as UnresolvedParameterValue;
                if (unresolvedParameterValue != null)
                {
                    value = unresolvedParameterValue.Resolve(controllerContext.Request.RequestUri);
                }
                controllerContext.RouteData.Values.Add(name, value);
            }
        }
    }
}
