using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tom
{
    public class Field
    {
        public Field(Type type, string name)
        {
            IsSecure = false;
            IsMapped = SqlMappings.IsMapped(type);
            Type = type;
            Name = name;
            IsNullable = SqlMappings.IsNullable(type);
            SqlDbType = SqlMappings.GetSqlDbTypes(type);
            EmptyValueFactory = SqlMappings.GetEmptyValueFactories(type);
            GetDbValue = value => DefaultGetDbValue(this, value);
        }

        /// <summary>
        /// A secured field's value will be encrypted when stored in the
        /// database, and decrypted when read from the database.
        /// </summary>
        public bool IsSecure { get; private set; }

        public bool IsMapped { get; set; }
        public Type Type { get; private set; }
        public string Name { get; private set; }
        public bool IsNullable { get; set; }
        public SqlDbType SqlDbType { get; set; }
        public Func<object> EmptyValueFactory { get; set; }

        /// <summary>
        /// Configure this field as secured.
        /// </summary>
        public virtual void Secure()
        {
            SqlDbType = SqlMappings.GetSqlDbTypes(typeof(byte[]));
            EmptyValueFactory = SqlMappings.GetEmptyValueFactories(typeof(byte[]));
            IsSecure = true;
        }

        public Func<object, object> GetDbValue { get; set; }

        public static object DefaultGetDbValue(Field field, object value)
        {
            if (value == null)
            {
                return field.EmptyValueFactory();
            }
            else if (field.IsSecure)
            {
                // TODO: Actually encrypt value.
                return Encoding.UTF8.GetBytes(value.ToString());
            }
            else
            {
                return value;
            }
        }

        public static IEnumerable<Field> CreateFieldsFromType(Type modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException("modelType");
            }

            var props = modelType.GetProperties();
            var fields = new Field[props.Length];

            var i = 0;
            foreach (var prop in props)
            {
                var field = new Field(prop.PropertyType, prop.Name);

                fields[i++] = field;
            }

            return fields;
        }
    }
}
