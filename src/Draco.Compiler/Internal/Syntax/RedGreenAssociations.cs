using System;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Handles associations between red and green node types.
/// </summary>
internal static partial class RedGreenAssociations
{
    /// <summary>
    /// Gets the red node type for a green node.
    /// </summary>
    /// <typeparam name="TGreen">The type of the green node.</typeparam>
    /// <returns>The <see cref="Type"/> for the red node.</returns>
    public static Type Red<TGreen>()
        where TGreen : SyntaxNode =>
        GreenToRed[typeof(TGreen)];

    /// <summary>
    /// Gets the green node type for a red node.
    /// </summary>
    /// <typeparam name="TRed">The type of the red node.</typeparam>
    /// <returns>The <see cref="Type"/> for the green node.</returns>
    public static Type Green<TRed>()
        where TRed : Api.Syntax.SyntaxNode =>
        RedToGreen[typeof(TRed)];
}
