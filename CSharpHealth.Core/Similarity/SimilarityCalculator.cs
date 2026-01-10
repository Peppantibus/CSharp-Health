using System;
using System.Collections.Generic;

namespace CSharpHealth.Core.Similarity
{
    public static class SimilarityCalculator
    {
        private const string NgramSeparator = "\u001F";

        public static double JaccardNgramSimilarity(IReadOnlyList<string> aTokens, IReadOnlyList<string> bTokens, int n = 3)
        {
            if (aTokens is null)
            {
                throw new ArgumentNullException(nameof(aTokens));
            }

            if (bTokens is null)
            {
                throw new ArgumentNullException(nameof(bTokens));
            }

            if (n <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(n), "N-gram size must be positive.");
            }

            var aSet = BuildNgramSet(aTokens, n);
            var bSet = BuildNgramSet(bTokens, n);

            if (aSet.Count == 0 && bSet.Count == 0)
            {
                return 1.0;
            }

            var intersection = 0;

            if (aSet.Count <= bSet.Count)
            {
                foreach (var item in aSet)
                {
                    if (bSet.Contains(item))
                    {
                        intersection++;
                    }
                }
            }
            else
            {
                foreach (var item in bSet)
                {
                    if (aSet.Contains(item))
                    {
                        intersection++;
                    }
                }
            }

            var union = aSet.Count + bSet.Count - intersection;
            return union == 0 ? 0.0 : intersection / (double)union;
        }

        private static HashSet<string> BuildNgramSet(IReadOnlyList<string> tokens, int n)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (tokens.Count < n)
            {
                return set;
            }

            var tokenArray = tokens as string[] ?? tokens.ToArray();
            for (var i = 0; i <= tokens.Count - n; i++)
            {
                var ngram = string.Join(NgramSeparator, tokenArray, i, n);
                set.Add(ngram);
            }

            return set;
        }
    }
}
