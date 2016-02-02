using System;
using System.Data;
using System.Reflection;

namespace Tom
{
    public class Column : TomParameter
    {
        public Column(PropertyInfo prop)
        {
            FieldName = prop.Name;
            SqlDbType = SqlMappings.SqlDbTypes[prop.PropertyType];
            IsNullable = prop.PropertyType.IsGenericType &&
                prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            EmptyValueFactory = SqlMappings.EmptyValueFactories[prop.PropertyType];

            FieldArguments = SqlMappings.FieldArguments[prop.PropertyType];
            DefaultFieldValue = SqlMappings.DefaultFieldValues[prop.PropertyType];
        }

        public string FieldArguments { get; private set; }
        public string DefaultFieldValue { get; set; }

        /// <summary>
        /// Configure this column to be secure.
        /// </summary>
        public override void Secure()
        {
            FieldArguments = SqlMappings.FieldArguments[typeof(byte[])];
            DefaultFieldValue = SqlMappings.DefaultFieldValues[typeof(byte[])];
            base.Secure();
        }

        public string FieldDeclaration(string tableName)
        {
            string declaration = string.Format("[{0}] [{1}]{2} {3} {4}",
                FieldName,
                SqlDbType.ToString().ToLower(),
                FieldArguments,
                IsNullable ? "NULL" : "NOT NULL",
                DefaultFieldValue == null ? null : ("CONSTRAINT [DF_" + tableName + "_" + FieldName + "]  DEFAULT " + DefaultFieldValue));

            return declaration;
        }
    }
}
