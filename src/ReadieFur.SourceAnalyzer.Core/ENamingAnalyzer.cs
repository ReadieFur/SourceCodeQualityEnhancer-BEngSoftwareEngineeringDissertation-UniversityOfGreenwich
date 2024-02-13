namespace ReadieFur.SourceAnalyzer.Core
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
