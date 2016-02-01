using System;
using System.Collections.Generic;
using System.Linq;

namespace Tom
{
    public interface IRoot
    {
        Type ModelType { get; }
        string TableName { get; }
        IEnumerable<Column> Columns { get; }
        IEnumerable<string> PrimaryKey { get; }
    }

    public class Root<TModel> : IRoot
    {
        public Root()
        {
            ModelType = typeof(TModel);
            TableName = ModelType.Name;
            Columns = ModelType.GetProperties()
                .Select(p => new Column(this, p))
                .ToArray();
            PrimaryKey = Columns.Take(1).Select(o => o.FieldName).ToArray();
        }

        public Type ModelType { get; private set; }
        public string TableName { get; set; }
        public IEnumerable<Column> Columns { get; private set; }
        public IEnumerable<string> PrimaryKey { get; set; }
    }
}