﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Net.Http;
using System.Web.Http.Hosting;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.TestCommon.Models;
using System.Web.Http.Routing;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.TestCommon;

namespace System.Web.Http.OData.Formatter.Serialization
{
    public class EntityTypeTest
    {
        private IEdmModel _model = GetSampleModel();

        [Fact]
        public void EntityTypeSerializesAsODataEntryForJsonLight()
        {
            EntityTypeSerializesAsODataEntry(BaselineResource.EmployeeEntryInJsonLight, true);
        }

        [Fact]
        public void EntityTypeSerializesAsODataEntryForAtom()
        {
            EntityTypeSerializesAsODataEntry(BaselineResource.EmployeeEntryInAtom, false);
        }

        private void EntityTypeSerializesAsODataEntry(string expectedContent, bool json)
        {
            ODataMediaTypeFormatter formatter = CreateFormatter();
            Employee employee = (Employee)TypeInitializer.GetInstance(SupportedTypes.Employee);
            ObjectContent<Employee> content = new ObjectContent<Employee>(employee, formatter, json ?
                ODataMediaTypes.ApplicationJsonODataMinimalMetadata : ODataMediaTypes.ApplicationAtomXmlTypeEntry);

            string actualContent = content.ReadAsStringAsync().Result;

            if (json)
            {
                JsonAssert.Equal(expectedContent, actualContent);
            }
            else
            {
                RegexReplacement replaceUpdateTime = new RegexReplacement(
                    "<updated>*.*</updated>", "<updated>UpdatedTime</updated>");
                Assert.Xml.Equal(expectedContent, actualContent, replaceUpdateTime);
            }
        }

        private ODataMediaTypeFormatter CreateFormatter()
        {
            ODataMediaTypeFormatter formatter = new ODataMediaTypeFormatter(_model,
                new ODataPayloadKind[] { ODataPayloadKind.Entry }, GetSampleRequest());
            formatter.SupportedMediaTypes.Add(ODataMediaTypes.ApplicationJsonODataMinimalMetadata);
            formatter.SupportedMediaTypes.Add(ODataMediaTypes.ApplicationAtomXmlTypeEntry);
            return formatter;
        }

        private HttpRequestMessage GetSampleRequest()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/employees");
            HttpConfiguration config = new HttpConfiguration();
            config.EnableOData(_model);
            request.Properties[HttpPropertyKeys.HttpConfigurationKey] = config;
            request.Properties[HttpPropertyKeys.HttpRouteDataKey] = new HttpRouteData(new HttpRoute());
            request.Properties["MS_ODataPath"] = new DefaultODataPathHandler(_model).Parse("employees");
            return request;
        }

        private static IEdmModel GetSampleModel()
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Employee>("employees");
            builder.EntitySet<WorkItem>("workitems");
            return builder.GetEdmModel();
        }
    }
}
