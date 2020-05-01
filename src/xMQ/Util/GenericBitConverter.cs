using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ.Util
{
    internal static class GenericBitConverter
    {
        public static byte[] GetBytes(object value)
        {
            var type = value.GetType();

            if (type == typeof(byte))
            {
                return new byte[] { (byte)value };
            }
            else if (type == typeof(byte[]))
            {
                return (byte[]) value;
            }
            else if (type == typeof(sbyte))
            {
                return BitConverter.GetBytes((sbyte)value);
            }
            else if (type == typeof(short))
            {
                return BitConverter.GetBytes((short)value);
            }
            else if (type == typeof(ushort))
            {
                return BitConverter.GetBytes((ushort)value);
            }
            else if (type == typeof(int))
            {
                return BitConverter.GetBytes((int)value);
            }
            else if (type == typeof(uint))
            {
                return BitConverter.GetBytes((uint)value);
            }
            else if (type == typeof(long))
            {
                return BitConverter.GetBytes((long)value);
            }
            else if (type == typeof(ulong))
            {
                return BitConverter.GetBytes((ulong)value);
            }
            else if (type == typeof(string))
            {
                return Encoding.UTF8.GetBytes(value.ToString());
            }
            else if (type == typeof(Guid))
            {
                return ((Guid)value).ToByteArray();
            }

            return null;
        }

        public static T GetValue<T>(byte[] array, int startIndex, out int size)
        {
            size = 0;
            object result = null;
            if (typeof(T) == typeof(byte))
            {
                size = sizeof(byte);

                var arr = GetValue(array, startIndex, size);
                result = arr.Length > 0 ? arr[0] : 0;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                size = sizeof(sbyte);

                var arr = GetValue(array, startIndex, size);
                result = arr.Length > 0 ? (sbyte)arr[0] : (sbyte)0;
            }
            else if (typeof(T) == typeof(ushort))
            {
                size = sizeof(ushort);

                var arr = GetValue(array, startIndex, size);
                result = BitConverter.ToUInt16(arr, 0);
            }
            else if (typeof(T) == typeof(short))
            {
                size = sizeof(short);

                var arr = GetValue(array, startIndex, size);
                result = BitConverter.ToInt16(arr, 0);
            }
            else if (typeof(T) == typeof(uint))
            {
                size = sizeof(uint);

                var arr = GetValue(array, startIndex, size);
                result = BitConverter.ToUInt32(arr, 0);
            }
            else if (typeof(T) == typeof(int))
            {
                size = sizeof(int);

                var arr = GetValue(array, startIndex, size);
                result = BitConverter.ToInt32(arr, 0);
            }
            else if (typeof(T) == typeof(ulong))
            {
                size = sizeof(ulong);

                var arr = GetValue(array, startIndex, size);
                result = BitConverter.ToUInt64(arr, 0);
            }
            else if (typeof(T) == typeof(long))
            {
                size = sizeof(long);

                var arr = GetValue(array, startIndex, size);
                result = BitConverter.ToInt64(arr, 0);
            }
            else if (typeof(T) == typeof(float))
            {
                size = sizeof(float);

                var arr = GetValue(array, startIndex, size);
                result = BitConverter.ToSingle(arr, 0);
            }
            else if (typeof(T) == typeof(double))
            {
                size = sizeof(double);

                var arr = GetValue(array, startIndex, size);
                result = BitConverter.ToDouble(arr, 0);
            }
            else if (typeof(T) == typeof(char))
            {
                size = sizeof(char);

                var arr = GetValue(array, startIndex, size);
                result = BitConverter.ToChar(arr, 0);
            }
            else if (typeof(T) == typeof(bool))
            {
                size = sizeof(bool);

                var arr = GetValue(array, startIndex, size);
                result = BitConverter.ToBoolean(arr, 0);
            }
            else if (typeof(T) == typeof(Guid))
            {
                size = 16;
                var arr = GetValue(array, startIndex, size);
                result = new Guid(arr);
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        private static byte[] GetValue(byte[] array, int startIndex, int length)
        {
            var arr = new byte[length];
            Array.Copy(array, startIndex, arr, 0, length);

            return arr;
        }
    }
}
