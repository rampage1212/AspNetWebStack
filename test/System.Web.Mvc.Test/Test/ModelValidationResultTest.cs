﻿using System.Web.TestUtil;
using Xunit;

namespace System.Web.Mvc.Test
{
    public class ModelValidationResultTest
    {
        [Fact]
        public void MemberNameProperty()
        {
            // Arrange
            ModelValidationResult result = new ModelValidationResult();

            // Act & assert
            MemberHelper.TestStringProperty(result, "MemberName", String.Empty);
        }

        [Fact]
        public void MessageProperty()
        {
            // Arrange
            ModelValidationResult result = new ModelValidationResult();

            // Act & assert
            MemberHelper.TestStringProperty(result, "Message", String.Empty);
        }
    }
}
