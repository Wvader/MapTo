﻿using static MapTo.Sources.Constants;

namespace MapTo.Sources
{
    internal static class JsonExtensionAttributeSource
    {
        internal const string AttributeName = "JsonExtension";
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
                    .WriteLine("/// Specifies that the annotated needs ToJson method")
                    .WriteLine("/// </summary>");
            }

            builder
                .WriteLine("[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]")
                .WriteLine($"public sealed class {AttributeName}Attribute : Attribute")
                .WriteOpeningBracket();

            builder
                .WriteClosingBracket() // class
                .WriteClosingBracket(); // namespace

            return new(builder.ToString(), $"{AttributeName}Attribute.g.cs");
        }
    }
}