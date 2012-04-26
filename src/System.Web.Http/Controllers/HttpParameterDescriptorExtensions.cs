﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http.Formatting;
using System.Web.Http.ModelBinding;
using System.Web.Http.Validation;
using System.Web.Http.ValueProviders;

namespace System.Web.Http.Controllers
{    
    /// <summary>
    /// Convenience helpers to easily create specific types of parameter bindings
    /// These provide a direct programmatic counterpart to the ModelBinder attributes. 
    /// </summary>
    public static class ParameterBindingExtensions
    {        
        /// <summary>
        /// If we know statically that this binding can never succeed, then use an error binding.
        /// This will prevent the action from executing.
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <param name="message">error message for user.</param>
        /// <returns>an error binding. Specifically, IsValid on the binding will be false.</returns>
        public static HttpParameterBinding BindAsError(this HttpParameterDescriptor parameter, string message)
        {
            return new ErrorParameterBinding(parameter, message);
        }
                
        /// <summary>
        /// Bind the parameter using model binding. Get all other information from the configuration.
        /// This is the same as having a plain ModelBinderAttribute on the parameter.
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <returns>a binding</returns>
        public static HttpParameterBinding BindWithModelBinding(this HttpParameterDescriptor parameter)
        {
            return BindWithModelBinding(parameter, new ModelBinderAttribute());
        }

        /// <summary>
        /// Bind the parameter as if it had the given attribute on the declaration.
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <param name="attribute">attribute to describe the binding.</param>
        /// <returns>a binding</returns>
        public static HttpParameterBinding BindWithModelBinding(this HttpParameterDescriptor parameter, ModelBinderAttribute attribute)
        {
            HttpControllerDescriptor controllerDescriptor = parameter.ActionDescriptor.ControllerDescriptor;

            IModelBinder binder = attribute.GetModelBinder(controllerDescriptor, parameter.ParameterType);
            IEnumerable<ValueProviderFactory> valueProviderFactories = attribute.GetValueProviderFactories(controllerDescriptor);

            return BindWithModelBinding(parameter, binder, valueProviderFactories);
        }

        /// <summary>
        /// Bind the parameter using the given model binder. 
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <param name="binder">model binder to use on parameter</param>
        /// <returns>a binding</returns>
        public static HttpParameterBinding BindWithModelBinding(this HttpParameterDescriptor parameter, IModelBinder binder)
        {
            HttpControllerDescriptor controllerDescriptor = parameter.ActionDescriptor.ControllerDescriptor;
            IEnumerable<ValueProviderFactory> valueProviderFactories = new ModelBinderAttribute().GetValueProviderFactories(controllerDescriptor);

            return BindWithModelBinding(parameter, binder, valueProviderFactories);
        }
        
        /// <summary>
        /// Bind the parameter using default model binding but with the supplied value providers.
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <param name="valueProviderFactories">value provider factories to feed to model binders</param>
        /// <returns>a binding</returns>
        public static HttpParameterBinding BindWithModelBinding(this HttpParameterDescriptor parameter, params ValueProviderFactory[] valueProviderFactories)
        {
            return BindWithModelBinding(parameter, (IEnumerable<ValueProviderFactory>)valueProviderFactories);
        }

        /// <summary>
        /// Bind the parameter using default model binding but with the supplied value providers.
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <param name="valueProviderFactories">value provider factories to feed to model binders</param>
        /// <returns>a binding</returns>
        public static HttpParameterBinding BindWithModelBinding(this HttpParameterDescriptor parameter, IEnumerable<ValueProviderFactory> valueProviderFactories)
        {
            HttpControllerDescriptor controllerDescriptor = parameter.ActionDescriptor.ControllerDescriptor;
            IModelBinder binder = new ModelBinderAttribute().GetModelBinder(controllerDescriptor, parameter.ParameterType);

            return new ModelBinderParameterBinding(parameter, binder, valueProviderFactories);
        }

        /// <summary>
        /// Bind the parameter using the supplied binder and value providers.
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <param name="binder">model binder to use for binding.</param>
        /// <param name="valueProviderFactories">value provider factories to feed to model binder.</param>
        /// <returns>a binding</returns>
        public static HttpParameterBinding BindWithModelBinding(this HttpParameterDescriptor parameter, IModelBinder binder, IEnumerable<ValueProviderFactory> valueProviderFactories)
        {
            return new ModelBinderParameterBinding(parameter, binder, valueProviderFactories);
        }
                
        /// <summary>
        /// Bind the parameter from the body using the formatters from the configuration. 
        /// This is like having a [FromBody] attribute on the parameter
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <returns>a binding</returns>
        public static HttpParameterBinding BindWithFormatter(this HttpParameterDescriptor parameter)
        {
            HttpControllerDescriptor controllerDescriptor = parameter.ActionDescriptor.ControllerDescriptor;

            IEnumerable<MediaTypeFormatter> formatters = controllerDescriptor.Formatters;
            IBodyModelValidator validators = controllerDescriptor.ControllerServices.GetBodyModelValidator();

            return new FormatterParameterBinding(parameter, formatters, validators);
        }

        /// <summary>
        /// Bind this parameter from the body using the supplied set of formatters.
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <param name="formatters">formatters to choose from when binding the body</param>
        /// <returns>a binding</returns>
        public static HttpParameterBinding BindWithFormatter(this HttpParameterDescriptor parameter, params MediaTypeFormatter[] formatters)
        {
            return BindWithFormatter(parameter, (IEnumerable<MediaTypeFormatter>)formatters);
        }

        /// <summary>
        /// Bind this parameter from the body using the supplied set of formatters.
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <param name="formatters">formatters to choose from when binding the body</param>
        /// <returns>a binding</returns>
        public static HttpParameterBinding BindWithFormatter(this HttpParameterDescriptor parameter, IEnumerable<MediaTypeFormatter> formatters)
        {
            HttpControllerDescriptor controllerDescriptor = parameter.ActionDescriptor.ControllerDescriptor;
            IBodyModelValidator validators = controllerDescriptor.ControllerServices.GetBodyModelValidator();
            return new FormatterParameterBinding(parameter, formatters, validators);
        }

        /// <summary>
        /// Bind this parameter from the body using the supplied set of formatters and validator. 
        /// </summary>
        /// <param name="parameter">parameter to provide binding for.</param>
        /// <param name="formatters">formatters to choose from when binding the body</param>
        /// <param name="bodyModelValidator">a validator. Null to disable validation for this parameter.</param>
        /// <returns>a binding</returns>
        public static HttpParameterBinding BindWithFormatter(this HttpParameterDescriptor parameter, IEnumerable<MediaTypeFormatter> formatters, IBodyModelValidator bodyModelValidator)
        {
            return new FormatterParameterBinding(parameter, formatters, bodyModelValidator);
        }
    }
}