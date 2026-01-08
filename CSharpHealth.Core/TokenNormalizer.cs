using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpHealth.Core
{
    public class TokenNormalizer
    {
        public NormalizedCandidate Normalize(CodeCandidate candidate)
        {
            if (candidate is null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            var identifierMap = new Dictionary<string, string>(StringComparer.Ordinal);
            var tokens = new List<string>();
            var identifierIndex = 0;

            foreach (var token in candidate.Node.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.IdentifierToken))
                {
                    if (!identifierMap.TryGetValue(token.ValueText, out var placeholder))
                    {
                        identifierIndex++;
                        placeholder = $"ID{identifierIndex}";
                        identifierMap[token.ValueText] = placeholder;
                    }

                    tokens.Add(placeholder);
                    continue;
                }

                if (token.IsKind(SyntaxKind.NumericLiteralToken))
                {
                    tokens.Add("NUM");
                    continue;
                }

                if (token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringTextToken))
                {
                    tokens.Add("STR");
                    continue;
                }

                if (token.IsKind(SyntaxKind.CharacterLiteralToken))
                {
                    tokens.Add("CHR");
                    continue;
                }

                tokens.Add(token.Text);
            }

            return new NormalizedCandidate(candidate, tokens, tokens.Count);
        }

        public IReadOnlyList<NormalizedCandidate> NormalizeMany(IEnumerable<CodeCandidate> candidates)
        {
            if (candidates is null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            var normalized = new List<NormalizedCandidate>();

            foreach (var candidate in candidates)
            {
                normalized.Add(Normalize(candidate));
            }

            return normalized;
        }

        public static string JoinTokens(IReadOnlyList<string> tokens, string separator = " ")
        {
            if (tokens is null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            return string.Join(separator, tokens);
        }
    }
}
