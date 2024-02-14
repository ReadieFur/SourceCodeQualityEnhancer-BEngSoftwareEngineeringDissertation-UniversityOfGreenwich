namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
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
