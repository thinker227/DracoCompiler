using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Utilities for <see cref="SeparatedSyntaxList{TNode}"/>.
/// </summary>
internal static class SeparatedSyntaxList
{
    /// <summary>
    /// Creates a builder for a <see cref="SeparatedSyntaxList{TNode}"/>.
    /// </summary>
    /// <typeparam name="TNode">The node type.</typeparam>
    /// <returns>The created builder.</returns>
    public static SeparatedSyntaxList<TNode>.Builder CreateBuilder<TNode>()
        where TNode : SyntaxNode => new();

    /// <summary>
    /// Creates an <see cref="IEnumerable{T}"/> of syntax ndoes by interleaving a sequence of values with a sequence of separators.
    /// </summary>
    /// <typeparam name="TNode">The node type.</typeparam>
    /// <param name="separators">The separator tokens.</param>
    /// <param name="values">The value nodes. Has to be equal to or one more than the length of <paramref name="separators"/>.</param>
    /// <returns>The constructed interleaved sequence.</returns>
    public static IEnumerable<SyntaxNode> CreateInterleavedSequence(IEnumerable<SyntaxToken> separators, IEnumerable<SyntaxNode> values)
    {
        using var valuesEnum = values.GetEnumerator();
        using var separatorsEnum = separators.GetEnumerator();

        while (valuesEnum.MoveNext())
        {
            yield return valuesEnum.Current;

            if (!separatorsEnum.MoveNext())
            {
                if (valuesEnum.MoveNext()) throw new ArgumentException("Found more elements than separators.", nameof(values));
                yield break;
            }

            yield return separatorsEnum.Current;
        }

        if (separatorsEnum.MoveNext()) throw new ArgumentException("Found more separators than elements.", nameof(separators));
    }
}

/// <summary>
/// A generic list of <see cref="SyntaxNode"/>s separated by <see cref="SyntaxToken"/>s.
/// </summary>
/// <typeparam name="TNode">The kind of <see cref="SyntaxNode"/>s the list holds between the separators.</typeparam>
internal sealed partial class SeparatedSyntaxList<TNode>(
    ImmutableArray<SyntaxNode> nodes) : SyntaxNode, IReadOnlyList<SyntaxNode>
    where TNode : SyntaxNode
{
    private static Type RedElementType { get; } = Assembly
        .GetExecutingAssembly()
        .GetType($"Draco.Compiler.Api.Syntax.{typeof(TNode).Name}")!;
    private static Type RedNodeType { get; } = typeof(Api.Syntax.SeparatedSyntaxList<>).MakeGenericType(RedElementType);
    private static ConstructorInfo RedNodeConstructor { get; } = RedNodeType.GetConstructor(
        BindingFlags.NonPublic | BindingFlags.Instance,
        [
            typeof(Api.Syntax.SyntaxTree),
            typeof(Api.Syntax.SyntaxNode),
            typeof(int),
            typeof(IReadOnlyList<SyntaxNode>),
        ])!;

    /// <summary>
    /// The separated values in this list.
    /// </summary>
    public IEnumerable<TNode> Values
    {
        get
        {
            for (var i = 0; i < this.Nodes.Length; i += 2) yield return (TNode)this.Nodes[i];
        }
    }

    /// <summary>
    /// The separators in this list.
    /// </summary>
    public IEnumerable<SyntaxToken> Separators
    {
        get
        {
            for (var i = 1; i < this.Nodes.Length; i += 2) yield return (SyntaxToken)this.Nodes[i];
        }
    }

    /// <summary>
    /// The raw nodes of this syntax list.
    /// </summary>
    public ImmutableArray<SyntaxNode> Nodes { get; } = nodes;

    int IReadOnlyCollection<SyntaxNode>.Count => this.Nodes.Length;
    SyntaxNode IReadOnlyList<SyntaxNode>.this[int index] => this.Nodes[index];

    public override IEnumerable<SyntaxNode> Children => this.Nodes;

    public SeparatedSyntaxList(IEnumerable<SyntaxNode> nodes)
        : this(nodes.ToImmutableArray())
    {
    }

    public Builder ToBuilder() => new(this.Nodes.ToBuilder());

    public override void Accept(SyntaxVisitor visitor) => visitor.VisitSeparatedSyntaxList(this);
    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) => visitor.VisitSeparatedSyntaxList(this);
    public override Api.Syntax.SyntaxNode ToRedNode(Api.Syntax.SyntaxTree tree, Api.Syntax.SyntaxNode? parent, int fullPosition) =>
        (Api.Syntax.SyntaxNode)RedNodeConstructor.Invoke([tree, parent, fullPosition, this])!;

    public IEnumerator<SyntaxNode> GetEnumerator() => this.Nodes.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
