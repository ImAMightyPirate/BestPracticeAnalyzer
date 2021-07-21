// <copyright file="PublicConstantAnalyzerCodeFixProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace BestPracticeAnalyzer.MakeFieldStaticReadonly
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BestPracticeAnalyzer.Constants;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Code fix provided for the <see cref="MakeFieldStaticReadonlyAnalyzer"/>.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeFieldStaticReadonlyAnalyzerCodeFixProvider))]
    [Shared]
    public class MakeFieldStaticReadonlyAnalyzerCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.MakeFieldStaticReadonly);

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();

            var codeAction = CodeAction.Create(
                title: Resources.MakeFieldStaticReadonlyAnalyzerCodeFixTitle,
                createChangedDocument: c => this.FixAsync(context.Document, declaration, c),
                equivalenceKey: nameof(MakeFieldStaticReadonlyAnalyzerCodeFixProvider));

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(codeAction, diagnostic);
        }

        private async Task<Document> FixAsync(
            Document document,
            FieldDeclarationSyntax oldFieldDeclaration,
            CancellationToken cancellationToken)
        {
            // Remove the const keyword from the original modifiers
            var newModifiers = oldFieldDeclaration.Modifiers
                .Where(m => !m.IsKind(SyntaxKind.ConstKeyword))
                .ToList();

            // Add the static and readonly keywords to the modifiers
            newModifiers.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            newModifiers.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            var newFieldDeclaration = oldFieldDeclaration
                .WithModifiers(
                    SyntaxTokenList
                    .Create(newModifiers.First())
                    .AddRange(newModifiers.Skip(1)));

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(root.ReplaceNode(oldFieldDeclaration, newFieldDeclaration));
        }
    }
}
