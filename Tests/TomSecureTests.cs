﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Tests.Models;
using Tom;

namespace Tests
{
    [TestClass]
    public class TomSecureTests : IDisposable
    {
        private readonly Db db;

        public TomSecureTests()
        {
            db = new Db("Db");

            using (var cx = new SqlConnection(db.ConnectionString))
            {
                cx.Open();
                cx.ExecuteAsync("truncate table dbo.Secure").Wait();
            }
        }

        public void Dispose()
        {
            db.Dispose();
        }


        [TestMethod]
        public async Task AddSecure()
        {
            var secure = new Secure
            {
                Id = Guid.NewGuid(),
                Nvarchar = "Created",
            };
            await db.Secure.AddAsync(secure);
            db.Commit();
        }

        [TestMethod]
        public async Task AddRangeSecure()
        {
            await db.Secure.AddRangeAsync(Enumerable.Range(0, 500).Select(i => new Secure
            {
                Id = Guid.NewGuid(),
                Nvarchar = "Created",
            }));
            db.Commit();
        }


        [TestMethod]
        public async Task UpdateSecure()
        {
            var secure = new Secure
            {
                Id = Guid.NewGuid(),
                Nvarchar = "Created",
            };
            await db.Secure.AddAsync(secure);
            db.Commit();

            secure.Nvarchar = "Updated";
            await db.Secure.UpdateAsync(secure);
            db.Commit();
        }

        [TestMethod]
        public async Task UpdateRangeSecure()
        {
            var secures = Enumerable.Range(0, 500).Select(i => new Secure
            {
                Id = Guid.NewGuid(),
                Nvarchar = "Created",
            }).ToList();
            await db.Secure.AddRangeAsync(secures);
            db.Commit();

            foreach (var item in secures)
            {
                item.Nvarchar = "Updated";
            }
            await db.Secure.UpdateRangeAsync(secures);
            db.Commit();
        }


        [TestMethod]
        public async Task RemoveSecure()
        {
            var secure = new Secure
            {
                Id = Guid.NewGuid(),
                Nvarchar = "Created",
            };
            await db.Secure.AddAsync(secure);
            db.Commit();

            await db.Secure.RemoveAsync(secure);
            db.Commit();
        }

        [TestMethod]
        public async Task RemoveRangeSecure()
        {
            var secures = Enumerable.Range(0, 500).Select(i => new Secure
            {
                Id = Guid.NewGuid(),
                Nvarchar = "Created",
            }).ToList();
            await db.Secure.AddRangeAsync(secures);
            db.Commit();

            await db.Secure.RemoveRangeAsync(secures);
            db.Commit();
        }
    }
}
