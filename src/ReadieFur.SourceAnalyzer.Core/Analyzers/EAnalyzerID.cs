namespace ReadieFur.SourceAnalyzer.Core.Analyzers
{
    internal enum EAnalyzerID : int
    {
        Default = 0,
        Naming_PrivateField = 1,
        Naming_InternalField,
        Naming_ProtectedField,
        Naming_PublicField,
        Naming_Property,
        Naming_Method,
        Naming_Class,
        Naming_Interface,
        Naming_Enum,
        Naming_Struct,
        Naming_LocalVariable,
        Naming_Parameter,
        Naming_Constant,
        Naming_Namespace,
        Naming_GenericParameter,
        Brace_Location,
        Indentation,
        Comment_LeadingSpace,
        Comment_TrailingFullStop,
        Comment_NewLine,
        Comment_Capitalize,
        Punctuation_Space,
        Punctuation_NewLine,
        Inferred_AccessModifier,
        Inferred_NewKeyword,
        ObjectStructure_PropertiesAtTop,
    }
}
