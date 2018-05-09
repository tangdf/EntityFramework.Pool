using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFramework.Pool.Test
{
    using System.Reflection;
    using Xunit.Abstractions;

    public static class ObjectAssert
    {
        public static ITestOutputHelper TestOutputHelper;



        public static void Equal<T>(T t1, T t2)
        {
            Equal(t1, t2, "Root", 0);

            TestOutputHelper.WriteLine(_logBuilder.ToString());
        }


   private  static readonly StringBuilder _logBuilder=new StringBuilder();

        private static void Equal(object o1, object o2, string propertyName,  int depth)
        {
            string IsNullOrNotNull(object t)
            {
                return t == null ? "null" : "not null";
            }

            if(object.ReferenceEquals(o1, o2))
             return;


            if (o1 == null || o2 == null)
            {
                AppendIndent(depth);
                _logBuilder.AppendFormat("{0} {1} {2}", propertyName, IsNullOrNotNull(o1), IsNullOrNotNull(o2));
                TestOutputHelper.WriteLine(_logBuilder.ToString());
                _logBuilder.Clear();
                return;
            }

            if (o1.GetType() != o2.GetType())
            {
                AppendIndent(depth);
                _logBuilder.AppendFormat("{0} {1} {2}", propertyName, o1.GetType().FullName, o2.GetType().FullName).AppendLine();
                TestOutputHelper.WriteLine(_logBuilder.ToString());
                _logBuilder.Clear();
                return;
            }
            var type = o1.GetType();
            if (IsSimpleType(type))
            {
                var string1 = o1.ToString();
                var string2 = o2.ToString();

                if (string1 != string2)
                {
                    AppendIndent(depth);
                    _logBuilder.AppendFormat("{0} {1} {2}", propertyName, string1, string2).AppendLine();
                    TestOutputHelper.WriteLine(_logBuilder.ToString());
                    _logBuilder.Clear();
                }
                return;
            }


            foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public))
            {
                 
                try
                {
                    var value1 = fieldInfo.GetValue(o1);
                    var value2 = fieldInfo.GetValue(o2);

                    Equal(value1, value2, fieldInfo.ToString(), depth + 1);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                
            }
        }

        private static void AppendIndent(int depth)
        {
            for (int index = 0; index < depth; index++)
            {
                _logBuilder.Append('\t');
            }
        }
        private static bool IsSimpleType(Type type)
        {
            return type.GetTypeInfo().IsPrimitive ||
                   type.Equals(typeof(decimal)) ||
                   type.Equals(typeof(string)) ||
                   type.Equals(typeof(DateTime)) ||
                   type.Equals(typeof(Guid)) ||
                   type.Equals(typeof(DateTimeOffset)) ||
                   type.Equals(typeof(TimeSpan)) ||
                   type.Equals(typeof(Uri));
        }
    }
}
