using System;
using System.Data;
using System.Reflection;

namespace Tom
{
    /// <summary>
    /// Describes a column for <see cref="ITable"/>'s.
    /// </summary>
    public class Column
    {
        public Column(PropertyInfo prop)
        {
            Field = new Field(prop.PropertyType, prop.Name);
            if (Field.IsMapped)
            {
                FieldArguments = SqlMappings.FieldArguments[prop.PropertyType];
                DefaultFieldValue = SqlMappings.DefaultFieldValues[prop.PropertyType];
            }
        }

        public Field Field { get; private set; }
        public string FieldArguments { get; private set; }
        public string DefaultFieldValue { get; set; }

        /// <summary>
        /// Configure this column to be secure.
        /// </summary>
        public void Secure()
        {
            FieldArguments = SqlMappings.FieldArguments[typeof(byte[])];
            DefaultFieldValue = SqlMappings.DefaultFieldValues[typeof(byte[])];
            Field.Secure();
        }
    }
}
