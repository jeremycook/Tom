﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public class CommandSecureTests
    {
        private readonly Command<Bar> barCmd;
        private readonly Command<Secure> cmd;
        private readonly SqlConnection con;

        public CommandSecureTests()
        {
            barCmd = new Command<Bar>();
            cmd = new Command<Secure>()
                .ConfigureAllFields(c => c.Secure(), c => c.Name != "Id")
                .ConfigureField(o => o.Bar, c => c.IsMapped = false)
                .ConfigureField(o => o.Bars, c => c.IsMapped = false);

            con = new SqlConnection(ConfigurationManager.ConnectionStrings["Db"].ConnectionString);
            con.Open();

            con.ExecuteAsync("truncate table dbo.Secure").Wait();
            con.ExecuteAsync("truncate table dbo.Bar").Wait();
        }

        public void Dispose()
        {
            con.Dispose();
        }

        [TestMethod]
        public async Task InsertEmptySecures()
        {

            var originals = Enumerable.Range(0, 500).Select(i => new Secure
            {
                Id = Guid.NewGuid(),
            }).OrderBy(o => o.Id).ToList();

            await cmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Secure ({0}) values ({1})",
                    cmd.ToFieldNamesText(), cmd.ToParameterNamesText()
                ),
                originals
            );

            var results = await cmd.ListAsync(con, "select * from dbo.Secure");
            Assert.AreEqual(originals.Count, results.Count);

            var actuals = results.OrderBy(o => o.Id).ToList();
            for (int i = 0; i < originals.Count; i++)
            {
                Assert.AreEqual(originals[i].Id, actuals[i].Id);
                Assert.AreEqual(originals[i].Bit, actuals[i].Bit);
                Assert.AreEqual(originals[i].BitNull, actuals[i].BitNull);
                Assert.AreEqual(originals[i].DateTime2, actuals[i].DateTime2);
                Assert.AreEqual(originals[i].DateTime2Null, actuals[i].DateTime2Null);
                Assert.AreEqual(originals[i].DateTimeOffset, actuals[i].DateTimeOffset);
                Assert.AreEqual(originals[i].DateTimeOffsetNull, actuals[i].DateTimeOffsetNull);
                Assert.AreEqual(originals[i].Decimal, actuals[i].Decimal);
                Assert.AreEqual(originals[i].DecimalNull, actuals[i].DecimalNull);
                Assert.AreEqual(originals[i].Float, actuals[i].Float);
                Assert.AreEqual(originals[i].FloatNull, actuals[i].FloatNull);
                Assert.AreEqual(originals[i].Guid, actuals[i].Guid);
                Assert.AreEqual(originals[i].GuidNull, actuals[i].GuidNull);
                Assert.AreEqual(originals[i].Int, actuals[i].Int);
                Assert.AreEqual(originals[i].IntNull, actuals[i].IntNull);
                Assert.AreEqual(actuals[i].Nvarchar, actuals[i].Nvarchar);
                Assert.AreEqual(actuals[i].Varbinary, actuals[i].Varbinary);
                Assert.AreEqual(actuals[i].Fi, actuals[i].Fi);
                Assert.AreEqual(actuals[i].Fees, actuals[i].Fees);
                Assert.AreEqual(actuals[i].BarId, actuals[i].BarId);
                Assert.AreEqual(actuals[i].Bar, actuals[i].Bar);
                Assert.AreEqual(actuals[i].BarIds, actuals[i].BarIds);
                Assert.AreEqual(actuals[i].Bars, actuals[i].Bars);
            }
        }

        [TestMethod]
        public async Task InsertSecures()
        {
            var bar = new Bar { Id = Guid.NewGuid(), Name = "The one and only bar" };
            await barCmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Bar ({0}) values ({1})",
                    barCmd.ToFieldNamesText(), barCmd.ToParameterNamesText()
                ),
                new[] { bar }
            );

            var originals = Enumerable.Range(0, 500).Select(i => new Secure
            {
                Id = Guid.NewGuid(),
                Int = i,
                IntNull = i,
                DateTime2 = DateTime.UtcNow,
                DateTime2Null = DateTime.UtcNow,
                DateTimeOffset = DateTimeOffset.Now,
                DateTimeOffsetNull = DateTimeOffset.Now,
                Bit = true,
                BitNull = true,
                Float = 1,
                FloatNull = 1,
                Decimal = 1,
                DecimalNull = 1,
                Guid = Guid.NewGuid(),
                GuidNull = Guid.NewGuid(),
                Nvarchar = "Created",
                Varbinary = new byte[] { 0, 1, 2, 3 },
                Fi = new SecureFi { Name = "I'm a fi you're a secure.", Number = 1 },
                Fees = new[]
                {
                    new SecureFee { Name = "I'm a fee you're a secure.", Number = 2 },
                    new SecureFee { Name = "I'm another fee you're a secure.", Number = 3 }
                },
                BarId = bar.Id,
                BarIds = new[]
                {
                    bar.Id,
                },
            }).OrderBy(o => o.Id).ToList();

            await cmd.ExecuteAsync(con, string.Format(
                    "insert into dbo.Secure ({0}) values ({1})",
                    cmd.ToFieldNamesText(), cmd.ToParameterNamesText()
                ),
                originals
            );

            var results = await cmd.ListAsync(con, "select * from dbo.Secure");
            Assert.AreEqual(originals.Count, results.Count);

            var actuals = results.OrderBy(o => o.Id).ToList();
            for (int i = 0; i < originals.Count; i++)
            {
                Assert.AreEqual(originals[i].Id, actuals[i].Id);
                Assert.AreEqual(originals[i].Bit, actuals[i].Bit);
                Assert.AreEqual(originals[i].BitNull, actuals[i].BitNull);
                Assert.AreEqual(originals[i].DateTime2, actuals[i].DateTime2);
                Assert.AreEqual(originals[i].DateTime2Null, actuals[i].DateTime2Null);
                Assert.AreEqual(originals[i].DateTimeOffset, actuals[i].DateTimeOffset);
                Assert.AreEqual(originals[i].DateTimeOffsetNull, actuals[i].DateTimeOffsetNull);
                Assert.AreEqual(originals[i].Decimal, actuals[i].Decimal);
                Assert.AreEqual(originals[i].DecimalNull, actuals[i].DecimalNull);
                Assert.AreEqual(originals[i].Float, actuals[i].Float);
                Assert.AreEqual(originals[i].FloatNull, actuals[i].FloatNull);
                Assert.AreEqual(originals[i].Guid, actuals[i].Guid);
                Assert.AreEqual(originals[i].GuidNull, actuals[i].GuidNull);
                Assert.AreEqual(originals[i].Int, actuals[i].Int);
                Assert.AreEqual(originals[i].IntNull, actuals[i].IntNull);
                Assert.AreEqual(originals[i].Nvarchar, actuals[i].Nvarchar);

                Assert.IsNotNull(actuals[i].Varbinary);
                Assert.AreEqual(originals[i].Varbinary.Length, actuals[i].Varbinary.Length);
                Assert.IsTrue(originals[i].Varbinary.All(o => actuals[i].Varbinary.Contains(o)));

                Assert.IsNotNull(actuals[i].Fi);
                Assert.AreEqual(originals[i].Fi.Name, actuals[i].Fi.Name);
                Assert.AreEqual(originals[i].Fi.Number, actuals[i].Fi.Number);

                Assert.IsNotNull(actuals[i].Fees);
                Assert.AreEqual(originals[i].Fees.First().Name, actuals[i].Fees.First().Name);
                Assert.AreEqual(originals[i].Fees.First().Number, actuals[i].Fees.First().Number);
                Assert.AreEqual(originals[i].Fees.Last().Name, actuals[i].Fees.Last().Name);
                Assert.AreEqual(originals[i].Fees.Last().Number, actuals[i].Fees.Last().Number);

                Assert.AreEqual(actuals[i].BarId, actuals[i].BarId);
                Assert.AreEqual(actuals[i].Bar, actuals[i].Bar);

                Assert.AreEqual(actuals[i].BarIds, actuals[i].BarIds);
                Assert.AreEqual(actuals[i].Bars, actuals[i].Bars);
            }
        }

        [TestMethod]
        public async Task ListAllSecures()
        {
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
                })
            );

            var results = await cmd.ListAsync(con, "select * from dbo.Secure");

            Assert.AreEqual(500, results.Count);
        }
    }
}
