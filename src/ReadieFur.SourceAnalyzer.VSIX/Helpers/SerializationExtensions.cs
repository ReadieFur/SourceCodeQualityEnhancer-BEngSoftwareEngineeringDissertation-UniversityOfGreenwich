using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace ReadieFur.SourceAnalyzer.VSIX.Helpers
{
    //https://github.com/ReadieFur/CreateProcessAsUser/blob/development/src/CreateProcessAsUser.Shared/Helpers.cs
    internal static class SerializationExtensions
    {
        public static Char[] ToFixedCharArray(this string str, uint length)
        {
            char[] buffer = new char[(int)length];
            str.CopyTo(0, buffer, 0, (int)length);
            return buffer;
        }

        public static string FromCharArray(this char[] buffer)
        {
            //TODO: Check for a size value to use, if it is not found then fall back to terminating at the first null character.
            return new string(buffer).TrimEnd('\0');
        }

        //I did orignally want to convert a string to a char array and store that but I was getting an index count error.
        public static void ToCustomSerializedData<T>(this T self, ref SerializationInfo info) where T : ISerializable
        {
            Type typeofT = typeof(T);

            foreach (FieldInfo field in typeofT.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                //Skip serialization of the size field.
                if (field.FieldType == typeof(UInt16) && field.Name.EndsWith("_size"))
                    continue;

                //Use default serialization for non-char arrays.
                if (field.FieldType != typeof(Char[]))
                {
                    info.AddValue(field.Name, field.GetValue(self), field.FieldType);
                    continue;
                }

                SerializedArraySizeAttribute? serializedArraySizeAttribute = field.GetCustomAttribute<SerializedArraySizeAttribute>();
                if (serializedArraySizeAttribute == null)
                    continue;

                FieldInfo? size = typeofT.GetField($"{field.Name}_size", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (size == null || size.FieldType != typeof(UInt16))
                    continue;

                //Get the original array value and set the size field to the length of the array.
                //Throw an error if the array is larger than the size attribute.
                Char[] charArray = (Char[])field.GetValue(self);
                UInt16 arrayLength = (UInt16)charArray.Length;

                if (arrayLength > serializedArraySizeAttribute.Size)
                    throw new SerializationException($"The array '{field.Name}' is larger than the size attribute '{size.Name}'.");

                size.SetValue(self, arrayLength);
                info.AddValue(size.Name, arrayLength, typeof(UInt16));

                //Convert the array to a fixed length array and add it to the serialization info.
                Char[] fixedCharArray = new Char[serializedArraySizeAttribute.Size];
                charArray.CopyTo(fixedCharArray, 0);
                info.AddValue(field.Name, fixedCharArray, typeof(Char[]));
            }
        }

        public static void FromCustomSerializedData<T>(this T self, ref SerializationInfo info) where T : ISerializable
        {
            Type typeofT = typeof(T);

            foreach (FieldInfo field in typeofT.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                //Skip deserialization of the size field.
                if (field.FieldType == typeof(UInt16) && field.Name.EndsWith("_size"))
                    continue;

                //Use default deserialization for non-char arrays.
                if (field.FieldType != typeof(Char[]))
                {
                    field.SetValue(self, info.GetValue(field.Name, field.FieldType));
                    continue;
                }

                FieldInfo? size = typeofT.GetField($"{field.Name}_size", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (size == null || size.FieldType != typeof(UInt16))
                    continue;

                Char[]? serializedArray = (Char[]?)info.GetValue(field.Name, typeof(Char[]));
                if (serializedArray == null)
                    return;

                UInt16? serializedSize = (UInt16?)info.GetValue(size.Name, typeof(UInt16));
                if (serializedSize == null)
                    return;

                //https://stackoverflow.com/questions/9783191/setting-value-in-an-array-via-reflection
                Char[] charArray = (Char[])field.GetValue(self);
                //This creates a new array, which results in the source array being unaltered.
                //And setting the source array by reflection dosen't seem to work.
                //So for now, as I will only need to be accessing this internally, I will use the FromCharArray method.
                //Array.Resize(ref charArray, serializedSize.Value);
                for (int i = 0; i < serializedSize.Value; i++)
                    charArray[i] = serializedArray[i];

                size.SetValue(self, serializedSize.Value);
            }
        }
    }
}
