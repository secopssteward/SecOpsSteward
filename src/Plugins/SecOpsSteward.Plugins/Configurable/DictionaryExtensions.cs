using System;
using System.Collections.Generic;

namespace SecOpsSteward.Plugins.Configurable
{
    public static class DictionaryExtensions
    {
        /// <summary>
        ///     Create an instance of an object from a Dictionary of values
        /// </summary>
        /// <typeparam name="T">Object type to create</typeparam>
        /// <param name="dict">Dictionary of values</param>
        /// <returns>Populated object</returns>
        public static T AsObject<T>(this IDictionary<string, object> dict)
        {
            return (T) dict.AsObject(typeof(T));
        }

        /// <summary>
        ///     Create an instance of an object from a Dictionary of values
        /// </summary>
        /// <param name="values">Dictionary of values</param>
        /// <param name="objectType">Object type to create</param>
        /// <returns>Populated object</returns>
        public static object AsObject(this IDictionary<string, object> values, Type objectType)
        {
            var configObject = Activator.CreateInstance(objectType);
            foreach (var prop in objectType.GetProperties())
                if (values.ContainsKey(prop.Name))
                    prop.SetValue(configObject, Convert.ChangeType(values[prop.Name], prop.PropertyType));
            return configObject;
        }

        /// <summary>
        ///     Convert the properties of an object to a Dictionary of values
        /// </summary>
        /// <typeparam name="T">Type of object to convert</typeparam>
        /// <param name="obj">Object to convert</param>
        /// <returns>Object properties as a Dictionary of values</returns>
        public static Dictionary<string, object> AsDictionaryProperties<T>(this T obj)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in obj.GetType().GetProperties()) dict[prop.Name] = prop.GetValue(obj);
            return dict;
        }
    }
}