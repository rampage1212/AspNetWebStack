﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Web.Http.Routing.Constraints;

namespace System.Web.Http.Routing
{
    public class DefaultInlineRouteConstraintResolver : IInlineRouteConstraintResolver
    {
        private readonly IDictionary<string, Type> _inlineRouteConstraintMap = GetDefaultInlineRouteConstraints();

        public IHttpRouteConstraint ResolveConstraint(string inlineConstraint)
        {
            string constraintKey;
            string[] arguments;
            int indexOfFirstOpenParens = inlineConstraint.IndexOf('(');
            if (indexOfFirstOpenParens >= 0 && inlineConstraint.EndsWith(")", StringComparison.Ordinal))
            {
                string argumentString = inlineConstraint.Substring(indexOfFirstOpenParens + 1, inlineConstraint.Length - indexOfFirstOpenParens - 2);
                constraintKey = inlineConstraint.Substring(0, indexOfFirstOpenParens);
                
                // If this is a regex constraint, don't split on commas that might be part of the pattern.
                if (constraintKey == "regex")
                {
                    arguments = new[] { argumentString };
                }
                else
                {
                    arguments = argumentString.Split(',');                    
                }
            }
            else
            {
                constraintKey = inlineConstraint;
                arguments = new string[0];
            }

            // TODO: parse parameters
            return ResolveConstraint(constraintKey, arguments);
        }

        internal IHttpRouteConstraint ResolveConstraint(string constraintKey, string[] arguments)
        {
            // Make sure the key exists in the key/Type map.
            if (!_inlineRouteConstraintMap.ContainsKey(constraintKey))
            {
                throw Error.KeyNotFound(
                    "Could not resolve a route constraint for the key '{0}'.", constraintKey);
            }

            Type type = _inlineRouteConstraintMap[constraintKey];

            // Convert the args to the types expected by the relevant constraint ctor.
            List<object> convertedArguments = new List<object>(arguments);
            foreach (ConstructorInfo constructor in type.GetConstructors())
            {
                // Find the ctor with the correct number of args.
                var parameters = constructor.GetParameters();
                if (parameters.Length == convertedArguments.Count)
                {
                    // Convert the given string args to the correct type.
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        ParameterInfo parameter = parameters[i];
                        Type parameterType = parameter.ParameterType;
                        object convertedValue = Convert.ChangeType(convertedArguments[i], parameterType, CultureInfo.InvariantCulture);
                        convertedArguments[i] = convertedValue;
                    }
            
                    break;
                }
            }

            return (IHttpRouteConstraint)Activator.CreateInstance(type, convertedArguments.ToArray());
        }

        private static IDictionary<string, Type> GetDefaultInlineRouteConstraints()
        {
            return new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "alpha", typeof(AlphaHttpRouteConstraint) },
                { "bool", typeof(BoolHttpRouteConstraint) },
                { "datetime", typeof(DateTimeHttpRouteConstraint) },
                { "decimal", typeof(DecimalHttpRouteConstraint) },
                { "double", typeof(DoubleHttpRouteConstraint) },
                { "float", typeof(FloatHttpRouteConstraint) },
                { "guid", typeof(GuidHttpRouteConstraint) },
                { "int", typeof(IntHttpRouteConstraint) },
                { "length", typeof(LengthHttpRouteConstraint) },
                { "long", typeof(LongHttpRouteConstraint) },
                { "max", typeof(MaxHttpRouteConstraint) },
                { "maxlength", typeof(MaxLengthHttpRouteConstraint) },
                { "min", typeof(MinHttpRouteConstraint) },
                { "minlength", typeof(MinLengthHttpRouteConstraint) },
                { "range", typeof(RangeHttpRouteConstraint) },
                { "regex", typeof(RegexHttpRouteConstraint) }
            };
        }
    }
}
