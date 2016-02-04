using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Tests.Models;
using Tom;

namespace Tests
{
    [TestClass]
    public class TomCommandTests
    {
        private readonly SqlConnection con;

        public TomCommandTests()
        {
            con = new SqlConnection(ConfigurationManager.ConnectionStrings["Db"].ConnectionString);
            con.Open();

            con.ExecuteAsync("truncate table dbo.Foo").Wait();
            con.ExecuteAsync("truncate table dbo.Secure").Wait();
        }

        public void Dispose()
        {
            con.Dispose();
        }

        [TestMethod]
        public async Task ExecuteInsertFoos()
        {
            var cmd = new Command<Foo>();

            await cmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Foo ({0}) values ({1})",
                    cmd.ToFieldNamesText(), cmd.ToParameterNamesText()
                ),
                Enumerable.Range(0, 1000).Select(i => new Foo
                {
                    Id = Guid.NewGuid(),
                    Int = i,
                    DateTime2 = DateTime.UtcNow,
                    DateTimeOffset = DateTimeOffset.Now,
                })
            );
        }

        [TestMethod]
        public async Task ExecuteInsertSecures()
        {
            var cmd = new Command<Secure>()
                .ConfigureAllFields(f => f.Secure(), f => f.Name != "Id");

            await cmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Secure ({0}) values ({1})",
                    cmd.ToFieldNamesText(), cmd.ToParameterNamesText()
                ),
                Enumerable.Range(0, 500).Select(i => new Secure
                {
                    Id = Guid.NewGuid(),
                    Int = i,
                    DateTime2 = DateTime.UtcNow,
                    DateTimeOffset = DateTimeOffset.Now,
                    Nvarchar = "",
                })
            );
        }
    }
}
