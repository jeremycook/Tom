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
            GetObjectValue = value => DefaultGetObjectValue(this, value);
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
            IsSecure = true;
        }

        public Func<object, object> GetObjectValue { get; set; }
        public Func<object, object> GetSqlParameterValue { get; set; }

        public static object DefaultGetObjectValue(Field field, object sqlvalue)
        {
            var objectValue = sqlvalue == DBNull.Value ? null : sqlvalue;

            if (field.IsSecure)
            {
                if (objectValue == null)
                {
                    return objectValue;
                }
                else
                {
                    var decryptedBytes = Settings.Current.Encryptor.Decrypt(objectValue as byte[]);
                    if (field.Type == typeof(byte[]))
                    {
                        return decryptedBytes;
                    }
                    else if (decryptedBytes.Any())
                    {
                        string text = Encoding.Unicode.GetString(decryptedBytes);
                        if (field.Type == typeof(Guid) || field.Type == typeof(Guid?))
                        {
                            return Guid.Parse(text);
                        }
                        else if (field.Type == typeof(DateTimeOffset) || field.Type == typeof(DateTimeOffset?))
                        {
                            return DateTimeOffset.Parse(text);
                        }
                        else
                        {
                            return Convert.ChangeType(text, field.Type);
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return objectValue;
            }
        }

        public static object DefaultGetSqlParameterValue(Field field, object objectValue)
        {
            var parameterValue = objectValue ?? field.EmptyValueFactory();

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
