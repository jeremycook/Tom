using Tom;

namespace Tests.Models
{
    public class Db : TomBase
    {
        public Db(string connectionStringOrName) : base(connectionStringOrName) { }

        protected override void Configure()
        {
            Secure.ConfigureAllColumns(c => c.Secure(), c => c.Field.Name != "Id");
        }

        public ITable<Foo> Foos { get; private set; }
        public ITable<Secure> Secure { get; private set; }
    }
}
