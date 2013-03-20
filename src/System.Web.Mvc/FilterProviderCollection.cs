﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Web.Mvc
{
    public class FilterProviderCollection : Collection<IFilterProvider>
    {
        private static FilterComparer _filterComparer = new FilterComparer();
        private IFilterProvider[] _combinedItems;
        private IDependencyResolver _dependencyResolver;

        public FilterProviderCollection()
        {
        }

        public FilterProviderCollection(IList<IFilterProvider> providers)
            : base(providers)
        {
        }

        internal FilterProviderCollection(IList<IFilterProvider> list, IDependencyResolver dependencyResolver)
            : base(list)
        {
            _dependencyResolver = dependencyResolver;
        }

        internal IFilterProvider[] CombinedItems
        {
            get
            {
                IFilterProvider[] combinedItems = _combinedItems;
                if (combinedItems == null)
                {
                    combinedItems = MultiServiceResolver.GetCombined<IFilterProvider>(Items, _dependencyResolver);
                    _combinedItems = combinedItems;
                }
                return combinedItems;
            }
        }

        private static bool AllowMultiple(object filterInstance)
        {
            IMvcFilter mvcFilter = filterInstance as IMvcFilter;
            if (mvcFilter == null)
            {
                return true;
            }

            return mvcFilter.AllowMultiple;
        }

        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException("controllerContext");
            }
            if (actionDescriptor == null)
            {
                throw new ArgumentNullException("actionDescriptor");
            }

            IEnumerable<Filter> combinedFilters =
                CombinedItems.SelectMany(fp => fp.GetFilters(controllerContext, actionDescriptor))
                    .OrderBy(filter => filter, _filterComparer);

            // Remove duplicates from the back forward
            return RemoveDuplicates(combinedFilters.Reverse()).Reverse();
        }

        private IEnumerable<Filter> RemoveDuplicates(IEnumerable<Filter> filters)
        {
            HashSet<Type> visitedTypes = new HashSet<Type>();

            foreach (Filter filter in filters)
            {
                object filterInstance = filter.Instance;
                Type filterInstanceType = filterInstance.GetType();

                if (!visitedTypes.Contains(filterInstanceType) || AllowMultiple(filterInstance))
                {
                    yield return filter;
                    visitedTypes.Add(filterInstanceType);
                }
            }
        }

        protected override void InsertItem(int index, IFilterProvider item)
        {
            _combinedItems = null;
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            _combinedItems = null;
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, IFilterProvider item)
        {
            _combinedItems = null;
            base.SetItem(index, item);
        }

        private class FilterComparer : IComparer<Filter>
        {
            public int Compare(Filter x, Filter y)
            {
                // Nulls always have to be less than non-nulls
                if (x == null && y == null)
                {
                    return 0;
                }
                if (x == null)
                {
                    return -1;
                }
                if (y == null)
                {
                    return 1;
                }

                // Sort first by order...

                if (x.Order < y.Order)
                {
                    return -1;
                }
                if (x.Order > y.Order)
                {
                    return 1;
                }

                // ...then by scope

                if (x.Scope < y.Scope)
                {
                    return -1;
                }
                if (x.Scope > y.Scope)
                {
                    return 1;
                }

                return 0;
            }
        }
    }
}
