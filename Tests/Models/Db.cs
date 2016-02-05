using System.Linq;
using Tom;

namespace Tests.Models
{
    public class Db : TomBase
    {
        public Db(string connectionStringOrName) : base(connectionStringOrName) { }

        protected override void Configure()
        {
            Secure.ConfigureAllColumns(c => c.Secure(), c => !Secure.PrimaryKey.Contains(c));
        }

        public ITable<Bar> Bars { get; private set; }
        public ITable<Foo> Foos { get; private set; }
        public ITable<Secure> Secure { get; private set; }
    }
}
