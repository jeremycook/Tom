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
            IsMapped = Settings.Current.IsMapped(type);
            Type = type;
            Name = name;
            IsNullable = Settings.Current.IsNullable(type);
            SqlDbType = Settings.Current.GetSqlDbTypes(type);
            EmptyValueFactory = Settings.Current.GetEmptyValueFactories(type);
            GetSqlParameterValue = value => DefaultGetSqlParameterValue(this, value);
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
            SqlDbType = Settings.Current.GetSqlDbTypes(typeof(byte[]));
            EmptyValueFactory = Settings.Current.GetEmptyValueFactories(typeof(byte[]));
            IsSecure = true;
        }

        public Func<object, object> GetSqlParameterValue { get; set; }

        public static object DefaultGetSqlParameterValue(Field field, object value)
        {
            var parameterValue = value ?? field.EmptyValueFactory();

            if (field.IsSecure)
            {
                if (parameterValue == DBNull.Value || parameterValue == null)
                {
                    return parameterValue;
                }
                else if (parameterValue is byte[])
                {
                    return Settings.Current.Encryptor.Encrypt(parameterValue as byte[]);
                }
                else
                {
                    byte[] clearBytes = Encoding.Unicode.GetBytes(parameterValue.ToString());
                    return Settings.Current.Encryptor.Encrypt(clearBytes);
                }
            }
            else
            {
                return parameterValue;
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
