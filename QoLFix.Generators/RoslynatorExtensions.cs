#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace QoLFix.Generators
{
    internal static class RoslynatorExtensions
    {
        /// <summary>
        /// Determines a parameter symbol that matches to the specified argument.
        /// Returns null if no matching parameter is found.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="argument"></param>
        /// <param name="allowParams"></param>
        /// <param name="allowCandidate"></param>
        /// <param name="cancellationToken"></param>
        public static IParameterSymbol DetermineParameter(
            this SemanticModel semanticModel,
            ArgumentSyntax argument,
            bool allowParams = false,
            bool allowCandidate = false,
            CancellationToken cancellationToken = default)
        {
            if (semanticModel == null)
                throw new ArgumentNullException(nameof(semanticModel));

            if (argument == null)
                throw new ArgumentNullException(nameof(argument));

            return DetermineParameterHelper.DetermineParameter(argument, semanticModel, allowParams, allowCandidate, cancellationToken);
        }
    }
}
