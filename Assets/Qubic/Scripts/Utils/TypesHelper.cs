using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QubicNS
{
    public static class TypesHelper
    {
        public static IEnumerable<T> GetStaticFieldValuesOfType<T>(this Type staticClassType)
        {
            return staticClassType
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.FieldType == typeof(T))
                .Select(field => (T)field.GetValue(null));
        }

        public static IEnumerable<FieldInfo> GetStaticFieldsOfType<T>(this Type staticClassType)
        {
            return staticClassType
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.FieldType == typeof(T));
        }

        public static IEnumerable<MethodInfo> GetStaticMethods(this Type staticClassType, Type returnType)
        {
            return staticClassType
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.ReturnType == returnType);
        }

        public static IEnumerable<Type> GetDerivedTypes<TBase>(params Assembly[] assemblies)
        {
            var baseType = typeof(TBase);
            if (assemblies.Length == 0)
            {
                var a1 = Assembly.GetExecutingAssembly();
                var a2 = Assembly.GetEntryAssembly();
                if (a1 == a2)
                    return GetDerivedTypes<TBase>(a1);
                else
                    return GetDerivedTypes<TBase>(a1).Concat(GetDerivedTypes<TBase>(a2));
            }
            else
                return assemblies.SelectMany(a => GetDerivedTypes<TBase>(a));
        }

        public static IEnumerable<Type> GetDerivedTypes<TBase>(Assembly assembly)
        {
            var baseType = typeof(TBase);
            IEnumerable<Type> types;
            if (assembly == null)
                return Array.Empty<Type>();

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            return types.Where(type => type != null && type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type));
        }

        public static IEnumerable<TBase> GetDerivedTypeInstances<TBase>(params Assembly[] assemblies)
        {
            return GetDerivedTypes<TBase>(assemblies)
                .Where(type => type.GetConstructor(Type.EmptyTypes) != null)
                .Select(type => (TBase)Activator.CreateInstance(type));
        }
    }
}