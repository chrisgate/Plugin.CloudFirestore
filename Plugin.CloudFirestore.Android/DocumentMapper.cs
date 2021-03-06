﻿using System;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Plugin.CloudFirestore.Attributes;
using System.Reflection;

namespace Plugin.CloudFirestore
{
    public static class DocumentMapper
    {
        public static IDictionary<string, object> Map(DocumentSnapshot source)
        {
            if (source != null && source.Exists())
            {
                return source.Data.ToDictionary(pair => pair.Key, pair => pair.Value.ToFieldValue(typeof(object)));
            }
            return null;
        }

        public static T Map<T>(DocumentSnapshot source) where T : class
        {
            if (source != null && source.Exists())
            {
                var properties = typeof(T).GetProperties();
                var idProperties = properties.Where(p => Attribute.GetCustomAttribute(p, typeof(IdAttribute)) != null);
                var mappedProperties = properties.Select(p => (Property: p, Attribute: Attribute.GetCustomAttribute(p, typeof(MapToAttribute)) as MapToAttribute))
                                                 .Where(t => t.Attribute != null)
                                                 .ToDictionary(t => t.Attribute.Mapping, t => t.Property);
                var igonoredProperties = properties.Where(p => Attribute.GetCustomAttribute(p, typeof(IgnoredAttribute)) != null);

                var instance = Activator.CreateInstance<T>();
                if (source != null)
                {
                    foreach (var (key, value) in source.Data)
                    {
                        try
                        {
                            PropertyInfo property;
                            if (mappedProperties.ContainsKey(key))
                            {
                                property = mappedProperties[key];
                            }
                            else
                            {
                                property = typeof(T).GetProperty(key);
                            }

                            if (property != null && !igonoredProperties.Contains(property))
                            {
                                property.SetValue(instance, value.ToFieldValue(property.PropertyType));
                            }
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine($"{key} is invalid: {e.Message}");
                            throw;
                        }
                    }
                }

                foreach (var idProperty in idProperties)
                {
                    idProperty.SetValue(instance, source.Id);
                }

                return instance;
            }
            return null;
        }
    }
}
