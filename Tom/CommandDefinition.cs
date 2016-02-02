using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Tom
{
    public class CommandDefinition
    {
        public static CommandDefinition Create(object example)
        {
            if (example == null)
            {
                throw new ArgumentNullException();
            }
            else if (example is IDictionary<string, object>)
            {
                var dict = example as IDictionary<string, object>;
                var qd = new CommandDefinition
                {
                    Parameters = dict
                        .Select(o => new SqlParameter(o.Key, o.Value))
                        .ToArray(),
                };
                qd.UpdateValues = qd.DictionaryParametersUpdater;
                return qd;
            }
            else if (example is IEnumerable<SqlParameter>)
            {
                var qd = new CommandDefinition
                {
                    Parameters = (example as IEnumerable<SqlParameter>)
                        .Select(param => CopySqlParameter(param))
                        .ToArray(),
                };
                qd.UpdateValues = qd.ParametersParametersUpdater;
                return qd;
            }
            else
            {
                var qd = new CommandDefinition
                {
                    Parameters = example.GetType().GetProperties()
                        .Select(o => new { o.Name, Type = o.PropertyType, Value = o.GetValue(example) })
                        .Select(o => new SqlParameter(o.Name, SqlMappings.SqlDbTypes[o.Type])
                        {
                            IsNullable = o.Type.IsGenericType &&
                                o.Type.GetGenericTypeDefinition() == typeof(Nullable<>),
                        })
                        .ToArray(),
                };
                qd.UpdateValues = qd.ObjectParametersUpdater;
                return qd;
            }
        }

        private static SqlParameter CopySqlParameter(SqlParameter param)
        {
            return new SqlParameter(
                parameterName: param.ParameterName,
                dbType: param.SqlDbType,
                size: param.Size,
                direction: param.Direction,
                isNullable: param.IsNullable,
                precision: param.Precision,
                scale: param.Scale,
                sourceColumn: param.SourceColumn,
                sourceVersion: param.SourceVersion,
                value: param.Value);
        }

        /// <summary>
        /// Use the static <see cref="Create(object)"/> method instead.
        /// </summary>
        private CommandDefinition() { }

        public SqlParameter[] Parameters { get; private set; }

        public Action<object> UpdateValues { get; private set; }

        private void DictionaryParametersUpdater(object item)
        {
            var dict = (IDictionary<string, object>)item;
            foreach (var param in Parameters)
            {
                object value = dict[param.ParameterName];
                if (value is SqlParameter)
                {
                    param.Value = (value as SqlParameter).Value;
                }
                else if (value == null)
                {
                    param.Value = DBNull.Value;
                }
                else
                {
                    param.Value = value;
                }
            }
        }

        private void ParametersParametersUpdater(object item)
        {
            var dict = (item as IEnumerable<SqlParameter>).ToDictionary(o => o.ParameterName);
            foreach (var param in Parameters)
            {
                param.Value = dict[param.ParameterName].Value;
            }
        }

        private void ObjectParametersUpdater(object item)
        {
            var props = item.GetType().GetProperties().ToDictionary(o => o.Name);
            foreach (var param in Parameters)
            {
                object value = props[param.ParameterName].GetValue(item);
                if (value is SqlParameter)
                {
                    param.Value = (value as SqlParameter).Value;
                }
                else if (value == null)
                {
                    param.Value = SqlMappings.EmptyValueFactories[props[param.ParameterName].PropertyType]();
                }
                else
                {
                    param.Value = value;
                }
            }
        }

        public IEnumerable<object> With(IEnumerable<object> args)
        {
            foreach (var item in args)
            {
                UpdateValues(item);
                yield return item;
            }
        }
    }
}