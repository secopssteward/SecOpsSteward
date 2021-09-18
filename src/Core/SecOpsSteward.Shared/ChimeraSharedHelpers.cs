using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SecOpsSteward.Shared
{
    /// <summary>
    ///     Commonly used commands in the Chimera system
    /// </summary>
    public static class ChimeraSharedHelpers
    {
        /// <summary>
        ///     Retrieve the SHA hash of an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object instance to hash</param>
        /// <returns>SHA hash</returns>
        public static byte[] GetHash<T>(T obj)
        {
            return GetHash(SerializeToBytes(obj));
        }

        /// <summary>
        ///     Retrieve the SHA hash of a set of byte arrays
        /// </summary>
        /// <param name="byteSets">Collection of byte arrays to hash</param>
        /// <returns>SHA hash</returns>
        public static byte[] GetHash(params byte[][] byteSets)
        {
            return GetHash(byteSets.SelectMany(b => b).ToArray());
        }

        /// <summary>
        ///     Retrieve the SHA hash of a string
        /// </summary>
        /// <param name="str">String to hash</param>
        /// <returns>SHA hash</returns>
        public static byte[] GetHash(string str)
        {
            return GetHash(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        ///     Retrieve the SHA hash of a byte array
        /// </summary>
        /// <param name="bytes">Byte array to hash</param>
        /// <returns>SHA hash</returns>
        public static byte[] GetHash(byte[] bytes)
        {
            return SHA256.Create().ComputeHash(bytes);
        }

        /// <summary>
        ///     Retrieve the SHA hash of a Stream
        /// </summary>
        /// <param name="stream">Stream to hash</param>
        /// <returns>SHA hash</returns>
        public static byte[] GetStreamHash(Stream stream)
        {
            return SHA256.Create().ComputeHash(stream);
        }

        /// <summary>
        ///     Serialize an object to bytes
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object instance to serialize</param>
        /// <returns>Serialized bytes</returns>
        public static byte[] SerializeToBytes<T>(T obj)
        {
            return Encoding.UTF8.GetBytes(SerializeToString(obj));
        }

        /// <summary>
        ///     Serialize an object to a string
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object instance to serialize</param>
        /// <param name="indented">If the resulting string should be indented</param>
        /// <returns>Serialized string</returns>
        public static string SerializeToString<T>(T obj, bool indented = false)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions {WriteIndented = indented});
        }

        /// <summary>
        ///     Get an object instance from serialized bytes
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="bytes">Serialized bytes</param>
        /// <returns>Populated object instance</returns>
        public static T GetFromSerializedBytes<T>(byte[] bytes)
        {
            return GetFromSerializedString<T>(Encoding.UTF8.GetString(bytes));
        }

        /// <summary>
        ///     Get an object instance from a serialized string
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="str">Serialized string</param>
        /// <returns>Populated object instance</returns>
        public static T GetFromSerializedString<T>(string str)
        {
            return JsonSerializer.Deserialize<T>(str);
        }

        /// <summary>
        ///     Get an object instance from serialized bytes
        /// </summary>
        /// <param name="bytes">Serialized bytes</param>
        /// <param name="targetType">Expected type</param>
        /// <returns>Populated object instance</returns>
        public static object GetFromSerializedBytes(byte[] bytes, Type targetType)
        {
            return GetFromSerializedString(Encoding.UTF8.GetString(bytes), targetType);
        }

        /// <summary>
        ///     Get an object instance from a serialized string
        /// </summary>
        /// <param name="str">Serialized string</param>
        /// <param name="targetType">Expected type</param>
        /// <returns>Populated object instance</returns>
        public static object GetFromSerializedString(string str, Type targetType)
        {
            return JsonSerializer.Deserialize(str, targetType);
        }

        /// <summary>
        ///     Generate a random alphanumeric string of given length
        /// </summary>
        /// <param name="length">String length</param>
        /// <returns>Random string</returns>
        public static string RandomString(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            var res = new StringBuilder();
            using (var rng = new RNGCryptoServiceProvider())
            {
                var uintBuffer = new byte[sizeof(uint)];

                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    var num = BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(valid[(int) (num % (uint) valid.Length)]);
                }
            }

            return res.ToString();
        }
    }
}