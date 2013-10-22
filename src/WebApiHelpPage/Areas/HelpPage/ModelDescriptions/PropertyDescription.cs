﻿using System.Collections.Generic;

namespace ROOT_PROJECT_NAMESPACE.Areas.HelpPage.ModelDescriptions
{
    public class PropertyDescription
    {
        public PropertyDescription()
        {
            Annotations = new List<PropertyAnnotation>();
        }

        public IList<PropertyAnnotation> Annotations { get; private set; }

        public string Documentation { get; set; }

        public string Name { get; set; }

        public ModelDescription TypeDescription { get; set; }
    }
}