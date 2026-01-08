using System.Collections.Generic;
using CSharpHealth.Core;
using Xunit;

namespace CSharpHealth.Tests
{
    public class SimilarityCalculatorTests
    {
        [Fact]
        public void JaccardNgramSimilarity_IdenticalTokens_ReturnsOne()
        {
            var tokens = new List<string> { "ID1", "+", "ID2", ";", "return", "ID1" };

            var similarity = SimilarityCalculator.JaccardNgramSimilarity(tokens, tokens, 3);

            Assert.Equal(1.0, similarity);
        }

        [Fact]
        public void JaccardNgramSimilarity_SlightlyDifferentTokens_ReturnsBetweenZeroAndOne()
        {
            var first = new List<string> { "ID1", "+", "ID2", ";", "return", "ID1" };
            var second = new List<string> { "ID1", "+", "ID2", ";", "return", "ID2" };

            var similarity = SimilarityCalculator.JaccardNgramSimilarity(first, second, 3);

            Assert.InRange(similarity, 0.0, 1.0);
            Assert.NotEqual(0.0, similarity);
            Assert.NotEqual(1.0, similarity);
        }

        [Fact]
        public void JaccardNgramSimilarity_DifferentTokens_ReturnsLowSimilarity()
        {
            var first = new List<string> { "ID1", "+", "ID2", ";", "return", "ID1" };
            var second = new List<string> { "while", "(", "ID3", ")", "{", "ID4", "}", ";" };

            var similarity = SimilarityCalculator.JaccardNgramSimilarity(first, second, 3);

            Assert.True(similarity < 0.2);
        }
    }
}
