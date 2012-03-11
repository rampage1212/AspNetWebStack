﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace System.Web.Mvc
{
    public class RegularExpressionAttributeAdapter : DataAnnotationsModelValidator<RegularExpressionAttribute>
    {
        public RegularExpressionAttributeAdapter(ModelMetadata metadata, ControllerContext context, RegularExpressionAttribute attribute)
            : base(metadata, context, attribute)
        {
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules()
        {
            return new[] { new ModelClientValidationRegexRule(ErrorMessage, Attribute.Pattern) };
        }
    }
}
