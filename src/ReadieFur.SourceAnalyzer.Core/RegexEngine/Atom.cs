namespace ReadieFur.SourceAnalyzer.Core.RegexEngine
{
    internal class Atom : Token
    {
        public char Value { get; set; }

        public Atom(char value) => Value = value;

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
