namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    //TODO: Add more selectors e.g. private methods, protected methods, etc.
    internal enum ENamingAnalyzer
    {
        PrivateField = 1,
        InternalField,
        ProtectedField,
        PublicField,
        Property,
        Method,
        Class,
        Interface,
        Enum,
        Struct,
        LocalVariable,
        Parameter,
        Constant,
        Namespace,
        GenericParameter
    }
}
