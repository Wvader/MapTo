using MapTo.Sources;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapTo.Extensions
{
    internal static class CommonExtensions
    {
        internal static SourceBuilder WriteComment(this SourceBuilder builder, string comment)
        {
            return builder.WriteLine($"// {comment}");
        }

        internal static SourceBuilder WriteMappedProperties(this SourceBuilder builder, System.Collections.Immutable.ImmutableArray<MappedProperty> mappedProperties)
        {
            foreach (var item in mappedProperties)
            {
                builder.WriteComment($"Name: {item.Name}");
                builder.WriteComment($"Type: {item.Type}");
                builder.WriteComment($"MappedSourcePropertyTypeName: {item.MappedSourcePropertyTypeName}");
                builder.WriteComment($"IsEnumerable: {item.IsEnumerable}");
                builder.WriteComment($"SourcePropertyName: {item.SourcePropertyName}");

            }

            return builder;
        }

    }
}
