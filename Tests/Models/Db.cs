using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tom;

namespace Tests.Models
{
    public class Db : TomBase
    {
        public Db(string connectionStringOrName) : base(connectionStringOrName) { }

        public Root<Foo> Foos { get; private set; }
        public Root<Data> Data { get; private set; }
    }
}
