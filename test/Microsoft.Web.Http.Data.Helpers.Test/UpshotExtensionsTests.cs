// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.Web.Mvc;
using Microsoft.Web.Http.Data.Test;
using Microsoft.Web.UnitTestUtil;
using Xunit;
using Assert = Microsoft.TestCommon.AssertEx;

namespace Microsoft.Web.Http.Data.Helpers.Test
{
    public class UpshotExtensionsTests
    {
        private static readonly string _catalogProductsContext = "<script type='text/javascript'>\nupshot.dataSources = upshot.dataSources || {};\nupshot.metadata({\r\n  \"Microsoft.Web.Http.Data.Test.Models.Product,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"ProductID\"\r\n    ],\r\n    \"fields\": {\r\n      \"Category\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Category,Microsoft.Web.Http.Data.Helpers.Test\"\r\n      },\r\n      \"CategoryID\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"Discontinued\": {\r\n        \"type\": \"System.Boolean,mscorlib\"\r\n      },\r\n      \"Order_Details\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Order_Detail,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true\r\n      },\r\n      \"ProductID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"ProductName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"QuantityPerUnit\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ReorderLevel\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"Supplier\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Supplier,Microsoft.Web.Http.Data.Helpers.Test\"\r\n      },\r\n      \"SupplierID\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"UnitPrice\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"UnitsInStock\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"UnitsOnOrder\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {},\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.Order,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"OrderID\"\r\n    ],\r\n    \"fields\": {\r\n      \"Customer\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Customer,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"association\": {\r\n          \"name\": \"Customer_Orders\",\r\n          \"thisKey\": [\r\n            \"CustomerID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"CustomerID\"\r\n          ],\r\n          \"isForeignKey\": true\r\n        }\r\n      },\r\n      \"CustomerID\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"EmployeeID\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"Freight\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"Order_Details\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Order_Detail,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true,\r\n        \"association\": {\r\n          \"name\": \"Order_Details\",\r\n          \"thisKey\": [\r\n            \"OrderID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"OrderID\"\r\n          ],\r\n          \"isForeignKey\": false\r\n        }\r\n      },\r\n      \"OrderDate\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"OrderID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"RequiredDate\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"ShipAddress\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipCity\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipCountry\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShippedDate\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"Shipper\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Shipper,Microsoft.Web.Http.Data.Helpers.Test\"\r\n      },\r\n      \"ShipPostalCode\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipRegion\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipVia\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {\r\n      \"ShipName\": {\r\n        \"maxlength\": 50\r\n      }\r\n    },\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.Customer,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"CustomerID\"\r\n    ],\r\n    \"fields\": {\r\n      \"Address\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"City\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"CompanyName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ContactName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ContactTitle\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Country\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"CustomerID\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Fax\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Orders\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Order,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true,\r\n        \"association\": {\r\n          \"name\": \"Customer_Orders\",\r\n          \"thisKey\": [\r\n            \"CustomerID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"CustomerID\"\r\n          ],\r\n          \"isForeignKey\": false\r\n        }\r\n      },\r\n      \"Phone\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"PostalCode\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Region\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {},\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.Order_Detail,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"OrderID\",\r\n      \"ProductID\"\r\n    ],\r\n    \"fields\": {\r\n      \"Discount\": {\r\n        \"type\": \"System.Single,mscorlib\"\r\n      },\r\n      \"Order\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Order,Microsoft.Web.Http.Data.Helpers.Test\"\r\n      },\r\n      \"OrderID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"Product\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Product,Microsoft.Web.Http.Data.Helpers.Test\"\r\n      },\r\n      \"ProductID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"Quantity\": {\r\n        \"type\": \"System.Int16,mscorlib\"\r\n      },\r\n      \"UnitPrice\": {\r\n        \"type\": \"System.Decimal,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {},\r\n    \"messages\": {}\r\n  }\r\n});\n\nupshot.dataSources.GetProducts = upshot.RemoteDataSource({\n    providerParameters: { url: \"myUrl\", operationName: \"GetProducts\" },\n    entityType: \"Microsoft.Web.Http.Data.Test.Models.Product,Microsoft.Web.Http.Data.Helpers.Test\",\n    bufferChanges: true,\n    dataContext: undefined,\n    mapping: {}\n});\r\n</script>";
        private static readonly string _northwindProductsContext = "<script type='text/javascript'>\nupshot.dataSources = upshot.dataSources || {};\nupshot.metadata({\r\n  \"Microsoft.Web.Http.Data.Test.Models.EF.Product,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"ProductID\"\r\n    ],\r\n    \"fields\": {\r\n      \"Category\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Category,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"association\": {\r\n          \"name\": \"Category_Product\",\r\n          \"thisKey\": [\r\n            \"CategoryID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"CategoryID\"\r\n          ],\r\n          \"isForeignKey\": true\r\n        }\r\n      },\r\n      \"CategoryID\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"CategoryReference\": {\r\n        \"type\": \"System.Data.Objects.DataClasses.EntityReference`1,System.Data.Entity\"\r\n      },\r\n      \"Discontinued\": {\r\n        \"type\": \"System.Boolean,mscorlib\"\r\n      },\r\n      \"EntityKey\": {\r\n        \"type\": \"System.Data.EntityKey,System.Data.Entity\"\r\n      },\r\n      \"Order_Details\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Order_Detail,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true,\r\n        \"association\": {\r\n          \"name\": \"Product_Order_Detail\",\r\n          \"thisKey\": [\r\n            \"ProductID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"ProductID\"\r\n          ],\r\n          \"isForeignKey\": false\r\n        }\r\n      },\r\n      \"ProductID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"ProductName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"QuantityPerUnit\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ReorderLevel\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"Supplier\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Supplier,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"association\": {\r\n          \"name\": \"Supplier_Product\",\r\n          \"thisKey\": [\r\n            \"SupplierID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"SupplierID\"\r\n          ],\r\n          \"isForeignKey\": true\r\n        }\r\n      },\r\n      \"SupplierID\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"SupplierReference\": {\r\n        \"type\": \"System.Data.Objects.DataClasses.EntityReference`1,System.Data.Entity\"\r\n      },\r\n      \"UnitPrice\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"UnitsInStock\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"UnitsOnOrder\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {\r\n      \"ProductName\": {\r\n        \"required\": true,\r\n        \"maxlength\": 40\r\n      },\r\n      \"QuantityPerUnit\": {\r\n        \"rangelength\": [\r\n          2,\r\n          777\r\n        ]\r\n      },\r\n      \"UnitPrice\": {\r\n        \"range\": [\r\n          0,\r\n          1000000\r\n        ]\r\n      }\r\n    },\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.EF.Category,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"CategoryID\"\r\n    ],\r\n    \"fields\": {\r\n      \"CategoryID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"CategoryName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Description\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"EntityKey\": {\r\n        \"type\": \"System.Data.EntityKey,System.Data.Entity\"\r\n      },\r\n      \"Picture\": {\r\n        \"type\": \"System.Byte,mscorlib\",\r\n        \"array\": true\r\n      },\r\n      \"Products\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Product,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true,\r\n        \"association\": {\r\n          \"name\": \"Category_Product\",\r\n          \"thisKey\": [\r\n            \"CategoryID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"CategoryID\"\r\n          ],\r\n          \"isForeignKey\": false\r\n        }\r\n      }\r\n    },\r\n    \"rules\": {\r\n      \"CategoryName\": {\r\n        \"required\": true,\r\n        \"maxlength\": 15\r\n      }\r\n    },\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.EF.Order_Detail,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"OrderID\",\r\n      \"ProductID\"\r\n    ],\r\n    \"fields\": {\r\n      \"Discount\": {\r\n        \"type\": \"System.Single,mscorlib\"\r\n      },\r\n      \"EntityKey\": {\r\n        \"type\": \"System.Data.EntityKey,System.Data.Entity\"\r\n      },\r\n      \"Order\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Order,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"association\": {\r\n          \"name\": \"Order_Order_Detail\",\r\n          \"thisKey\": [\r\n            \"OrderID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"OrderID\"\r\n          ],\r\n          \"isForeignKey\": true\r\n        }\r\n      },\r\n      \"OrderID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"OrderReference\": {\r\n        \"type\": \"System.Data.Objects.DataClasses.EntityReference`1,System.Data.Entity\"\r\n      },\r\n      \"Product\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Product,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"association\": {\r\n          \"name\": \"Product_Order_Detail\",\r\n          \"thisKey\": [\r\n            \"ProductID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"ProductID\"\r\n          ],\r\n          \"isForeignKey\": true\r\n        }\r\n      },\r\n      \"ProductID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"ProductReference\": {\r\n        \"type\": \"System.Data.Objects.DataClasses.EntityReference`1,System.Data.Entity\"\r\n      },\r\n      \"Quantity\": {\r\n        \"type\": \"System.Int16,mscorlib\"\r\n      },\r\n      \"UnitPrice\": {\r\n        \"type\": \"System.Decimal,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {},\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.EF.Order,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"OrderID\"\r\n    ],\r\n    \"fields\": {\r\n      \"Customer\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Customer,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"association\": {\r\n          \"name\": \"Customer_Order\",\r\n          \"thisKey\": [\r\n            \"CustomerID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"CustomerID\"\r\n          ],\r\n          \"isForeignKey\": true\r\n        }\r\n      },\r\n      \"CustomerID\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"CustomerReference\": {\r\n        \"type\": \"System.Data.Objects.DataClasses.EntityReference`1,System.Data.Entity\"\r\n      },\r\n      \"Employee\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Employee,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"association\": {\r\n          \"name\": \"Employee_Order\",\r\n          \"thisKey\": [\r\n            \"EmployeeID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"EmployeeID\"\r\n          ],\r\n          \"isForeignKey\": true\r\n        }\r\n      },\r\n      \"EmployeeID\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"EmployeeReference\": {\r\n        \"type\": \"System.Data.Objects.DataClasses.EntityReference`1,System.Data.Entity\"\r\n      },\r\n      \"EntityKey\": {\r\n        \"type\": \"System.Data.EntityKey,System.Data.Entity\"\r\n      },\r\n      \"Freight\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"Order_Details\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Order_Detail,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true,\r\n        \"association\": {\r\n          \"name\": \"Order_Order_Detail\",\r\n          \"thisKey\": [\r\n            \"OrderID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"OrderID\"\r\n          ],\r\n          \"isForeignKey\": false\r\n        }\r\n      },\r\n      \"OrderDate\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"OrderID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"RequiredDate\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"ShipAddress\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipCity\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipCountry\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShippedDate\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"Shipper\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Shipper,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"association\": {\r\n          \"name\": \"Shipper_Order\",\r\n          \"thisKey\": [\r\n            \"ShipVia\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"ShipperID\"\r\n          ],\r\n          \"isForeignKey\": true\r\n        }\r\n      },\r\n      \"ShipperReference\": {\r\n        \"type\": \"System.Data.Objects.DataClasses.EntityReference`1,System.Data.Entity\"\r\n      },\r\n      \"ShipPostalCode\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipRegion\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipVia\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {\r\n      \"CustomerID\": {\r\n        \"maxlength\": 5\r\n      },\r\n      \"ShipAddress\": {\r\n        \"maxlength\": 60\r\n      },\r\n      \"ShipCity\": {\r\n        \"maxlength\": 15\r\n      },\r\n      \"ShipCountry\": {\r\n        \"maxlength\": 15\r\n      },\r\n      \"ShipName\": {\r\n        \"maxlength\": 40\r\n      },\r\n      \"ShipPostalCode\": {\r\n        \"maxlength\": 10\r\n      },\r\n      \"ShipRegion\": {\r\n        \"maxlength\": 15\r\n      }\r\n    },\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.EF.Customer,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"CustomerID\"\r\n    ],\r\n    \"fields\": {\r\n      \"Address\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"City\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"CompanyName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ContactName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ContactTitle\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Country\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"CustomerDemographics\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.CustomerDemographic,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true\r\n      },\r\n      \"CustomerID\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"EntityKey\": {\r\n        \"type\": \"System.Data.EntityKey,System.Data.Entity\"\r\n      },\r\n      \"Fax\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Orders\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Order,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true,\r\n        \"association\": {\r\n          \"name\": \"Customer_Order\",\r\n          \"thisKey\": [\r\n            \"CustomerID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"CustomerID\"\r\n          ],\r\n          \"isForeignKey\": false\r\n        }\r\n      },\r\n      \"Phone\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"PostalCode\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Region\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {\r\n      \"Address\": {\r\n        \"maxlength\": 60\r\n      },\r\n      \"City\": {\r\n        \"maxlength\": 15\r\n      },\r\n      \"CompanyName\": {\r\n        \"required\": true,\r\n        \"maxlength\": 40\r\n      },\r\n      \"ContactName\": {\r\n        \"maxlength\": 30\r\n      },\r\n      \"ContactTitle\": {\r\n        \"maxlength\": 30\r\n      },\r\n      \"Country\": {\r\n        \"maxlength\": 15\r\n      },\r\n      \"CustomerID\": {\r\n        \"required\": true,\r\n        \"maxlength\": 5\r\n      },\r\n      \"Fax\": {\r\n        \"maxlength\": 24\r\n      },\r\n      \"Phone\": {\r\n        \"maxlength\": 24\r\n      },\r\n      \"PostalCode\": {\r\n        \"maxlength\": 10\r\n      },\r\n      \"Region\": {\r\n        \"maxlength\": 15\r\n      }\r\n    },\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.EF.Employee,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"EmployeeID\"\r\n    ],\r\n    \"fields\": {\r\n      \"Address\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"BirthDate\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"City\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Country\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Employee1\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Employee,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"association\": {\r\n          \"name\": \"Employee_Employee\",\r\n          \"thisKey\": [\r\n            \"ReportsTo\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"EmployeeID\"\r\n          ],\r\n          \"isForeignKey\": true\r\n        }\r\n      },\r\n      \"Employee1Reference\": {\r\n        \"type\": \"System.Data.Objects.DataClasses.EntityReference`1,System.Data.Entity\"\r\n      },\r\n      \"EmployeeID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"Employees1\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Employee,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true,\r\n        \"association\": {\r\n          \"name\": \"Employee_Employee\",\r\n          \"thisKey\": [\r\n            \"EmployeeID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"ReportsTo\"\r\n          ],\r\n          \"isForeignKey\": false\r\n        }\r\n      },\r\n      \"EntityKey\": {\r\n        \"type\": \"System.Data.EntityKey,System.Data.Entity\"\r\n      },\r\n      \"Extension\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"FirstName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"HireDate\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"HomePhone\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"LastName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Notes\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Orders\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Order,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true,\r\n        \"association\": {\r\n          \"name\": \"Employee_Order\",\r\n          \"thisKey\": [\r\n            \"EmployeeID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"EmployeeID\"\r\n          ],\r\n          \"isForeignKey\": false\r\n        }\r\n      },\r\n      \"Photo\": {\r\n        \"type\": \"System.Byte,mscorlib\",\r\n        \"array\": true\r\n      },\r\n      \"PhotoPath\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"PostalCode\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Region\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ReportsTo\": {\r\n        \"type\": \"System.Nullable`1,mscorlib\"\r\n      },\r\n      \"Territories\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Territory,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true\r\n      },\r\n      \"Title\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"TitleOfCourtesy\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {\r\n      \"Address\": {\r\n        \"maxlength\": 60\r\n      },\r\n      \"City\": {\r\n        \"maxlength\": 15\r\n      },\r\n      \"Country\": {\r\n        \"maxlength\": 15\r\n      },\r\n      \"Extension\": {\r\n        \"maxlength\": 4\r\n      },\r\n      \"FirstName\": {\r\n        \"required\": true,\r\n        \"maxlength\": 10\r\n      },\r\n      \"HomePhone\": {\r\n        \"maxlength\": 24\r\n      },\r\n      \"LastName\": {\r\n        \"required\": true,\r\n        \"maxlength\": 20\r\n      },\r\n      \"PhotoPath\": {\r\n        \"maxlength\": 255\r\n      },\r\n      \"PostalCode\": {\r\n        \"maxlength\": 10\r\n      },\r\n      \"Region\": {\r\n        \"maxlength\": 15\r\n      },\r\n      \"Title\": {\r\n        \"maxlength\": 30\r\n      },\r\n      \"TitleOfCourtesy\": {\r\n        \"maxlength\": 25\r\n      }\r\n    },\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.EF.Shipper,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"ShipperID\"\r\n    ],\r\n    \"fields\": {\r\n      \"CompanyName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"EntityKey\": {\r\n        \"type\": \"System.Data.EntityKey,System.Data.Entity\"\r\n      },\r\n      \"Orders\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Order,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true,\r\n        \"association\": {\r\n          \"name\": \"Shipper_Order\",\r\n          \"thisKey\": [\r\n            \"ShipperID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"ShipVia\"\r\n          ],\r\n          \"isForeignKey\": false\r\n        }\r\n      },\r\n      \"Phone\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ShipperID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {\r\n      \"CompanyName\": {\r\n        \"required\": true,\r\n        \"maxlength\": 40\r\n      },\r\n      \"Phone\": {\r\n        \"maxlength\": 24\r\n      }\r\n    },\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.EF.Supplier,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"SupplierID\"\r\n    ],\r\n    \"fields\": {\r\n      \"Address\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"City\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"CompanyName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ContactName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ContactTitle\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Country\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"EntityKey\": {\r\n        \"type\": \"System.Data.EntityKey,System.Data.Entity\"\r\n      },\r\n      \"Fax\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"HomePage\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Phone\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"PostalCode\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Products\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.EF.Product,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true,\r\n        \"association\": {\r\n          \"name\": \"Supplier_Product\",\r\n          \"thisKey\": [\r\n            \"SupplierID\"\r\n          ],\r\n          \"otherKey\": [\r\n            \"SupplierID\"\r\n          ],\r\n          \"isForeignKey\": false\r\n        }\r\n      },\r\n      \"Region\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"SupplierID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {\r\n      \"Address\": {\r\n        \"maxlength\": 60\r\n      },\r\n      \"City\": {\r\n        \"maxlength\": 15\r\n      },\r\n      \"CompanyName\": {\r\n        \"required\": true,\r\n        \"maxlength\": 40\r\n      },\r\n      \"ContactName\": {\r\n        \"maxlength\": 30\r\n      },\r\n      \"ContactTitle\": {\r\n        \"maxlength\": 30\r\n      },\r\n      \"Country\": {\r\n        \"maxlength\": 15\r\n      },\r\n      \"Fax\": {\r\n        \"maxlength\": 24\r\n      },\r\n      \"Phone\": {\r\n        \"maxlength\": 24\r\n      },\r\n      \"PostalCode\": {\r\n        \"maxlength\": 10\r\n      },\r\n      \"Region\": {\r\n        \"maxlength\": 15\r\n      }\r\n    },\r\n    \"messages\": {}\r\n  }\r\n});\n\nupshot.dataSources.GetProducts = upshot.RemoteDataSource({\n    providerParameters: { url: \"myUrl\", operationName: \"GetProducts\" },\n    entityType: \"Microsoft.Web.Http.Data.Test.Models.EF.Product,Microsoft.Web.Http.Data.Helpers.Test\",\n    bufferChanges: true,\n    dataContext: undefined,\n    mapping: {}\n});\r\n</script>";
        private static readonly string _citiesContext = "<script type='text/javascript'>\nupshot.dataSources = upshot.dataSources || {};\nupshot.metadata({\r\n  \"Microsoft.Web.Http.Data.Test.Models.City,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"CountyName\",\r\n      \"Name\",\r\n      \"StateName\"\r\n    ],\r\n    \"fields\": {\r\n      \"CalculatedCounty\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"CountyName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Name\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"StateName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ZipCodes\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Zip,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true\r\n      },\r\n      \"ZoneID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"ZoneName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {},\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.CityWithInfo,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"CountyName\",\r\n      \"Name\",\r\n      \"StateName\"\r\n    ],\r\n    \"fields\": {\r\n      \"CalculatedCounty\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"CountyName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"EditHistory\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"Info\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"LastUpdated\": {\r\n        \"type\": \"System.DateTime,mscorlib\"\r\n      },\r\n      \"Name\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"StateName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ZipCodes\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Zip,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true\r\n      },\r\n      \"ZipCodesWithInfo\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.ZipWithInfo,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true\r\n      },\r\n      \"ZoneID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"ZoneName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {},\r\n    \"messages\": {}\r\n  },\r\n  \"Microsoft.Web.Http.Data.Test.Models.CityWithEditHistory,Microsoft.Web.Http.Data.Helpers.Test\": {\r\n    \"key\": [\r\n      \"CountyName\",\r\n      \"Name\",\r\n      \"StateName\"\r\n    ],\r\n    \"fields\": {\r\n      \"CalculatedCounty\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"CountyName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"EditHistory\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"LastUpdated\": {\r\n        \"type\": \"System.DateTime,mscorlib\"\r\n      },\r\n      \"Name\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"StateName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      },\r\n      \"ZipCodes\": {\r\n        \"type\": \"Microsoft.Web.Http.Data.Test.Models.Zip,Microsoft.Web.Http.Data.Helpers.Test\",\r\n        \"array\": true\r\n      },\r\n      \"ZoneID\": {\r\n        \"type\": \"System.Int32,mscorlib\"\r\n      },\r\n      \"ZoneName\": {\r\n        \"type\": \"System.String,mscorlib\"\r\n      }\r\n    },\r\n    \"rules\": {},\r\n    \"messages\": {}\r\n  }\r\n});\n\nupshot.dataSources.GetCities = upshot.RemoteDataSource({\n    providerParameters: { url: \"myUrl\", operationName: \"GetCities\" },\n    entityType: \"Microsoft.Web.Http.Data.Test.Models.City,Microsoft.Web.Http.Data.Helpers.Test\",\n    bufferChanges: true,\n    dataContext: undefined,\n    mapping: {}\n});\r\n</script>";

        [Fact]
        public void UpshotContextTest1()
        {
            HtmlHelper html = MvcHelper.GetHtmlHelper(new ViewDataDictionary());

            string catalogProductsContext = UpshotExtensions.UpshotContext(html, true).DataSource<CatalogController>(x => x.GetProducts(), "myUrl", "GetProducts").ToHtmlString();
            Assert.Equal(catalogProductsContext, _catalogProductsContext);

            string northwindProductsContext = UpshotExtensions.UpshotContext(html, true).DataSource<NorthwindEFTestController>(x => x.GetProducts(), "myUrl", "GetProducts").ToHtmlString();
            Assert.Equal(northwindProductsContext, _northwindProductsContext);

            string citiesContext = UpshotExtensions.UpshotContext(html, true).DataSource<CitiesController>(x => x.GetCities(), "myUrl", "GetCities").ToHtmlString();
            Assert.Equal(citiesContext, _citiesContext);
        }
    }
}
