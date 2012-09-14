﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Http.OData.Properties;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData.Query.SemanticAst;

namespace System.Web.Http.OData.Query
{
    /// <summary>
    /// This defines a $orderby OData query option that can be used to perform query composition. 
    /// </summary>
    public class OrderByQueryOption
    {
        private OrderByQueryNode _queryNode;
        private ICollection<OrderByPropertyNode> _propertyNodes;

        /// <summary>
        /// Initialize a new instance of <see cref="OrderByQueryOption"/> based on the raw $orderby value and 
        /// an EdmModel from <see cref="ODataQueryContext"/>.
        /// </summary>
        /// <param name="rawValue">The raw value for $orderby query. It can be null or empty.</param>
        /// <param name="context">The <see cref="ODataQueryContext"/> which contains the <see cref="IEdmModel"/> and some type information</param>
        public OrderByQueryOption(string rawValue, ODataQueryContext context)
        {
            if (context == null)
            {
                throw Error.ArgumentNull("context");
            }

            if (String.IsNullOrEmpty(rawValue))
            {
                throw Error.ArgumentNullOrEmpty("rawValue");
            }

            Context = context;
            RawValue = rawValue;
        }

        /// <summary>
        ///  Gets the given <see cref="ODataQueryContext"/>.
        /// </summary>        
        public ODataQueryContext Context { get; private set; }

        /// <summary>
        /// Gets the collection of <see cref="OrderByPropertyNode"/> instance
        /// for the current <see cref="OrderByQueryOption"/>.
        /// </summary>
        /// <remarks>
        /// This collection can be modified as needed.
        /// </remarks>
        public ICollection<OrderByPropertyNode> PropertyNodes
        {
            get
            {
                if (_propertyNodes == null)
                {
                    _propertyNodes = OrderByPropertyNode.CreateCollection(QueryNode);
                }
                return _propertyNodes;
            }
        }

        /// <summary>
        ///  Gets the raw $orderby value.
        /// </summary>
        public string RawValue { get; private set; }

        /// <summary>
        /// Gets the <see cref="OrderByQueryNode"/> for this query option.
        /// </summary>
        private OrderByQueryNode QueryNode
        {
            get
            {
                if (_queryNode == null)
                {
                    // TODO: Bug 462293: Review this code with Alex!
                    // 1. Do I need to create this fake uri?
                    Uri fakeServiceRootUri = new Uri("http://server/");
                    Uri fakeQueryOptionsUri = new Uri(fakeServiceRootUri, String.Format(CultureInfo.InvariantCulture, "{0}/?$orderby={1}", Context.EntitySet.Name, Uri.EscapeDataString(RawValue)));
                    SemanticTree semanticTree = SemanticTree.ParseUri(fakeQueryOptionsUri, fakeServiceRootUri, Context.Model);
                    _queryNode = semanticTree.Query as OrderByQueryNode;
                }
                return _queryNode;
            }
        }

        /// <summary>
        /// Apply the $orderby query to the given IQueryable.
        /// </summary>
        /// <param name="query">The IQueryable that we are applying orderby query against.</param>
        /// <returns>The query that the orderby query has been applied to.</returns>
        public IOrderedQueryable<T> ApplyTo<T>(IQueryable<T> query)
        {
            return ApplyToCore(query) as IOrderedQueryable<T>;
        }

        /// <summary>
        /// Apply the $orderby query to the given IQueryable.
        /// </summary>
        /// <param name="query">The IQueryable that we are applying orderby query against.</param>
        /// <returns>The query that the orderby query has been applied to.</returns>
        public IOrderedQueryable ApplyTo(IQueryable query)
        {
            return ApplyToCore(query);
        }

        private IOrderedQueryable ApplyToCore(IQueryable query)
        {
            // TODO 463999: [OData] Consider moving OrderByPropertyNode to ODataLib
            ICollection<OrderByPropertyNode> props = PropertyNodes;

            bool alreadyOrdered = false;
            IQueryable querySoFar = query;
            HashSet<IEdmProperty> propertiesSoFar = new HashSet<IEdmProperty>();

            foreach (OrderByPropertyNode prop in props)
            {
                IEdmProperty property = prop.Property;
                OrderByDirection direction = prop.Direction;

                // This check prevents queries with duplicate properties (e.g. $orderby=Id,Id,Id,Id...) from causing stack overflows
                if (propertiesSoFar.Contains(property))
                {
                    throw new ODataException(Error.Format(SRResources.OrderByDuplicateProperty, property.Name));
                }
                propertiesSoFar.Add(property);

                querySoFar = ExpressionHelpers.OrderBy(querySoFar, property, direction, Context.EntityClrType, alreadyOrdered);
                alreadyOrdered = true;
            }

            return querySoFar as IOrderedQueryable;
        }
    }
}
