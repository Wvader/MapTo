﻿using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class MapFromAttributeSource
    {
        internal const string AttributeName = "MapFrom";
        internal const string AttributeClassName = AttributeName + "Attribute";
        internal const string FullyQualifiedName = RootNamespace + "." + AttributeClassName;
        
        internal static SourceCode Generate(SourceGenerationOptions options)
        {
            using var builder = new SourceBuilder()
                .WriteLine(GeneratedFilesHeader)
                .WriteLine("using System;")
                .WriteLine()
                .WriteLine($"namespace {RootNamespace}")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Specifies that the annotated class can be mapped from the provided <see cref=\"SourceType\"/>.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine("[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]")
                .WriteLine($"public sealed class {AttributeName}Attribute : Attribute")
                .WriteOpeningBracket();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine($"/// Initializes a new instance of the <see cref=\"{AttributeName}Attribute\"/> class with the specified <paramref name=\"sourceType\"/>.")
                    .WriteLine("/// </summary>")
                    .WriteLine("/// <param name=\"sourceType\">The type of to map from.</param>");
            }

            builder
                .WriteLine($"public {AttributeName}Attribute(Type sourceType)")
                .WriteOpeningBracket()
                .WriteLine("SourceType = sourceType;")
                .WriteClosingBracket()
                .WriteLine();

            if (options.GenerateXmlDocument)
            {
                builder
                    .WriteLine("/// <summary>")
                    .WriteLine("/// Gets the type to map from.")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine("public Type SourceType { get; }")
                .WriteClosingBracket() // class
                .WriteClosingBracket(); // namespace

            return new(builder.ToString(), $"{AttributeName}Attribute.g.cs");
        }
    }
}