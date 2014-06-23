﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.IO;
using Microsoft.TestCommon;
using Microsoft.Web.UnitTestUtil;
using Moq;

namespace System.Web.Mvc.Html.Test
{
    public class EditorExtensionsTest
    {
        [Fact]
        public void EditorNullExpressionThrows()
        {
            // Act & Assert
            Assert.ThrowsArgumentNull(
                () => MvcHelper.GetHtmlHelper().Editor(expression: null),
                "expression");
        }

        [Theory]
        [PropertyData("AttributeEncodedData", PropertyType = typeof(EncodedDataSets))]
        public void BooleanTemplate_AttributeEncodes_AddedHtmlAttributes(
            string text,
            bool htmlEncode,
            string htmlEncodedText)
        {
            // Arrange
            var expectedResult = "<input attribute=\"" +
                htmlEncodedText +
                "\" checked=\"checked\" class=\"check-box\" id=\"Prefix\" name=\"Prefix\" type=\"checkbox\" " +
                "value=\"true\" />" +
                "<input name=\"Prefix\" type=\"hidden\" value=\"false\" />";
            var viewData = new ViewDataDictionary<bool>(true);
            viewData.Add("htmlAttributes", new { attribute = text, });
            viewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

            var html = MvcHelper.GetHtmlHelper(viewData);

            var viewContext = Mock.Get(html.ViewContext);
            viewContext.Setup(c => c.TempData).Returns(new TempDataDictionary());
            viewContext.Setup(c => c.View).Returns(new DummyView());
            viewContext.Setup(c => c.Writer).Returns(TextWriter.Null);

            string editorResult;
            string editorForResult;
            string editorForModelResult;
            using (new TemplateHelpersSafeScope())
            {
                // Act
                editorResult = html.Editor("").ToHtmlString();
                editorForResult = html.EditorFor(m => m).ToHtmlString();
                editorForModelResult = html.EditorForModel().ToHtmlString();
            }

            // Assert
            Assert.Equal(expectedResult, editorResult);
            Assert.Equal(expectedResult, editorForResult);
            Assert.Equal(expectedResult, editorForModelResult);
        }

        [Theory]
        [PropertyData("ConditionallyHtmlEncodedData", PropertyType = typeof(EncodedDataSets))]
        public void CollectionTemplateWrappingObjectTemplate_EncodesSimpleDisplayTextOfItems_IfHtmlEncode(
            string text,
            bool htmlEncode,
            string expectedResult)
        {
            // Arrange
            var innerModel = new ObjectTemplateModel
            {
                Property1 = text,           // SimpleDisplayText uses first property by default.
            };
            var model = new[] { innerModel, innerModel, };
            var viewData = new ViewDataDictionary<ObjectTemplateModel[]>(model);
            var html = MvcHelper.GetHtmlHelper(viewData);

            // GetHtmlHelper does not mock enough of the ViewContext for TemplateHelpers use.
            var viewContext = Mock.Get(html.ViewContext);
            viewContext.Setup(c => c.TempData).Returns(new TempDataDictionary());
            viewContext.Setup(c => c.View).Returns(new DummyView());
            viewContext.Setup(c => c.Writer).Returns(TextWriter.Null);

            // Developers might need to do something similar (including MetadataOverrideScope or another approach
            // replacing ModelMetadataProviders.Current) since for example [DisplayFormat] cannot be applied to a class.
            var metadata = ModelMetadataProviders.Current.GetMetadataForType(() => null, typeof(ObjectTemplateModel));
            metadata.HtmlEncode = htmlEncode;

            string editorResult;
            string editorForResult;
            string editorForModelResult;
            using (new TemplateHelpersSafeScope())
            {
                using (new MetadataOverrideScope(metadata))
                {
                    // Act
                    editorResult = html.Editor("").ToHtmlString();
                    editorForResult = html.EditorFor(m => m).ToHtmlString();
                    editorForModelResult = html.EditorForModel().ToHtmlString();
                }
            }

            // Assert
            Assert.Equal(expectedResult + expectedResult, editorResult);
            Assert.Equal(expectedResult + expectedResult, editorForResult);
            Assert.Equal(expectedResult + expectedResult, editorForModelResult);
        }

