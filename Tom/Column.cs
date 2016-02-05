namespace Tom
{
    using System.Reflection;

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
                var fieldSettings = Settings.Current.GetFieldSettings(prop.PropertyType);
                FieldArguments = fieldSettings.FieldArguments;
                FieldDefault = fieldSettings.FieldDefault;
            }
        }

        public Field Field { get; private set; }
        public string FieldArguments { get; private set; }
        public string FieldDefault { get; set; }

        /// <summary>
        /// Configure this column to be secure.
        /// </summary>
        public void Secure()
        {
            var fieldSettings = Settings.Current.GetFieldSettings(typeof(byte[]));
            FieldArguments = fieldSettings.FieldArguments;
            FieldDefault = fieldSettings.FieldDefault;
            Field.Secure();
        }
    }
}
