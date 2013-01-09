﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Builder.TestModels;
using System.Web.Http.OData.Routing;
using System.Xml.Linq;
using Microsoft.Data.Edm;
using Microsoft.TestCommon;
using Newtonsoft.Json.Linq;

namespace System.Web.Http.OData.Formatter
{
    public class InheritanceTests
    {
        HttpConfiguration _configuration;
        HttpServer _server;
        HttpClient _client;
        XNamespace _atomNamespace = "http://www.w3.org/2005/Atom";
        IEdmModel _model;

        public InheritanceTests()
        {
            _configuration = new HttpConfiguration();
            _model = GetEdmModel();
            IEnumerable<ODataMediaTypeFormatter> formatters = ODataMediaTypeFormatters.Create();

            _configuration.Formatters.Clear();
            _configuration.Formatters.AddRange(formatters);

            _configuration.AddFakeODataRoute();

            _configuration.Routes.MapHttpRoute("default", "{action}", new { Controller = "Inheritance" });

            _server = new HttpServer(_configuration);
            _client = new HttpClient(_server);
        }

        [Fact]
        public void Action_Can_Return_Entity_In_Inheritance()
        {
            HttpResponseMessage response = _client.SendAsync(GetODataRequest("http://localhost/GetMotorcycleAsVehicle")).Result;
            response.EnsureSuccessStatusCode();

            dynamic result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            result = result.d;
            ValidateMotorcycle(result);
        }

        [Fact]
        public void Action_Can_Return_Car_As_vehicle()
        {
            HttpResponseMessage response = _client.SendAsync(GetODataRequest("http://localhost/GetCarAsVehicle")).Result;
            response.EnsureSuccessStatusCode();

            dynamic result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            result = result.d;
            ValidateCar(result);
        }

        [Fact]
        public void Action_Can_Return_ClrType_NotInModel()
        {
            HttpResponseMessage response = _client.SendAsync(GetODataRequest("http://localhost/GetSportBikeAsVehicle")).Result;
            response.EnsureSuccessStatusCode();

            dynamic result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            result = result.d;
            ValidateSportbike(result);
        }

        [Fact]
        public void Action_Can_Return_CollectionOfEntities()
        {
            HttpResponseMessage response = _client.SendAsync(GetODataRequest("http://localhost/GetVehicles")).Result;
            response.EnsureSuccessStatusCode();

            dynamic result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            result = result.d;

            ValidateMotorcycle(result.results[0]);
            ValidateCar(result.results[1]);
            ValidateSportbike(result.results[2]);
        }

        [Fact]
        public void Action_Can_Take_Entity_In_Inheritance()
        {
            Stream body = GetResponseStream("http://localhost/GetMotorcycleAsVehicle", "application/atom+xml");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/PostMotorcycle_When_Expecting_Motorcycle");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata=verbose"));
            AddRequestInfo(request);
            request.Content = new StreamContent(body);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/atom+xml");

            HttpResponseMessage response = _client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            dynamic result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            result = result.d;

