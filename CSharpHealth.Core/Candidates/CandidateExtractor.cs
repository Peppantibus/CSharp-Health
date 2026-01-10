using System;
using System.Collections.Generic;
using CSharpHealth.Core.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpHealth.Core.Candidates
{
    public class CandidateExtractor
    {
        private readonly int? _minimumLineCount;

        public CandidateExtractor(int? minimumLineCount = null)
        {
            if (minimumLineCount.HasValue && minimumLineCount.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumLineCount), "Minimum line count must be greater than zero.");
            }

            _minimumLineCount = minimumLineCount;
        }

        public IReadOnlyList<CodeCandidate> Extract(ParseResult parseResult)
        {
            if (!parseResult.Success || parseResult.Root is null || parseResult.SyntaxTree is null)
            {
                return Array.Empty<CodeCandidate>();
            }

            var candidates = new List<CodeCandidate>();
            var syntaxTree = parseResult.SyntaxTree;

            foreach (var node in parseResult.Root.DescendantNodes())
            {
                switch (node)
                {
                    case MethodDeclarationSyntax method:
                        AddCandidate(candidates, parseResult.FilePath, syntaxTree, method, CandidateKind.Method);
                        break;
                    case SimpleLambdaExpressionSyntax simpleLambda:
                        AddCandidate(candidates, parseResult.FilePath, syntaxTree, simpleLambda, CandidateKind.Lambda);
                        break;
                    case ParenthesizedLambdaExpressionSyntax parenthesizedLambda:
                        AddCandidate(candidates, parseResult.FilePath, syntaxTree, parenthesizedLambda, CandidateKind.Lambda);
                        break;
                    case BlockSyntax block when IsEligibleBlock(block):
                        AddCandidate(candidates, parseResult.FilePath, syntaxTree, block, CandidateKind.Block);
                        break;
                }
            }

            return candidates;
        }

        public IReadOnlyList<CodeCandidate> ExtractMany(IEnumerable<ParseResult> parseResults)
        {
            var candidates = new List<CodeCandidate>();

            foreach (var parseResult in parseResults)
            {
                candidates.AddRange(Extract(parseResult));
            }

            return candidates;
        }

        private void AddCandidate(
            ICollection<CodeCandidate> candidates,
            string filePath,
            SyntaxTree syntaxTree,
            SyntaxNode node,
            CandidateKind kind)
        {
            var lineSpan = syntaxTree.GetLineSpan(node.Span);
            var startLine = lineSpan.StartLinePosition.Line + 1;
            var endLine = lineSpan.EndLinePosition.Line + 1;

            if (_minimumLineCount.HasValue)
            {
                var lineCount = endLine - startLine + 1;
                if (lineCount < _minimumLineCount.Value)
                {
                    return;
                }
            }

            candidates.Add(new CodeCandidate(
                kind,
                filePath,
                startLine,
                endLine,
                node.SpanStart,
                node.Span.Length,
                node));
        }

        private static bool IsEligibleBlock(BlockSyntax block)
        {
            return block.Parent switch
            {
                IfStatementSyntax ifStatement => ifStatement.Statement == block,
                ElseClauseSyntax elseClause => elseClause.Statement == block,
                ForStatementSyntax forStatement => forStatement.Statement == block,
                ForEachStatementSyntax forEachStatement => forEachStatement.Statement == block,
                ForEachVariableStatementSyntax forEachVariableStatement => forEachVariableStatement.Statement == block,
                WhileStatementSyntax whileStatement => whileStatement.Statement == block,
                DoStatementSyntax doStatement => doStatement.Statement == block,
                TryStatementSyntax tryStatement => tryStatement.Block == block,
                CatchClauseSyntax catchClause => catchClause.Block == block,
                FinallyClauseSyntax finallyClause => finallyClause.Block == block,
                _ => false
            };
        }
    }
}
