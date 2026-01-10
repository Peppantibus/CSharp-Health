using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using CSharpHealth.Core.Duplicates;
using CSharpHealth.Core.Normalization;

namespace CSharpHealth.Core.Similarity
{
    public sealed class SignatureComputer
    {
        private const string TokenSeparator = "\u001F";

        public HashedCandidate Compute(NormalizedCandidate normalized)
        {
            if (normalized is null)
            {
                throw new ArgumentNullException(nameof(normalized));
            }

            var joined = string.Join(TokenSeparator, normalized.Tokens);
            var hash = ComputeHashHex(joined);

            return new HashedCandidate(normalized, hash, normalized.TokenCount);
        }

        public IReadOnlyList<HashedCandidate> ComputeMany(IEnumerable<NormalizedCandidate> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var results = new List<HashedCandidate>();

            foreach (var item in items)
            {
                results.Add(Compute(item));
            }

            return results;
        }

        private static string ComputeHashHex(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = SHA256.HashData(bytes);
            var builder = new StringBuilder(hash.Length * 2);

            foreach (var value in hash)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
