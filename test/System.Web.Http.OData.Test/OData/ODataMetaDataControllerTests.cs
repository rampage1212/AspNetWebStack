﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Hosting;
using System.Web.Http.OData.Formatter;
using System.Web.Http.Tracing;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Csdl;
using Microsoft.Data.Edm.Library;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Http.OData.Builder
{
    public class ODataMetaDataControllerTests
    {
        [Fact]
        public void DollarMetaData_Works_WithoutAcceptHeader()
        {
            HttpServer server = new HttpServer();
            server.Configuration.MapODataRoute(ODataTestUtil.GetEdmModel());

            HttpClient client = new HttpClient(server);
            var response = client.GetAsync("http://localhost/$metadata").Result;

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("application/xml", response.Content.Headers.ContentType.MediaType);
            Assert.Contains("<edmx:Edmx", response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public void GetMetadata_Returns_EdmModelFromRequest()
        {
            IEdmModel model = new EdmModel();

            ODataMetadataController controller = new ODataMetadataController();
            controller.Request = new HttpRequestMessage();
            controller.Request.SetEdmModel(model);

            IEdmModel responseModel = controller.GetMetadata();
            Assert.Equal(model, responseModel);
        }

        [Fact]
        public void GetMetadata_Throws_IfModelIsNotSetOnRequest()
        {
            HttpConfiguration configuration = new HttpConfiguration();
            ODataMetadataController controller = new ODataMetadataController();
            controller.Request = new HttpRequestMessage();

            Assert.Throws<InvalidOperationException>(
                () => controller.GetMetadata(),
                "The request must have an associated EDM model. Consider using the extension method HttpConfiguration.MapODataRoute to register a route that parses the OData URI and attaches the model information.");
        }

        [Fact]
        public void DollarMetaDataWorks_AfterTracingIsEnabled()
        {
            HttpServer server = new HttpServer();
            server.Configuration.MapODataRoute(ODataTestUtil.GetEdmModel());
            server.Configuration.Services.Replace(typeof(ITraceWriter), new Mock<ITraceWriter>().Object);

            HttpClient client = new HttpClient(server);
            var response = client.GetAsync("http://localhost/$metadata").Result;

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("application/xml", response.Content.Headers.ContentType.MediaType);
            Assert.Contains("<edmx:Edmx", response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public void ServiceDocumentWorks_AfterTracingIsEnabled_IfModelIsSetOnConfiguration()
        {
            HttpServer server = new HttpServer();
            server.Configuration.MapODataRoute(ODataTestUtil.GetEdmModel());
            server.Configuration.Services.Replace(typeof(ITraceWriter), new Mock<ITraceWriter>().Object);

            HttpClient client = new HttpClient(server);
            var response = client.GetAsync("http://localhost/").Result;

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("application/xml", response.Content.Headers.ContentType.MediaType);
            Assert.Contains("<workspace>", response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public void Controller_DoesNotAppear_InApiDescriptions()
        {
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute("Default", "{controller}/{action}");
            config.MapODataRoute(new ODataConventionModelBuilder().GetEdmModel());
            var explorer = config.Services.GetApiExplorer();

            var apis = explorer.ApiDescriptions.Select(api => api.ActionDescriptor.ControllerDescriptor.ControllerName);

            Assert.DoesNotContain("ODataMetadata", apis);
        }

        [Fact]
        public void GetMetadata_Doesnot_Change_DataServiceVersion()
        {
            // Arrange
            IEdmModel model = new EdmModel();
            model.SetDataServiceVersion(new Version(0, 42));

            ODataMetadataController controller = new ODataMetadataController();
            controller.Request = new HttpRequestMessage();
            controller.Request.SetEdmModel(model);

            // Act
            IEdmModel controllerModel = controller.GetMetadata();

            // Assert
            Assert.Equal(new Version(0, 42), controllerModel.GetDataServiceVersion());
        }
    }
}
