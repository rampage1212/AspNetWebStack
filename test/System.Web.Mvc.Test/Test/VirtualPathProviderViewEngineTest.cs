﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Routing;
using System.Web.WebPages;
using Microsoft.TestCommon;
using Moq;

namespace System.Web.Mvc.Test
{
    [CLSCompliant(false)]
    public class VirtualPathProviderViewEngineTest : IDisposable
    {
        private ControllerContext _context = CreateContext();
        private ControllerContext _mobileContext = CreateContext(isMobileDevice: true);
        private TestableVirtualPathProviderViewEngine _engine = new TestableVirtualPathProviderViewEngine();

        public void Dispose()
        {
            _engine.MockPathProvider.Verify();
            _engine.MockCache.Verify();
        }

        [Fact]
        public void FindView_NullControllerContext_Throws()
        {
            // Act & Assert
            Assert.ThrowsArgumentNull(
                () => _engine.FindView(null, "view name", null, false),
                "controllerContext"
                );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FindView_InvalidViewName_Throws(string viewName)
        {
            // Act & Assert
            Assert.ThrowsArgumentNullOrEmpty(
                () => _engine.FindView(_context, viewName, null, false),
                "viewName"
                );
        }

        [Fact]
        public void FindView_ControllerNameNotInRequestContext_Throws()
        {
            // Arrange
            _context.RouteData.Values.Remove("controller");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => _engine.FindView(_context, "viewName", null, false),
                "The RouteData must contain an item named 'controller' with a non-empty string value."
                );
        }

        [Fact]
        public void FindView_EmptyViewLocations_Throws()
        {
            // Arrange
            _engine.ClearViewLocations();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => _engine.FindView(_context, "viewName", null, false),
                "The property 'ViewLocationFormats' cannot be null or empty."
                );
        }

        [Fact]
        public void FindView_ViewDoesNotExistAndNoMaster_ReturnsSearchedLocationsResult()
        {
            // Arrange
            SetupFileDoesNotExist("~/vpath/controllerName/viewName.view");

            // Act
            ViewEngineResult result = _engine.FindView(_context, "viewName", null, false);

            // Assert
            Assert.Null(result.View);
            Assert.Equal("~/vpath/controllerName/viewName.view", Assert.Single(result.SearchedLocations));
        }

        [Fact]
        public void FindView_ViewExistsAndNoMaster_ReturnsView()
        {
            // Arrange
            _engine.ClearMasterLocations(); // If master is not provided, master locations can be empty

            SetupFileExists("~/vpath/controllerName/viewName.view");
            SetupCacheHit("~/vpath/controllerName/viewName.view");

            SetupFileDoesNotExist("~/vpath/controllerName/viewName.Mobile.view");

            // Act
            ViewEngineResult result = _engine.FindView(_context, "viewName", null, false);

            // Assert
            TestView view = Assert.IsType<TestView>(result.View);
            Assert.Null(result.SearchedLocations);
            Assert.Same(_context, view.ControllerContext);
            Assert.Equal("~/vpath/controllerName/viewName.view", view.Path);
            Assert.Equal(String.Empty, view.MasterPath);
        }

        [Theory]
        [InlineData("~/foo/bar.view")]
        [InlineData("/foo/bar.view")]
        public void FindView_PathViewExistsAndNoMaster_ReturnsView(string path)
        {
            // Arrange
            _engine.ClearMasterLocations();

            SetupFileExists(path);
            SetupCacheHit(path);

            // Act
            ViewEngineResult result = _engine.FindView(_context, path, null, false);

            // Assert
            TestView view = Assert.IsType<TestView>(result.View);
            Assert.Null(result.SearchedLocations);
            Assert.Same(_context, view.ControllerContext);
            Assert.Equal(path, view.Path);
            Assert.Equal(String.Empty, view.MasterPath);
        }

