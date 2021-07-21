// <copyright file="PublicConstantAnalyzer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace BestPracticeAnalyzer.MakeFieldStaticReadonly
{
    using System.Collections.Immutable;
    using System.Linq;
    using BestPracticeAnalyzer.Constants;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>
    /// Roslyn analyzer that identifies public constant fields.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeFieldStaticReadonlyAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.MakeFieldStaticReadonly,
            new LocalizableResourceString(nameof(Resources.MakeFieldStaticReadonlyAnalyzerTitle), Resources.ResourceManager, typeof(Resources)),
            new LocalizableResourceString(nameof(Resources.MakeFieldStaticReadonlyAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources)),
            DiagnosticDescriptorCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register the actions that will be performed by this analyser
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.FieldDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = context.Node as FieldDeclarationSyntax;
            var isConst = fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword);

            if (isConst && CanConstBeMadeStaticReadonly(fieldDeclaration))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
            }
        }

        private static bool CanConstBeMadeStaticReadonly(FieldDeclarationSyntax fieldDeclaration)
        {
            var isInternal = fieldDeclaration.Modifiers.Any(SyntaxKind.InternalKeyword);
            var isProtected = fieldDeclaration.Modifiers.Any(SyntaxKind.ProtectedKeyword);

            // 'internal' is the only access modifier that prevents usage from other assemblies
            // Must ensure the 'protected' access modifier is not also present as that increases access
            if (isInternal && !isProtected)
            {
                return false;
            }

            // All other access modifiers allow usage from other assemblies
            // See https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/access-modifiers
            // public
            // private
            // protected
            // protected internal
            // private protected
            return true;
        }
    }
}
