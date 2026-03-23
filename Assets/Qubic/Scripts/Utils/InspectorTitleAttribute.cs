using System;

namespace QubicNS
{
    public class InspectorTitleAttribute : Attribute
    {
        //
        public string Name { get; set; }

        public InspectorTitleAttribute(string name)
        {
            Name = name;
        }
    }
}