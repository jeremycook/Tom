using System;
using System.Data;
using System.Reflection;

namespace Tom
{
    public class Column
    {
        public Column(IRoot root, PropertyInfo property)
        {
            Root = root;
            Mapped = true;

            FieldName = property.Name;
            SqlDbType = GetSqlDbType(property);
            Arguments = GetArguments(property);
            IsNullable = property.PropertyType.IsGenericType &&
                property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            DefaultValue = GetDefaultValue(property);
        }

        private static SqlDbType GetSqlDbType(PropertyInfo property)
        {
            if (!SqlMappings.SqlDbTypes.ContainsKey(property.PropertyType))
            {
                throw new ArgumentException(string.Format("SqlMappings.SqlDbTypes does not contain the type '{0}'.", property.PropertyType), "property");
            }

            return SqlMappings.SqlDbTypes[property.PropertyType];
        }

        private static string GetDefaultValue(PropertyInfo property)
        {
            if (!SqlMappings.DefaultValues.ContainsKey(property.PropertyType))
            {
                throw new ArgumentException(string.Format("SqlMappings.DefaultValues does not contain the type '{0}'.", property.PropertyType), "property");
            }

            return SqlMappings.DefaultValues[property.PropertyType];
        }

        private static string GetArguments(PropertyInfo property)
        {
            if (!SqlMappings.Arguments.ContainsKey(property.PropertyType))
            {
                throw new ArgumentException(string.Format("SqlMappings.Arguments does not contain the type '{0}'.", property.PropertyType), "property");
            }
            
            return SqlMappings.Arguments[property.PropertyType];
        }

        public IRoot Root { get; private set; }
        public bool Mapped { get; private set; }

        public string FieldName { get; private set; }
        public SqlDbType SqlDbType { get; private set; }
        public string Arguments { get; private set; }
        public bool IsNullable { get; private set; }
        public string DefaultValue { get; private set; }

        public string Declaration()
        {
            string declaration = string.Format("[{0}] [{1}]{2} {3} {4}",
                FieldName,
                SqlDbType.ToString().ToLower(),
                Arguments,
                IsNullable ? "NULL" : "NOT NULL",
                DefaultValue == null ? null : ("CONSTRAINT [DF_" + Root.TableName + "_" + FieldName + "]  DEFAULT " + DefaultValue));

            return declaration;
        }
    }
}