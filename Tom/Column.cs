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
                FieldArguments = Settings.Current.FieldArguments[prop.PropertyType];
                DefaultFieldValue = Settings.Current.DefaultFieldValues[prop.PropertyType];
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
            FieldArguments = Settings.Current.FieldArguments[typeof(byte[])];
            DefaultFieldValue = Settings.Current.DefaultFieldValues[typeof(byte[])];
            Field.Secure();
        }
    }
}
