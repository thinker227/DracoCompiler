using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Draco.Lsp.Model;
using Draco.Lsp.Server.Language;
using DocumentDiagnosticReport = Draco.Lsp.Model.OneOf<Draco.Lsp.Model.RelatedFullDocumentDiagnosticReport, Draco.Lsp.Model.RelatedUnchangedDocumentDiagnosticReport>;

namespace Draco.LanguageServer;

internal partial class DracoLanguageServer : IPullDiagnostics
{
    public DiagnosticRegistrationOptions DiagnosticRegistrationOptions => new()
    {
        DocumentSelector = this.DocumentSelector,
        Identifier = "draco", // TODO: do we need this? does it do anything?
        InterFileDependencies = true,
        WorkspaceDiagnostics = true,
    };

    private int diagVersion = 0;

    public async Task<DocumentDiagnosticReport> DocumentDiagnosticsAsync(DocumentDiagnosticParams param, CancellationToken cancellationToken)
    {
        var syntaxTree = this.GetSyntaxTree(param.TextDocument.Uri);
        if (syntaxTree is null)
        {
            return new RelatedFullDocumentDiagnosticReport()
            {
                Items = Array.Empty<Diagnostic>(),
            };
        }

        // Clear push diagnostics to avoid duplicates
        await this.client.PublishDiagnosticsAsync(new()
        {
            Diagnostics = new List<Diagnostic>(),
            Uri = param.TextDocument.Uri
        });

        var semanticModel = this.compilation.GetSemanticModel(syntaxTree);
        var diags = semanticModel.Diagnostics;
        var lspDiags = diags.Select(Translator.ToLsp).ToList();
        return new RelatedFullDocumentDiagnosticReport()
        {
            Items = lspDiags,
            // TODO: related documents for future
        };
    }

    public Task<WorkspaceDiagnosticReport> WorkspaceDiagnosticsAsync(WorkspaceDiagnosticParams param, CancellationToken cancellationToken)
    {
        var fileDiags = new List<OneOf<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>>();
        foreach (var file in this.compilation.SyntaxTrees)
        {
            var model = this.compilation.GetSemanticModel(file);
            var diags = model.Diagnostics;
            var lspDiags = diags.Select(Translator.ToLsp).ToList();
            var fileDiag = new WorkspaceFullDocumentDiagnosticReport()
            {
                Items = lspDiags,
                Uri = DocumentUri.From(file.SourceText.Path!),
                Version = this.diagVersion, //TODO: what is this? is this required?
            };
            fileDiags.Add(fileDiag);
        }
        this.diagVersion++;
        return Task.FromResult(new WorkspaceDiagnosticReport()
        {
            Items = fileDiags,
        });
    }
}