            ValidateMotorcycle(result);
        }

        [Fact]
        public void Can_Patch_Entity_In_Inheritance()
        {
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), "http://localhost/PatchMotorcycle_When_Expecting_Motorcycle");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata=verbose"));
            AddRequestInfo(request);
            request.Content = new StringContent("{ 'CanDoAWheelie' : false }");
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("application/json;odata=verbose");

            HttpResponseMessage response = _client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            dynamic result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            result = result.d;

            Assert.False((bool)result.CanDoAWheelie);
        }

        [Fact]
        public void Can_Post_DerivedType_To_Action_Expecting_BaseType()
        {
            Stream body = GetResponseStream("http://localhost/GetMotorcycleAsVehicle", "application/atom+xml");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/PostMotorcycle_When_Expecting_Vehicle");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata=verbose"));
            AddRequestInfo(request);
            request.Content = new StreamContent(body);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/atom+xml");

            HttpResponseMessage response = _client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            dynamic result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            result = result.d;

            ValidateMotorcycle(result);
        }

        [Fact]
        public void Can_Patch_DerivedType_To_Action_Expecting_BaseType()
        {
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), "http://localhost/PatchMotorcycle_When_Expecting_Vehicle");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata=verbose"));
            AddRequestInfo(request);
            request.Content = new StringContent("{ '__metadata': { 'type': 'System.Web.Http.OData.Builder.TestModels.Motorcycle' }, 'CanDoAWheelie' : false }");
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("application/json;odata=verbose");

            HttpResponseMessage response = _client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();

            dynamic result = JObject.Parse(response.Content.ReadAsStringAsync().Result);
            result = result.d;

            Assert.False((bool)result.CanDoAWheelie);
        }

        [Fact]
        public void Posting_NonDerivedType_To_Action_Expecting_BaseType_Throws()
        {
            Stream body = GetResponseStream("http://localhost/GetMotorcycleAsVehicle", "application/atom+xml");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/PostMotorcycle_When_Expecting_Car");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata=verbose"));
            AddRequestInfo(request);
            request.Content = new StreamContent(body);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/atom+xml");

            HttpResponseMessage response = _client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            Assert.Contains(
                "An entry with type 'System.Web.Http.OData.Builder.TestModels.Motorcycle' was found, " +
                "but it is not assignable to the expected type 'System.Web.Http.OData.Builder.TestModels.Car'. " +
                "The type specified in the entry must be equal to either the expected type or a derived type.",
                response.Content.ReadAsStringAsync().Result);
        }

        [Fact]
        public void Patch_NonDerivedType_To_Action_Expecting_BaseType_Throws()
        {
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), "http://localhost/PatchMotorcycle_When_Expecting_Car");
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata=verbose"));
            AddRequestInfo(request);
            request.Content = new StringContent("{ '__metadata': { 'type': 'System.Web.Http.OData.Builder.TestModels.Motorcycle' }, 'CanDoAWheelie' : false }");
            request.Content.Headers.ContentType = MediaTypeWithQualityHeaderValue.Parse("application/json;odata=verbose");

            HttpResponseMessage response = _client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            Assert.Contains(
                "An entry with type 'System.Web.Http.OData.Builder.TestModels.Motorcycle' was found, " +
                "but it is not assignable to the expected type 'System.Web.Http.OData.Builder.TestModels.Car'. " +
                "The type specified in the entry must be equal to either the expected type or a derived type.",
                response.Content.ReadAsStringAsync().Result);
        }

        private Stream GetResponseStream(string uri, string contentType)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(contentType));
            AddRequestInfo(request);
            HttpResponseMessage response = _client.SendAsync(request).Result;
            response.EnsureSuccessStatusCode();
            Stream stream = response.Content.ReadAsStreamAsync().Result;

            return stream;
        }

        private static void ValidateMotorcycle(dynamic result)
        {
            Assert.Equal("System.Web.Http.OData.Builder.TestModels.Motorcycle", (string)result.__metadata.type);
            Assert.Equal("sample motorcycle", (string)result.Name);
            Assert.Equal("2009", (string)result.Model);
            Assert.Equal(2, (int)result.WheelCount);
            Assert.Equal(true, (bool)result.CanDoAWheelie);
        }

        private static void ValidateCar(dynamic result)
        {
            Assert.Equal("System.Web.Http.OData.Builder.TestModels.Car", (string)result.__metadata.type);
            Assert.Equal("sample car", (string)result.Name);
            Assert.Equal("2009", (string)result.Model);
            Assert.Equal(4, (int)result.WheelCount);
            Assert.Equal(5, (int)result.SeatingCapacity);
        }

        private static void ValidateSportbike(dynamic result)
        {
            Assert.Equal("System.Web.Http.OData.Builder.TestModels.Motorcycle", (string)result.__metadata.type);
            Assert.Equal("sample sportsbike", (string)result.Name);
            Assert.Equal("2009", (string)result.Model);
            Assert.Equal(2, (int)result.WheelCount);
            Assert.Equal(true, (bool)result.CanDoAWheelie);
            Assert.Null(result.SportBikeProperty_NotVisible);
        }

        private HttpRequestMessage GetODataRequest(string uri)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json;odata=verbose"));
            AddRequestInfo(request);
            return request;
        }

        private void AddRequestInfo(HttpRequestMessage request)
        {
            request.SetODataPath(new DefaultODataPathHandler().Parse(_model, GetODataPath(request.RequestUri.AbsoluteUri)));
            request.SetEdmModel(_model);
        }

        private static IEdmModel GetEdmModel()
        {
            ODataModelBuilder builder = new ODataModelBuilder();

            builder
                .Entity<Vehicle>()
                .HasKey(v => v.Name)
                .HasKey(v => v.Model)
                .Property(v => v.WheelCount);

            builder
                .Entity<Motorcycle>()
                .DerivesFrom<Vehicle>()
                .Property(m => m.CanDoAWheelie);

            builder
                .Entity<Car>()
                .DerivesFrom<Vehicle>()
                .Property(c => c.SeatingCapacity);

            builder.EntitySet<Vehicle>("vehicles").HasIdLink(
                (v) => "http://localhost/vehicles/" + v.EntityInstance.Name, followsConventions: false);
            builder.EntitySet<Motorcycle>("motorcycles").HasIdLink(
                (m) => "http://localhost/motorcycles/" + m.EntityInstance.Name, followsConventions: false);
            builder.EntitySet<Car>("cars");

            builder
                .Action("GetCarAsVehicle")
                .ReturnsFromEntitySet<Vehicle>("vehicles");
            builder
                .Action("GetMotorcycleAsVehicle")
                .ReturnsFromEntitySet<Vehicle>("vehicles");
            builder
                .Action("GetSportBikeAsVehicle")
                .ReturnsFromEntitySet<Vehicle>("vehicles");
            builder
                .Action("GetVehicles")
                .ReturnsFromEntitySet<Vehicle>("vehicles");
            builder
                .Action("PatchMotorcycle_When_Expecting_Motorcycle")
                .ReturnsFromEntitySet<Motorcycle>("motorcycles");
            builder
                .Action("PostMotorcycle_When_Expecting_Motorcycle")
                .ReturnsFromEntitySet<Motorcycle>("motorcycles");
            builder
                .Action("PatchMotorcycle_When_Expecting_Vehicle")
                .ReturnsFromEntitySet<Vehicle>("vehicles");
            builder
                .Action("PostMotorcycle_When_Expecting_Vehicle")
                .ReturnsFromEntitySet<Vehicle>("vehicles");
            builder
                .Action("PostMotorcycle_When_Expecting_Car")
                .ReturnsFromEntitySet<Car>("cars");
            builder
                .Action("PatchMotorcycle_When_Expecting_Car")
                .ReturnsFromEntitySet<Car>("cars");

            return builder.GetEdmModel();
        }

        private static string GetODataPath(string url)
        {
            string serverBaseUri = "http://localhost/";
            Assert.True(url.StartsWith(serverBaseUri)); // Guard
            return url.Substring(serverBaseUri.Length);
        }
    }

    [ODataFormatting]
    public class InheritanceController : ApiController
    {
        private Motorcycle motorcycle = new Motorcycle { Model = 2009, Name = "sample motorcycle", CanDoAWheelie = true };
        private Car car = new Car { Model = 2009, Name = "sample car", SeatingCapacity = 5 };
        private SportBike sportBike = new SportBike { Model = 2009, Name = "sample sportsbike", CanDoAWheelie = true, SportBikeProperty_NotVisible = 100 };

        public Vehicle GetMotorcycleAsVehicle()
        {
            return motorcycle;
        }

        public Vehicle GetCarAsVehicle()
        {
            return car;
        }

        public Vehicle GetSportBikeAsVehicle()
        {
            return sportBike;
        }

        public IEnumerable<Vehicle> GetVehicles()
        {
            return new Vehicle[] { motorcycle, car, sportBike };
        }

        public Motorcycle PostMotorcycle_When_Expecting_Motorcycle(Motorcycle motorcycle)
        {
            Assert.IsType<Motorcycle>(motorcycle);
            return motorcycle;
        }

        public Motorcycle PatchMotorcycle_When_Expecting_Motorcycle(Delta<Motorcycle> patch)
        {
            patch.Patch(motorcycle);
            return motorcycle;
        }

        public Motorcycle PutMotorcycle_When_Expecting_Motorcycle(Delta<Motorcycle> patch)
        {
            patch.Put(motorcycle);
            return motorcycle;
        }

        public Vehicle PostMotorcycle_When_Expecting_Vehicle(Vehicle motorcycle)
        {
            Assert.IsType<Motorcycle>(motorcycle);
            return motorcycle;
        }

        public Vehicle PatchMotorcycle_When_Expecting_Vehicle(Delta<Vehicle> patch)
        {
            Assert.IsType<Motorcycle>(patch.GetEntity());
            patch.Patch(motorcycle);
            return motorcycle;
        }

        public string PostMotorcycle_When_Expecting_Car(Car car)
        {
            Assert.Null(car);
            var carErrors = ModelState["car"];
            Assert.NotNull(carErrors);

            return carErrors.Errors[0].Exception.Message;
        }

        public string PatchMotorcycle_When_Expecting_Car(Delta<Car> delta)
        {
            Assert.Null(delta);
            var deltaErrors = ModelState["delta"];
            Assert.NotNull(deltaErrors);

            return deltaErrors.Errors[0].Exception.Message;
        }
    }
}
