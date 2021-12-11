using MapTo.Sources;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapTo.Extensions
{
    internal static class CommonExtensions
    {
        internal static SourceBuilder WriteComment(this SourceBuilder builder, string comment = "")
        {
            return builder.WriteLine($"// {comment}");
        }

        internal static SourceBuilder WriteModelInfo(this SourceBuilder builder, MappingModel model)
        {
            return builder
                        .WriteLine()
                        .WriteComment($" IsTypeUpdatable                {model.IsTypeUpdatable}")
                        .WriteComment($" HasMappedBaseClass             {model.HasMappedBaseClass.ToString()}")
                        .WriteComment($" Namespace                      {model.Namespace}")
                        .WriteComment($" Options                        {model.Options.ToString()}")
                        .WriteComment($" Type                           {model.Type}")
                        .WriteComment($" TypeIdentifierName             {model.TypeIdentifierName}")
                        .WriteComment($" SourceNamespace                {model.SourceNamespace}")
                        .WriteComment($" SourceTypeFullName             {model.SourceTypeFullName}")
                        .WriteComment($" SourceTypeIdentifierName       {model.SourceTypeIdentifierName}");

        }

        internal static SourceBuilder WriteMappedProperties(this SourceBuilder builder, System.Collections.Immutable.ImmutableArray<MappedMember> mappedProperties)
        {
            foreach (var item in mappedProperties)
            {
                builder .WriteComment($" Name                           {item.Name}")
                        .WriteComment($" Type                           {item.Type}")
                        .WriteComment($" MappedSourcePropertyTypeName   {item.MappedSourcePropertyTypeName}")
                        .WriteComment($" IsEnumerable                   {item.IsEnumerable}")
                        .WriteComment($" FullyQualifiedType             {item.FullyQualifiedType}")
                        .WriteComment($" EnumerableTypeArgument         {item.EnumerableTypeArgument}")
                        .WriteComment($" SourcePropertyName             {item.SourcePropertyName}")
                        .WriteComment($" TypeSymbol                     {item.FullyQualifiedType.ToString()}")
                        .WriteComment($" isReadOnly                     {item.isReadOnly.ToString()}")
                        .WriteLine();
            }

            return builder;
        }

    }
}