        // Inconsistent but long-standing behavior.
        [Theory]
        [PropertyData("ConditionallyHtmlEncodedData", PropertyType = typeof(EncodedDataSets))]
        public void CollectionTemplateWrappingObjectTemplate_DoesNotEncodeNullDisplayText_IfNull(
            string text,
            bool htmlEncode,
            string unusedText)
        {
            // Arrange
            var model = new[] { (ObjectTemplateModel)null, };
            var viewData = new ViewDataDictionary<ObjectTemplateModel[]>(model);
            var html = MvcHelper.GetHtmlHelper(viewData);

            // GetHtmlHelper does not mock enough of the ViewContext for TemplateHelpers use.
            var viewContext = Mock.Get(html.ViewContext);
            viewContext.Setup(c => c.TempData).Returns(new TempDataDictionary());
            viewContext.Setup(c => c.View).Returns(new DummyView());
            viewContext.Setup(c => c.Writer).Returns(TextWriter.Null);

            // Developers might need to do something similar (including MetadataOverrideScope or another approach
            // replacing ModelMetadataProviders.Current) since for example [DisplayFormat] cannot be applied to a class.
            var metadata = ModelMetadataProviders.Current.GetMetadataForType(() => null, typeof(ObjectTemplateModel));
            metadata.HtmlEncode = htmlEncode;
            metadata.NullDisplayText = text;

            string editorResult;
            string editorForResult;
            string editorForModelResult;
            using (new TemplateHelpersSafeScope())
            {
                using (new MetadataOverrideScope(metadata))
                {
                    // Act
                    editorResult = html.Editor("").ToHtmlString();
                    editorForResult = html.EditorFor(m => m).ToHtmlString();
                    editorForModelResult = html.EditorForModel().ToHtmlString();
                }
            }

            // Assert
            Assert.Equal(text, editorResult);
            Assert.Equal(text, editorForResult);
            Assert.Equal(text, editorForModelResult);
        }

        [Theory]
        [PropertyData("AttributeEncodedData", PropertyType = typeof(EncodedDataSets))]
        public void StringTemplate_AttributeEncodes_AddedHtmlAttributes(
            string text,
            bool htmlEncode,
            string htmlEncodedText)
        {
            // Arrange
            var expectedResult = "<input attribute=\"" +
                htmlEncodedText +
                "\" class=\"text-box single-line\" id=\"Prefix\" name=\"Prefix\" type=\"text\" value=\"string\" />";
            var viewData = new ViewDataDictionary<string>("string");
            viewData.Add("htmlAttributes", new { attribute = text, });
            viewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

            var html = MvcHelper.GetHtmlHelper(viewData);

            var viewContext = Mock.Get(html.ViewContext);
            viewContext.Setup(c => c.TempData).Returns(new TempDataDictionary());
            viewContext.Setup(c => c.View).Returns(new DummyView());
            viewContext.Setup(c => c.Writer).Returns(TextWriter.Null);

            string editorResult;
            string editorForResult;
            string editorForModelResult;
            using (new TemplateHelpersSafeScope())
            {
                // Act
                editorResult = html.Editor("").ToHtmlString();
                editorForResult = html.EditorFor(m => m).ToHtmlString();
                editorForModelResult = html.EditorForModel().ToHtmlString();
            }

            // Assert
            Assert.Equal(expectedResult, editorResult);
            Assert.Equal(expectedResult, editorForResult);
            Assert.Equal(expectedResult, editorForModelResult);
        }

        [Theory]
        [PropertyData("AttributeEncodedData", PropertyType = typeof(EncodedDataSets))]
        public void StringTemplate_AttributeEncodesText(
            string text,
            bool htmlEncode,
            string htmlEncodedText)
        {
            // Arrange
            var expectedResult =
                "<input class=\"text-box single-line\" id=\"Prefix\" name=\"Prefix\" type=\"text\" value=\"" +
                    htmlEncodedText +
                    "\" />";
            var viewData = new ViewDataDictionary<string>(text);
            viewData.TemplateInfo.HtmlFieldPrefix = "Prefix";

            var html = MvcHelper.GetHtmlHelper(viewData);

            var viewContext = Mock.Get(html.ViewContext);
            viewContext.Setup(c => c.TempData).Returns(new TempDataDictionary());
            viewContext.Setup(c => c.View).Returns(new DummyView());
            viewContext.Setup(c => c.Writer).Returns(TextWriter.Null);

            string editorResult;
            string editorForResult;
            string editorForModelResult;
            using (new TemplateHelpersSafeScope())
            {
                // Act
                editorResult = html.Editor("").ToHtmlString();
                editorForResult = html.EditorFor(m => m).ToHtmlString();
                editorForModelResult = html.EditorForModel().ToHtmlString();
            }

            // Assert
            Assert.Equal(expectedResult, editorResult);
            Assert.Equal(expectedResult, editorForResult);
            Assert.Equal(expectedResult, editorForModelResult);
        }

        private class ObjectTemplateModel
        {
            public string Property1 { get; set; }
            public string Property2 { get; set; }
        }

        private class DummyView : IView
        {
            public void Render(ViewContext viewContext, TextWriter writer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
