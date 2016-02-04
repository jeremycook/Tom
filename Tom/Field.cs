using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tom
{
    public class Field
    {
        public Field(Type type, string name)
        {
            var fieldSettings = Settings.Current.GetFieldSettings(type);
            IsSecure = false;
            IsMapped = fieldSettings.IsMapped;
            IsSerialized = fieldSettings.IsSerialized;
            Type = type;
            Name = name;
            IsNullable = fieldSettings.IsNullable;
            SqlDbType = fieldSettings.SqlDbType;
            EmptyValueFactory = fieldSettings.EmptyValueFactory;
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
            SqlDbType = Settings.Current.GetFieldSettings(typeof(byte[])).SqlDbType;
            IsSecure = true;
        }

        public Func<object, object> GetObjectValue { get; set; }
        public Func<object, object> GetSqlParameterValue { get; set; }
        public bool IsSerialized { get; private set; }

        public static object DefaultGetObjectValue(Field field, object sqlvalue)
        {
            var objectValue = sqlvalue == DBNull.Value ? null : sqlvalue;

            if (field.IsSecure && objectValue != null)
            {
                var decryptedBytes = Settings.Current.Encryptor.Decrypt(objectValue as byte[]);
                if (field.IsSerialized)
                {
                    objectValue = decryptedBytes;
                }
                else if (field.Type == typeof(byte[]))
                {
                    objectValue = decryptedBytes;
                }
                else if (field.Type == typeof(DateTime) || field.Type == typeof(DateTime?))
                {
                    var ticks = BitConverter.ToInt64(decryptedBytes, 0);
                    objectValue = new DateTime(ticks);
                }
                else
                {
                    string text = Encoding.UTF8.GetString(decryptedBytes);

                    if (field.Type == typeof(Guid) || field.Type == typeof(Guid?))
                    {
                        objectValue = Guid.Parse(text);
                    }
                    else if (field.Type == typeof(DateTimeOffset) || field.Type == typeof(DateTimeOffset?))
                    {
                        objectValue = DateTimeOffset.Parse(text);
                    }
                    else if (field.Type.IsGenericType && field.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        objectValue = Convert.ChangeType(text, field.Type.GetGenericArguments()[0]);
                    }
                    else
                    {
                        objectValue = Convert.ChangeType(text, field.Type);
                    }
                }
            }

            if (field.IsSerialized && objectValue != null)
            {
                switch (field.SqlDbType)
                {
                    case SqlDbType.NVarChar:
                        objectValue = JsonConvert.DeserializeObject((string)objectValue, field.Type);
                        break;
                    case SqlDbType.VarBinary:
                        byte[] data = (byte[])objectValue;
                        using (MemoryStream ms = new MemoryStream(data))
                        using (var reader = new BsonReader(ms))
                        {
                            reader.ReadRootValueAsArray =
                                typeof(IEnumerable).IsAssignableFrom(field.Type) &&
                                !typeof(IDictionary).IsAssignableFrom(field.Type) &&
                                field.Type != typeof(string);

                            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
                            objectValue = jsonSerializer.Deserialize(reader, field.Type);
                        }
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Cannot deserialize from SqlDbType.{0}. Only NVarChar and VarBinary are supported.", field.SqlDbType));
                }
            }

            return objectValue;
        }

        public static object DefaultGetSqlParameterValue(Field field, object objectValue)
        {
            object parameterValue = objectValue ?? field.EmptyValueFactory();

            if (field.IsSerialized && parameterValue != null)
            {
                switch (field.SqlDbType)
                {
                    case SqlDbType.NVarChar:
                        parameterValue = JsonConvert.SerializeObject(parameterValue);
                        break;
                    case SqlDbType.VarBinary:
                        using (MemoryStream ms = new MemoryStream())
                        using (BsonWriter writer = new BsonWriter(ms))
                        {
                            JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
                            jsonSerializer.Serialize(writer, parameterValue);
                            parameterValue = ms.ToArray();
                        }
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Cannot serialize to SqlDbType.{0}. Only NVarChar abd VarBinary are supported.", field.SqlDbType));
                }
            }

            if (field.IsSecure && parameterValue != null)
            {
                if (parameterValue is byte[])
                {
                    parameterValue = Settings.Current.Encryptor.Encrypt((byte[])parameterValue);
                }
                else if (parameterValue is DateTime?)
                {
                    byte[] clearBytes = BitConverter.GetBytes(((DateTime?)parameterValue).Value.Ticks);
                    parameterValue = Settings.Current.Encryptor.Encrypt(clearBytes);
                }
                else if (parameterValue is DateTimeOffset?)
                {
                    byte[] clearBytes = Encoding.UTF8.GetBytes(((DateTimeOffset?)parameterValue).Value.ToString("O"));
                    parameterValue = Settings.Current.Encryptor.Encrypt(clearBytes);
                }
                else
                {
                    byte[] clearBytes = Encoding.UTF8.GetBytes(parameterValue.ToString());
                    parameterValue = Settings.Current.Encryptor.Encrypt(clearBytes);
                }
            }

            return parameterValue ?? DBNull.Value;
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
