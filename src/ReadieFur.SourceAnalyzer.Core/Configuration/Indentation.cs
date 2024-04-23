using System;
using YamlDotNet.Serialization;

namespace ReadieFur.SourceAnalyzer.Core.Configuration
{
    public sealed class Indentation : ConfigBase
    {
        public const int DEFAULT_SIZE = 4;

        //Not using nameof becuase I want a lowercase value and attribute values must be constant so .ToLower() cannot be used.
        [YamlMember(Alias = "size")]
        [Obsolete("This property is for deserialization only. Use Size instead.", false)]
        public int _size { get; set; } = DEFAULT_SIZE;

#pragma warning disable CS0618 // Type or member is obsolete
        [YamlIgnore]
        public int Size
        {
            get
            {
                if (_size < 1)
                {
#if DEBUG || false
                    throw new ArgumentException("Value must not be less than 1.", nameof(Size));
#else
                    _size = DEFAULT_SIZE;
#endif
                }
                return _size;
            }
            set => _size = value;
        }
#pragma warning restore CS0618
    }
}
