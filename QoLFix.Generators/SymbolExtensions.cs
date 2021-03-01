// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace QoLFix.Generators
{
    /// <summary>
    /// A set of extension methods for <see cref="ISymbol"/> and its derived types.
    /// </summary>
    internal static class SymbolExtensions
    {
        internal static ImmutableArray<IParameterSymbol> ParametersOrDefault(this ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).Parameters;
                case SymbolKind.Property:
                    return ((IPropertySymbol)symbol).Parameters;
                default:
                    return default;
            }
        }
    }
}
