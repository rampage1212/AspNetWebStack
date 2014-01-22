﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.OData.Routing
{
    public class BoundFunctionPathSegmentTest
    {
        [Fact]
        public void Ctor_ThrowsArgumentNull_Function()
        {
            // Arrange
            IEdmModel model = new EdmModel();
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            // Act & Assert
            Assert.ThrowsArgumentNull(() => new BoundFunctionPathSegment(function: null, model: model, parameterValues: parameters),
                "function");
        }

        [Fact]
        public void Ctor_TakingFunction_InitializesFunctionProperty()
        {
            // Arrange
            IEdmModel model = new EdmModel();
            Mock<IEdmFunction> edmFunction = new Mock<IEdmFunction>();
            edmFunction.Setup(a => a.Namespace).Returns("NS");
            edmFunction.Setup(a => a.Name).Returns("Function");

            // Act
            BoundFunctionPathSegment functionPathSegment = new BoundFunctionPathSegment(edmFunction.Object, model, null);

            // Assert
            Assert.Same(edmFunction.Object, functionPathSegment.Function);
        }

        [Fact]
        public void Ctor_TakingFunction_InitializesFunctionNameProperty()
        {
            // Arrange
            IEdmModel model = new EdmModel();
            Mock<IEdmFunction> edmFunction = new Mock<IEdmFunction>();
            edmFunction.Setup(a => a.Namespace).Returns("NS");
            edmFunction.Setup(a => a.Name).Returns("Function");

            // Act
            BoundFunctionPathSegment functionPathSegment = new BoundFunctionPathSegment(edmFunction.Object, model, null);

            // Assert
            Assert.Equal("NS.Function", functionPathSegment.FunctionName);
        }

        [Fact]
        public void Property_SegmentKind_IsEntitySet()
        {
            // Arrange
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            BoundFunctionPathSegment segment = new BoundFunctionPathSegment("function", parameters);

            // Act & Assert
            Assert.Equal(ODataSegmentKinds.Function, segment.SegmentKind);
        }

        [Fact]
        public void GetEdmType_Returns_FunctionReturnType()
        {
            // Arrange
            IEdmEntityType returnType = new Mock<IEdmEntityType>().Object;
            EdmFunction function = new EdmFunction("NS", "Function", new EdmEntityTypeReference(returnType, isNullable: false));
            BoundFunctionPathSegment segment = new BoundFunctionPathSegment(function, model: null, parameterValues: null);

            // Act
            var result = segment.GetEdmType(returnType);

            // Assert
            Assert.Same(returnType, result);
        }

        [Fact]
        public void GetEntitySet_Returns_FunctionTargetEntitySet()
        {
            // Arrange
            Mock<IEdmEntitySet> targetEntitySet = new Mock<IEdmEntitySet>();
            Mock<IEdmFunction> edmFuncton = new Mock<IEdmFunction>();
            edmFuncton.Setup(a => a.Namespace).Returns("NS");
            edmFuncton.Setup(a => a.Name).Returns("Funtion");

            // Act
            BoundFunctionPathSegment segment = new BoundFunctionPathSegment(edmFuncton.Object, null, null);

            // Assert
            Assert.Same(targetEntitySet.Object, segment.GetEntitySet(targetEntitySet.Object));
        }

        [Fact]
        public void ToString_ReturnsSameString_Function()
        {
            // Arrange
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("Id", "123");
            parameters.Add("Name", "John");
            BoundFunctionPathSegment segment = new BoundFunctionPathSegment("function", parameters);

            // Act
            string actual = segment.ToString();

            // Assert
            Assert.Equal("function(Id=123,Name=John)", actual);
        }

        [Fact]
        public void GetParameterValue_Returns_FunctionParameterValue()
        {
            // Arrange
            string parameterName = "Parameter";
            EdmModel model = new EdmModel();
            var entityType = new EdmEntityType("NS", "Customer");
            model.AddElement(entityType);

            IEdmTypeReference returnType = EdmCoreModel.Instance.GetPrimitive(EdmPrimitiveTypeKind.Boolean, isNullable: false);
            IEdmTypeReference parameterType = EdmCoreModel.Instance.GetPrimitive(EdmPrimitiveTypeKind.Int32, isNullable: false);
            IEdmTypeReference bindingParamterType = new EdmEntityTypeReference(entityType, isNullable: false);
            EdmFunction function = new EdmFunction("NS", "Function", returnType);
            function.AddParameter("bindingParameter", bindingParamterType);
            function.AddParameter(parameterName, parameterType);
            model.AddElement(function);

            IDictionary<string, string> parameterValues = new Dictionary<string, string>();
            parameterValues.Add(parameterName, "101");

            // Act
            BoundFunctionPathSegment segment = new BoundFunctionPathSegment(function, model, parameterValues);
            var result = segment.GetParameterValue(parameterName);

            // Assert
            Assert.Equal("System.Int32", result.GetType().FullName);
            Assert.Equal("101", result.ToString());
        }

        [Fact]
        public void TryMatch_ReturnsTrue_IfSameFunction()
        {
            // Arrange
            IEdmEntityType returnType = new Mock<IEdmEntityType>().Object;
            EdmFunction function = new EdmFunction("NS", "Function", new EdmEntityTypeReference(returnType, isNullable: false));

            BoundFunctionPathSegment template = new BoundFunctionPathSegment(function, model: null, parameterValues: null);
            BoundFunctionPathSegment segment = new BoundFunctionPathSegment(function, model: null, parameterValues: null);

            // Act
            Dictionary<string, object> values = new Dictionary<string,object>();
            bool result = template.TryMatch(segment, values);

            // Assert
            Assert.True(result);
            Assert.Empty(values);
        }
    }
}
