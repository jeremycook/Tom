using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Tom
{
    /// <summary>
    /// Creates a <see cref="System.Data.SqlClient.SqlParameter"/> based on a
    /// <see cref="Tom.Field"/>, and pairs the two together. Useful when 
    /// executing a parameterized SQL command with a list of values.
    /// </summary>
    public class FieldParameter
    {
        public FieldParameter(Field field)
        {
            Field = field;
            SqlParameter = new SqlParameter
            {
                ParameterName = field.Name,
                SqlDbType = field.SqlDbType,
                IsNullable = field.IsNullable,
            };
        }

        public Field Field { get; private set; }
        public SqlParameter SqlParameter { get; private set; }


        public static IEnumerable<FieldParameter> CreateParameters(IEnumerable<Field> fields)
        {
            return fields.Select(o => new FieldParameter(o));
        }

        public static void UpdateSqlParameterValues<TCommandModel>(IEnumerable<FieldParameter> fieldParameters, TCommandModel commandModel)
        {
            if (commandModel == null)
            {
                throw new ArgumentNullException("commandModel");
            }

            var props = typeof(TCommandModel).GetProperties().ToDictionary(o => o.Name);
            foreach (var param in fieldParameters)
            {
                var value = props[param.Field.Name].GetValue(commandModel);
                var sqlvalue = param.Field.GetSqlParameterValue(value);
                param.SqlParameter.Value = sqlvalue;
            }
        }
    }
}
