using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Tom
{
    internal class QueryDefinition
    {
        public static QueryDefinition Create(object example)
        {
            if (example == null)
            {
                throw new ArgumentNullException();
            }
            else if (example is IDictionary<string, object>)
            {
                var dict = example as IDictionary<string, object>;
                var qd = new QueryDefinition
                {
                    Parameters = dict
                        .Select(o => o.Value is SqlParameter ?
                            (SqlParameter)o.Value :
                            new SqlParameter(o.Key, o.Value)
                        )
                        .ToArray(),
                };
                qd.UpdateValues = qd.DictionaryParametersUpdater;
                return qd;
            }
            else if (example is IEnumerable<SqlParameter>)
            {
                var qd = new QueryDefinition
                {
                    Parameters = (example as IEnumerable<SqlParameter>)
                        .ToArray(),
                };
                qd.UpdateValues = qd.ParametersParametersUpdater;
                return qd;
            }
            else
            {
                var qd = new QueryDefinition
                {
                    Parameters = example.GetType().GetProperties()
                        .Select(o => new SqlParameter(o.Name, o.GetValue(example)))
                        .ToArray(),
                };
                qd.UpdateValues = qd.ObjectParametersUpdater;
                return qd;
            }
        }

        public SqlParameter[] Parameters { get; private set; }

        public Action<object> UpdateValues { get; private set; }

        private void DictionaryParametersUpdater(object item)
        {
            var dict = (IDictionary<string, object>)item;
            foreach (var param in Parameters)
            {
                param.Value = dict[param.ParameterName] is SqlParameter ?
                    (dict[param.ParameterName] as SqlParameter).Value :
                    dict[param.ParameterName];
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
                param.Value = value is SqlParameter ?
                    (value as SqlParameter).Value :
                    value;
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