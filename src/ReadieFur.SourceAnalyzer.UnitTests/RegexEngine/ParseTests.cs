using ReadieFur.SourceAnalyzer.Core.RegexEngine;
using ReadieFur.SourceAnalyzer.UnitTests.Compatibility;
using static ReadieFur.SourceAnalyzer.UnitTests.RegexEngine.Common;
using System.Reflection;

namespace ReadieFur.SourceAnalyzer.UnitTests.RegexEngine
{
    [CTest]
    public class ParseTests
    {
        private struct SExpectedTree
        {
            public Type Node { get; set; } = null!;
            public List<SExpectedTree> Children { get; set; } = new();

            public SExpectedTree() {}

#if DEBUG
            public override string ToString() => $"{Node.Name}";
#endif
        }

        private void CompareTree(AToken actual, SExpectedTree expected)
        {
            Assert.AreEqual(expected.Node, actual.GetType());

            if (expected.Children is null)
                return;

            Assert.AreEqual(expected.Children.Count, actual.Children.Count);

            for (int i = 0; i < expected.Children.Count; i++)
                CompareTree(actual.Children[i], expected.Children[i]);
        }

        private void Parse(string pattern, List<SExpectedTree>? expectedTree = null)
        {
            Regex regex = new Regex(pattern);

            if (expectedTree is null)
                return;

            AToken actualTree = (AToken)typeof(Regex)
                .GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(regex)!;

            SExpectedTree expectedRoot = new()
            {
                Node = typeof(GroupConstruct),
                Children = expectedTree,
            };

            CompareTree(actualTree, expectedRoot);
        }

        [MTest]
        public void Pascal() => Parse(PATTERN_PASCAL_CASE, new()
        {
            new()
            {
                Node = typeof(Quantifier),
                Children =
                {
                    new()
                    {
                        Node = typeof(GroupConstruct),
                        Children =
                        {
                            new()
                            {
                                Node = typeof(CharacterClass),
                                Children =
                                {
                                    new()
                                    {
                                        Node = typeof(CharacterRange)
                                    },
                                }
                            },
                            new()
                            {
                                Node = typeof(Quantifier),
                                Children =
                                {
                                    new()
                                    {
                                        Node = typeof(CharacterClass),
                                        Children =
                                        {
                                            new()
                                            {
                                                Node = typeof(CharacterRange)
                                            },
                                        }
                                    }
                                }
                            },
                        }
                    },
                }
            },
        });

        [MTest] public void Camel() => Parse(PATTERN_CAMEL_CASE);

        [MTest] public void Snake() => Parse(PATTERN_SNAKE_CASE);
    }
}