        [Theory]
        [InlineData("~/foo/bar.unsupported")]
        [InlineData("/foo/bar.unsupported")]
        public void FindView_PathViewExistsAndNoMaster_Legacy_ReturnsView(string path)
        {
            // Arrange
            _engine.FileExtensions = null; // Set FileExtensions to null to simulate View Engines that do not set this property            
            _engine.ClearMasterLocations();

            SetupFileExists(path);
            SetupCacheHit(path);

            // Act
            ViewEngineResult result = _engine.FindView(_context, path, null, false);

            // Assert
            TestView view = Assert.IsType<TestView>(result.View);
            Assert.Null(result.SearchedLocations);
            Assert.Same(_context, view.ControllerContext);
            Assert.Equal(path, view.Path);
            Assert.Equal(String.Empty, view.MasterPath);
        }

        [Theory]
        [InlineData("~/foo/bar.view")]
        [InlineData("/foo/bar.view")]
        public void FindView_PathViewDoesNotExistAndNoMaster_ReturnsSearchedLocationsResult(string path)
        {
            // Arrange
            SetupFileDoesNotExist(path);
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), ""))
                .Verifiable();

            // Act
            ViewEngineResult result = _engine.FindView(_context, path, null, false);

            // Assert
            Assert.Null(result.View);
            Assert.Equal(path, Assert.Single(result.SearchedLocations));
        }

        [Theory]
        [InlineData("~/foo/bar.unsupported")]
        [InlineData("/foo/bar.unsupported")]
        public void FindView_PathViewNotSupportedAndNoMaster_ReturnsSearchedLocationsResult(string path)
        {
            // Arrange
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), ""))
                .Verifiable();

            // Act
            ViewEngineResult result = _engine.FindView(_context, path, null, false);

            // Assert
            Assert.Null(result.View);
            Assert.Equal(path, Assert.Single(result.SearchedLocations));
            _engine.MockPathProvider.Verify(vpp => vpp.FileExists(path), Times.Never());
        }

        [Fact]
        public void FindView_ViewExistsAndMasterNameProvidedButEmptyMasterLocations_Throws()
        {
            // Arrange
            _engine.ClearMasterLocations();
            SetupFileExists("~/vpath/controllerName/viewName.view");
            SetupCacheHit("~/vpath/controllerName/viewName.view");

            SetupFileDoesNotExist("~/vpath/controllerName/viewName.Mobile.view");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => _engine.FindView(_context, "viewName", "masterName", false),
                "The property 'MasterLocationFormats' cannot be null or empty."
                );
        }

        [Fact]
        public void FindView_ViewDoesNotExistAndMasterDoesNotExist_ReturnsSearchedLocationsResult()
        {
            // Arrange
            SetupFileDoesNotExist("~/vpath/controllerName/viewName.view");
            SetupFileDoesNotExist("~/vpath/controllerName/masterName.master");

            // Act
            ViewEngineResult result = _engine.FindView(_context, "viewName", "masterName", false);

            // Assert
            Assert.Null(result.View);
            Assert.Equal(2, result.SearchedLocations.Count()); // Both view and master locations
            Assert.True(result.SearchedLocations.Contains("~/vpath/controllerName/viewName.view"));
            Assert.True(result.SearchedLocations.Contains("~/vpath/controllerName/masterName.master"));
        }

        [Fact]
        public void FindView_ViewExistsButMasterDoesNotExist_ReturnsSearchedLocationsResult()
        {
            // Arrange
            SetupFileExists("~/vpath/controllerName/viewName.view");
            SetupCacheHit("~/vpath/controllerName/viewName.view");
            SetupFileDoesNotExist("~/vpath/controllerName/viewName.Mobile.view");
            SetupFileDoesNotExist("~/vpath/controllerName/masterName.master");

            // Act
            ViewEngineResult result = _engine.FindView(_context, "viewName", "masterName", false);

            // Assert
            Assert.Null(result.View);
            // View was found, not included in 'searched locations'
            Assert.Equal("~/vpath/controllerName/masterName.master", Assert.Single(result.SearchedLocations));
        }

        [Fact]
        public void FindView_MasterInAreaDoesNotExist_ReturnsSearchedLocationsResult()
        {
            // Arrange
            _context.RouteData.DataTokens["area"] = "areaName";
            SetupFileExists("~/vpath/areaName/controllerName/viewName.view");
            SetupCacheHit("~/vpath/areaName/controllerName/viewName.view");
            SetupFileDoesNotExist("~/vpath/areaName/controllerName/viewName.Mobile.view");
            SetupFileDoesNotExist("~/vpath/areaName/controllerName/masterName.master");
            SetupFileDoesNotExist("~/vpath/controllerName/masterName.master");

            // Act
            ViewEngineResult result = _engine.FindView(_context, "viewName", "masterName", false);

            // Assert
            Assert.Null(result.View);
            Assert.Equal(2, result.SearchedLocations.Count()); // View was found, not included in 'searched locations'
            Assert.True(result.SearchedLocations.Contains("~/vpath/areaName/controllerName/masterName.master"));
            Assert.True(result.SearchedLocations.Contains("~/vpath/controllerName/masterName.master"));
        }

        [Fact]
        public void FindView_ViewExistsAndMasterExists_ReturnsView()
        {
            // Arrange
            SetupFileExists("~/vpath/controllerName/viewName.view");
            SetupCacheHit("~/vpath/controllerName/viewName.view");

            SetupFileDoesNotExist("~/vpath/controllerName/viewName.Mobile.view");

            SetupFileExists("~/vpath/controllerName/masterName.master");
            SetupCacheHit("~/vpath/controllerName/masterName.master");

            SetupFileDoesNotExist("~/vpath/controllerName/masterName.Mobile.master");

            // Act
            ViewEngineResult result = _engine.FindView(_context, "viewName", "masterName", false);

            // Assert
            TestView view = Assert.IsType<TestView>(result.View);
            Assert.Null(result.SearchedLocations);
            Assert.Same(_context, view.ControllerContext);
            Assert.Equal("~/vpath/controllerName/viewName.view", view.Path);
            Assert.Equal("~/vpath/controllerName/masterName.master", view.MasterPath);
        }

        [Fact]
        public void FindView_ViewInAreaExistsAndMasterExists_ReturnsView()
        {
            // Arrange
            _context.RouteData.DataTokens["area"] = "areaName";
            SetupFileExists("~/vpath/areaName/controllerName/viewName.view");
            SetupCacheHit("~/vpath/areaName/controllerName/viewName.view");

            SetupFileDoesNotExist("~/vpath/areaName/controllerName/viewName.Mobile.view");
            
            SetupFileDoesNotExist("~/vpath/areaName/controllerName/masterName.master");

            SetupFileExists("~/vpath/controllerName/masterName.master");
            SetupCacheHit("~/vpath/controllerName/masterName.master");
            
            SetupFileDoesNotExist("~/vpath/controllerName/masterName.Mobile.master");

            // Act
            ViewEngineResult result = _engine.FindView(_context, "viewName", "masterName", false);

            // Assert
            TestView view = Assert.IsType<TestView>(result.View);
            Assert.Null(result.SearchedLocations);
            Assert.Same(_context, view.ControllerContext);
            Assert.Equal("~/vpath/areaName/controllerName/viewName.view", view.Path);
            Assert.Equal("~/vpath/controllerName/masterName.master", view.MasterPath);
        }

        [Fact]
        public void FindView_ViewInAreaExistsAndMasterExists_ReturnsView_Mobile()
        {
            // Arrange
            _mobileContext.RouteData.DataTokens["area"] = "areaName";
            SetupFileExists("~/vpath/areaName/controllerName/viewName.view");
            SetupCacheHit("~/vpath/areaName/controllerName/viewName.view");

            SetupFileExists("~/vpath/areaName/controllerName/viewName.Mobile.view");
            SetupCacheHit("~/vpath/areaName/controllerName/viewName.Mobile.view");
            
            SetupFileDoesNotExist("~/vpath/areaName/controllerName/masterName.master");
            SetupFileDoesNotExist("~/vpath/areaName/controllerName/masterName.Mobile.master");
            
            SetupFileExists("~/vpath/controllerName/masterName.master");
            SetupCacheHit("~/vpath/controllerName/masterName.master");

            SetupFileExists("~/vpath/controllerName/masterName.Mobile.master");
            SetupCacheHit("~/vpath/controllerName/masterName.Mobile.master");

            // Act
            ViewEngineResult result = _engine.FindView(_mobileContext, "viewName", "masterName", false);

            // Assert
            TestView view = Assert.IsType<TestView>(result.View);
            Assert.Null(result.SearchedLocations);
            Assert.Same(_mobileContext, view.ControllerContext);
            Assert.Equal("~/vpath/areaName/controllerName/viewName.Mobile.view", view.Path);
            Assert.Equal("~/vpath/controllerName/masterName.Mobile.master", view.MasterPath);
        }

        [Fact]
        public void FindPartialView_NullControllerContext_Throws()
        {
            // Act & Assert
            Assert.ThrowsArgumentNull(
                () => _engine.FindPartialView(null, "view name", false),
                "controllerContext"
                );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FindPartialView_InvalidPartialViewName_Throws(string partialViewName)
        {
            // Act & Assert
            Assert.ThrowsArgumentNullOrEmpty(
                () => _engine.FindPartialView(_context, partialViewName, false),
                "partialViewName"
                );
        }

        [Fact]
        public void FindPartialView_ControllerNameNotInRequestContext_Throws()
        {
            // Arrange
            _context.RouteData.Values.Remove("controller");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => _engine.FindPartialView(_context, "partialName", false),
                "The RouteData must contain an item named 'controller' with a non-empty string value."
                );
        }

        [Fact]
        public void FindPartialView_EmptyPartialViewLocations_Throws()
        {
            // Arrange
            _engine.ClearPartialViewLocations();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                () => _engine.FindPartialView(_context, "partialName", false),
                "The property 'PartialViewLocationFormats' cannot be null or empty."
                );
        }

        [Fact]
        public void FindPartialView_ViewDoesNotExist_ReturnsSearchLocationsResult()
        {
            // Arrange
            SetupFileDoesNotExist("~/vpath/controllerName/partialName.partial");

            // Act
            ViewEngineResult result = _engine.FindPartialView(_context, "partialName", false);

            // Assert
            Assert.Null(result.View);
            Assert.Equal("~/vpath/controllerName/partialName.partial", Assert.Single(result.SearchedLocations));
        }

        [Fact]
        public void FindPartialView_ViewExists_ReturnsView()
        {
            // Arrange
            SetupFileExists("~/vpath/controllerName/partialName.partial");
            SetupCacheHit("~/vpath/controllerName/partialName.partial");

            SetupFileDoesNotExist("~/vpath/controllerName/partialName.Mobile.partial");

            // Act
            ViewEngineResult result = _engine.FindPartialView(_context, "partialName", false);

            // Assert
            TestView view = Assert.IsType<TestView>(result.View);
            Assert.Null(result.SearchedLocations);
            Assert.Same(_context, view.ControllerContext);
            Assert.Equal("~/vpath/controllerName/partialName.partial", view.Path);
        }

        [Theory]
        [InlineData("~/foo/bar.partial")]
        [InlineData("/foo/bar.partial")]
        public void FindPartialView_PathViewExists_ReturnsView(string path)
        {
            // Arrange
            SetupFileExists(path);
            SetupCacheHit(path);

            // Act
            ViewEngineResult result = _engine.FindPartialView(_context, path, false);

            // Assert
            TestView view = Assert.IsType<TestView>(result.View);
            Assert.Null(result.SearchedLocations);
            Assert.Same(_context, view.ControllerContext);
            Assert.Equal(path, view.Path);
        }

        [Theory]
        [InlineData("/foo/bar.unsupported")]
        [InlineData("~/foo/bar.unsupported")]
        public void FindPartialView_PathViewExists_Legacy_ReturnsView(string path)
        {
            // Arrange
            _engine.FileExtensions = null; // Set FileExtensions to null to simulate View Engines that do not set this property
            SetupFileExists(path);
            SetupCacheHit(path);

            // Act
            ViewEngineResult result = _engine.FindPartialView(_context, path, false);

            // Assert
            TestView view = Assert.IsType<TestView>(result.View);
            Assert.Null(result.SearchedLocations);
            Assert.Same(_context, view.ControllerContext);
            Assert.Equal(path, view.Path);
        }

        [Theory]
        [InlineData("~/foo/bar.partial")]
        [InlineData("/foo/bar.partial")]
        public void FindPartialView_PathViewDoesNotExist_ReturnsSearchedLocationsResult(string path)
        {
            // Arrange
            SetupFileDoesNotExist(path);
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), ""))
                .Verifiable();

            // Act
            ViewEngineResult result = _engine.FindPartialView(_context, path, false);

            // Assert
            Assert.Null(result.View);
            Assert.Equal(path, Assert.Single(result.SearchedLocations));
        }

        [Theory]
        [InlineData("~/foo/bar.unsupported")]
        [InlineData("/foo/bar.unsupported")]
        public void FindPartialView_PathViewNotSupported_ReturnsSearchedLocationsResult(string path)
        {
            // Arrange
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), ""))
                .Verifiable();

            // Act
            ViewEngineResult result = _engine.FindPartialView(_context, path, false);

            // Assert
            Assert.Null(result.View);
            Assert.Equal(path, Assert.Single(result.SearchedLocations));
            _engine.MockPathProvider.Verify(vpp => vpp.FileExists(path), Times.Never());
        }

        [Fact]
        public void FileExtensions()
        {
            // Arrange + Assert
            Assert.Null(new Mock<VirtualPathProviderViewEngine>().Object.FileExtensions);
        }

        [Fact]
        public void GetExtensionThunk()
        {
            // Arrange and Assert
            Assert.Equal(VirtualPathUtility.GetExtension, new Mock<VirtualPathProviderViewEngine>().Object.GetExtensionThunk);
        }

        [Fact]
        public void DisplayModeSetOncePerRequest()
        {
            // Arrange
            SetupFileExists("~/vpath/controllerName/viewName.view");
            SetupCacheHit("~/vpath/controllerName/viewName.view");

            SetupFileDoesNotExist("~/vpath/controllerName/viewName.Mobile.view");

            SetupFileDoesNotExist("~/vpath/controllerName/partialName.partial");

            SetupFileExists("~/vpath/controllerName/partialName.Mobile.partial");
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), "~/vpath/controllerName/partialName.Mobile.partial"))
                .Callback<HttpContextBase, string, string>((httpContext, key, virtualPath) =>
                {
                    _engine.MockCache
                        .Setup(c => c.GetViewLocation(It.IsAny<HttpContextBase>(), key))
                        .Returns("~/vpath/controllerName/partialName.Mobile.partial")
                        .Verifiable();
                })
                .Verifiable();

            // Act
            ViewEngineResult viewResult = _engine.FindView(_mobileContext, "viewName", masterName: null, useCache: false);

            // Mobile display mode will be used to locate the view with and without the cache.
            // In neither case should this set the DisplayModeId to Mobile because it has already been set.
            ViewEngineResult partialResult = _engine.FindPartialView(_mobileContext, "partialName", useCache: false);
            ViewEngineResult cachedPartialResult = _engine.FindPartialView(_mobileContext, "partialName", useCache: true);

            // Assert

            Assert.Equal(DisplayModeProvider.DefaultDisplayModeId, _mobileContext.DisplayMode.DisplayModeId);
        }

        // The core caching scenarios are covered in the FindView/FindPartialView tests. These
        // extra tests deal with the cache itself, rather than specifics around finding views.

        private const string MASTER_VIRTUAL = "~/vpath/controllerName/name.master";
        private const string PARTIAL_VIRTUAL = "~/vpath/controllerName/name.partial";
        private const string VIEW_VIRTUAL = "~/vpath/controllerName/name.view";
        private const string MOBILE_VIEW_VIRTUAL = "~/vpath/controllerName/name.Mobile.view";

        [Fact]
        public void UsesDifferentKeysForViewMasterAndPartial()
        {
            string keyMaster = null;
            string keyPartial = null;
            string keyView = null;

            // Arrange
            SetupFileExists(VIEW_VIRTUAL);
            SetupFileDoesNotExist(MOBILE_VIEW_VIRTUAL);
            SetupFileExists(MASTER_VIRTUAL);
            SetupFileDoesNotExist("~/vpath/controllerName/name.Mobile.master");
            SetupFileExists(PARTIAL_VIRTUAL);
            SetupFileDoesNotExist("~/vpath/controllerName/name.Mobile.partial");
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), VIEW_VIRTUAL))
                .Callback<HttpContextBase, string, string>((httpContext, key, path) => keyView = key)
                .Verifiable();
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), MASTER_VIRTUAL))
                .Callback<HttpContextBase, string, string>((httpContext, key, path) => keyMaster = key)
                .Verifiable();
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), PARTIAL_VIRTUAL))
                .Callback<HttpContextBase, string, string>((httpContext, key, path) => keyPartial = key)
                .Verifiable();

            // Act
            _engine.FindView(_context, "name", "name", false);
            _engine.FindPartialView(_context, "name", false);

            // Assert
            Assert.NotNull(keyMaster);
            Assert.NotNull(keyPartial);
            Assert.NotNull(keyView);
            Assert.NotEqual(keyMaster, keyPartial);
            Assert.NotEqual(keyMaster, keyView);
            Assert.NotEqual(keyPartial, keyView);
            _engine.MockPathProvider
                .Verify(vpp => vpp.FileExists(VIEW_VIRTUAL), Times.AtMostOnce());
            _engine.MockPathProvider
                .Verify(vpp => vpp.FileExists(MASTER_VIRTUAL), Times.AtMostOnce());
            _engine.MockPathProvider
                .Verify(vpp => vpp.FileExists(PARTIAL_VIRTUAL), Times.AtMostOnce());
            _engine.MockCache
                .Verify(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), VIEW_VIRTUAL), Times.AtMostOnce());
            _engine.MockCache
                .Verify(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), MASTER_VIRTUAL), Times.AtMostOnce());
            _engine.MockCache
                .Verify(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), PARTIAL_VIRTUAL), Times.AtMostOnce());
        }

        // This tests the protocol involved with two calls to FindView for the same view name
        // where the request succeeds. The calls happen in this order:
        //
        //    FindView("view")
        //      Cache.GetViewLocation(key for "view") -> returns null (not found)
        //      VirtualPathProvider.FileExists(virtual path for "view") -> returns true
        //      Cache.InsertViewLocation(key for "view", virtual path for "view")
        //    FindView("view")
        //      Cache.GetViewLocation(key for "view") -> returns virtual path for "view"
        //
        // The mocking code is written as it is because we don't want to make any assumptions
        // about the format of the cache key. So we intercept the first call to Cache.GetViewLocation and
        // take the key they gave us to set up the rest of the mock expectations.
        // The ViewCollection class will typically place to successive calls to FindView and FindPartialView and
        // set the useCache parameter to true/false respectively. To simulate this, both calls to FindView are executed
        // with useCache set to true. This mimics the behavior of always going to the cache first and after finding a
        // view, ensuring that subsequent calls from the cache are successful.

        [Fact]
        public void ValueInCacheBypassesVirtualPathProvider()
        {
            // Arrange
            string cacheKey = null;

            SetupFileExists(VIEW_VIRTUAL); // It wasn't found, so they call vpp.FileExists
            SetupFileDoesNotExist(MOBILE_VIEW_VIRTUAL);
            _engine.MockCache // Then they set the value into the cache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), VIEW_VIRTUAL))
                .Callback<HttpContextBase, string, string>((httpContext, key, virtualPath) =>
                {
                    cacheKey = key;
                    _engine.MockCache // Second time through, we give them a cache hit
                        .Setup(c => c.GetViewLocation(It.IsAny<HttpContextBase>(), key))
                        .Returns(VIEW_VIRTUAL)
                        .Verifiable();
                })
                .Verifiable();

            // Act
            _engine.FindView(_context, "name", null, false); // Call it once with false to seed the cache
            _engine.FindView(_context, "name", null, true); // Call it once with true to check the cache

            // Assert

            _engine.MockPathProvider.Verify(vpp => vpp.FileExists(VIEW_VIRTUAL), Times.AtMostOnce());
            _engine.MockCache.Verify(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), VIEW_VIRTUAL), Times.AtMostOnce());
            _engine.MockCache.Verify(c => c.GetViewLocation(It.IsAny<HttpContextBase>(), cacheKey), Times.AtMostOnce());

            // We seed the cache with all possible display modes but since the mobile view does not exist we don't insert it into the cache.
            _engine.MockPathProvider.Verify(vpp => vpp.FileExists(MOBILE_VIEW_VIRTUAL), Times.Exactly(1));
            _engine.MockCache.Verify(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), MOBILE_VIEW_VIRTUAL), Times.Never());
            _engine.MockCache.Verify(c => c.GetViewLocation(It.IsAny<HttpContextBase>(), VirtualPathProviderViewEngine.AppendDisplayModeToCacheKey(cacheKey, DisplayModeProvider.MobileDisplayModeId)), Times.Never());
        }

        [Fact]
        public void ValueInCacheBypassesVirtualPathProviderForAllAvailableDisplayModesForContext()
        {
            // Arrange
            string cacheKey = null;
            string mobileCacheKey = null;

            SetupFileExists(VIEW_VIRTUAL);
            SetupFileExists(MOBILE_VIEW_VIRTUAL);

            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), VIEW_VIRTUAL))
                .Callback<HttpContextBase, string, string>((httpContext, key, virtualPath) =>
                {
                    cacheKey = key;
                    _engine.MockCache
                        .Setup(c => c.GetViewLocation(It.IsAny<HttpContextBase>(), key))
                        .Returns(MOBILE_VIEW_VIRTUAL)
                        .Verifiable();
                })
                .Verifiable();
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), MOBILE_VIEW_VIRTUAL))
                .Callback<HttpContextBase, string, string>((httpContext, key, virtualPath) =>
                {
                    mobileCacheKey = key;
                    _engine.MockCache
                        .Setup(c => c.GetViewLocation(It.IsAny<HttpContextBase>(), key))
                        .Returns(MOBILE_VIEW_VIRTUAL)
                        .Verifiable();
                })
                .Verifiable();

            // Act
            _engine.FindView(_mobileContext, "name", null, false);
            _engine.FindView(_mobileContext, "name", null, true);

            // Assert

            // DefaultDisplayMode with Mobile substitution is cached and hit on the second call to FindView
            _engine.MockPathProvider.Verify(vpp => vpp.FileExists(MOBILE_VIEW_VIRTUAL), Times.AtMostOnce());
            _engine.MockCache.Verify(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), MOBILE_VIEW_VIRTUAL), Times.AtMostOnce());
            _engine.MockCache.Verify(c => c.GetViewLocation(It.IsAny<HttpContextBase>(), VirtualPathProviderViewEngine.AppendDisplayModeToCacheKey(cacheKey, DisplayModeProvider.MobileDisplayModeId)), Times.AtMostOnce());

            _engine.MockPathProvider.Verify(vpp => vpp.FileExists(VIEW_VIRTUAL), Times.AtMostOnce());
            _engine.MockCache.Verify(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), VIEW_VIRTUAL), Times.Exactly(1));

            Assert.NotEqual(cacheKey, mobileCacheKey);

            // Act
            _engine.FindView(_context, "name", null, true);

            // Assert

            // The first call to FindView without a mobile browser results in a cache hit
            _engine.MockPathProvider.Verify(vpp => vpp.FileExists(VIEW_VIRTUAL), Times.AtMostOnce());
            _engine.MockCache.Verify(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), VIEW_VIRTUAL), Times.Exactly(1));
            _engine.MockCache.Verify(c => c.GetViewLocation(It.IsAny<HttpContextBase>(), cacheKey), Times.Exactly(1));
        }

        [Fact]
        public void NoValueInCacheButFileExistsReturnsNullIfUsingCache()
        {
            // Arrange
            _engine.MockCache
                .Setup(c => c.GetViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>()))
                .Returns((string)null)
                .Verifiable();
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), It.IsAny<string>()));

            // Act
            IView viewNotInCacheResult = _engine.FindView(_mobileContext, "name", masterName: null, useCache: true).View;

            // Assert
            Assert.Null(viewNotInCacheResult);

            // On a cache miss we should never check the file system. FindView will be called on a second pass
            // without using the cache.
            _engine.MockPathProvider.Verify(vpp => vpp.FileExists(MOBILE_VIEW_VIRTUAL), Times.Never());
            _engine.MockPathProvider.Verify(vpp => vpp.FileExists(VIEW_VIRTUAL), Times.Never());

            SetupFileExists(MOBILE_VIEW_VIRTUAL);
            SetupFileExists(VIEW_VIRTUAL);

            // Act & Assert

            //At this point the view on disk should be found and cached.
            Assert.NotNull(_engine.FindView(_mobileContext, "name", masterName: null, useCache: false).View);
        }

        [Fact]
        public void ReleaseViewCallsDispose()
        {
            // Arrange
            IView view = new TestView();

            // Act
            _engine.ReleaseView(_context, view);

            // Assert
            Assert.True(((TestView)view).Disposed);
        }

        private static ControllerContext CreateContext(bool isMobileDevice = false)
        {
            RouteData routeData = new RouteData();
            routeData.Values["controller"] = "controllerName";
            routeData.Values["action"] = "actionName";

            Mock<ControllerContext> mockControllerContext = new Mock<ControllerContext>();
            mockControllerContext.Setup(c => c.HttpContext.Request.Browser.IsMobileDevice).Returns(isMobileDevice);
            mockControllerContext.Setup(c => c.HttpContext.Items).Returns(new Hashtable());
            mockControllerContext.Setup(c => c.RouteData).Returns(routeData);

            mockControllerContext.Setup(c => c.HttpContext.Response.Cookies).Returns(new HttpCookieCollection());
            mockControllerContext.Setup(c => c.HttpContext.Request.Cookies).Returns(new HttpCookieCollection());

            return mockControllerContext.Object;
        }

        private void SetupFileExists(string path)
        {
            SetupFileExistsHelper(path, exists: true);
        }

        private void SetupFileDoesNotExist(string path)
        {
            SetupFileExistsHelper(path, exists: false);
        }

        private void SetupCacheHit(string path)
        {
            _engine.MockCache
                .Setup(c => c.InsertViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>(), path))
                .Verifiable();
        }

        private void SetupFileExistsHelper(string path, bool exists)
        {
            _engine.MockPathProvider.Setup(vpp => vpp.FileExists(path)).Returns(exists).Verifiable();
        }

        private class TestView : IView, IDisposable
        {
            public bool Disposed { get; set; }
            public ControllerContext ControllerContext { get; set; }
            public string Path { get; set; }
            public string MasterPath { get; set; }

            void IDisposable.Dispose()
            {
                Disposed = true;
            }

            void IView.Render(ViewContext viewContext, TextWriter writer)
            {
            }
        }

        private class TestableVirtualPathProviderViewEngine : VirtualPathProviderViewEngine
        {
            public Mock<IViewLocationCache> MockCache = new Mock<IViewLocationCache>(MockBehavior.Strict);
            public Mock<VirtualPathProvider> MockPathProvider = new Mock<VirtualPathProvider>(MockBehavior.Strict);

            public TestableVirtualPathProviderViewEngine()
            {
                MasterLocationFormats = new[] { "~/vpath/{1}/{0}.master" };
                ViewLocationFormats = new[] { "~/vpath/{1}/{0}.view" };
                PartialViewLocationFormats = new[] { "~/vpath/{1}/{0}.partial" };
                AreaMasterLocationFormats = new[] { "~/vpath/{2}/{1}/{0}.master" };
                AreaViewLocationFormats = new[] { "~/vpath/{2}/{1}/{0}.view" };
                AreaPartialViewLocationFormats = new[] { "~/vpath/{2}/{1}/{0}.partial" };
                FileExtensions = new[] { "view", "partial", "master" };

                ViewLocationCache = MockCache.Object;
                VirtualPathProvider = MockPathProvider.Object;

                MockCache
                    .Setup(c => c.GetViewLocation(It.IsAny<HttpContextBase>(), It.IsAny<string>()))
                    .Returns((string)null);

                GetExtensionThunk = GetExtension;
            }

            public void ClearViewLocations()
            {
                ViewLocationFormats = new string[0];
            }

            public void ClearMasterLocations()
            {
                MasterLocationFormats = new string[0];
            }

            public void ClearPartialViewLocations()
            {
                PartialViewLocationFormats = new string[0];
            }

            protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
            {
                return new TestView() { ControllerContext = controllerContext, Path = partialPath };
            }

            protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
            {
                return new TestView() { ControllerContext = controllerContext, Path = viewPath, MasterPath = masterPath };
            }

            private static string GetExtension(string virtualPath)
            {
                var extension = virtualPath.Substring(virtualPath.LastIndexOf('.'));
                return extension;
            }
        }
    }
}
