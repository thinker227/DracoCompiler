namespace Draco.SourceGeneration.Lsp.Metamodel;

internal abstract class Type
{
    /// <summary>
    /// The discriminating type kind.
    /// </summary>
    public abstract string Kind { get; set; }
}
