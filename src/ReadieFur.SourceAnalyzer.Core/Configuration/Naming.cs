namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public sealed class Naming
    {
        public NamingConvention? PrivateField { get; set; }
        public NamingConvention? InternalField { get; set; }
        public NamingConvention? ProtectedField { get; set; }
        public NamingConvention? PublicField { get; set; }
        public NamingConvention? Property { get; set; }
        public NamingConvention? Method { get; set; }
        public NamingConvention? Class { get; set; }
        public NamingConvention? Interface { get; set; }
        public NamingConvention? Enum { get; set; }
        public NamingConvention? Struct { get; set; }
        public NamingConvention? LocalVariable { get; set; }
        public NamingConvention? Parameter { get; set; }
        public NamingConvention? Constant { get; set; }
        public NamingConvention? Namespace { get; set; }
        public NamingConvention? GenericParameter { get; set; }
    }
}
