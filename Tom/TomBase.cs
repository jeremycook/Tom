using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tom
{
    public abstract class TomBase : IDisposable
    {
        private SqlConnection _Connection;

        public TomBase(string connectionStringOrName)
        {
            ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringOrName] != null ?
                ConfigurationManager.ConnectionStrings[connectionStringOrName].ConnectionString :
                connectionStringOrName;

            Roots = CreateRoots();
            Configure();
        }

        /// <summary>
        /// Create <see cref="Roots"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<IRoot> CreateRoots()
        {
            var roots = new List<IRoot>();
            var rootProps = GetType().GetProperties()
                .Where(p => typeof(IRoot).IsAssignableFrom(p.PropertyType));
            foreach (var prop in rootProps)
            {
                var root = Activator.CreateInstance(prop.PropertyType, args: new object[] { this }) as IRoot;
                prop.SetValue(this, root);
                roots.Add(root);
            }

            return roots;
        }

        /// <summary>
        /// Override to configure <see cref="Roots"/>.
        /// </summary>
        protected virtual void Configure()
        {
            // Intentionally blank.
        }

        public string ConnectionString { get; private set; }
        public IEnumerable<IRoot> Roots { get; private set; }
        public SqlTransaction Transaction { get; private set; }

        /// <summary>
        /// Returns the open connection, or opens a connection and begins a
        /// <see cref="Transaction"/> if a connection is not open. Call <see cref="Commit"/>
        /// after performing create, update, delete operations to save changes.
        /// </summary>
        /// <returns></returns>
        public async Task<SqlConnection> UseConnectionAsync()
        {
            if (_Connection == null)
            {
                _Connection = new SqlConnection(ConnectionString);
                await _Connection.OpenAsync();
                Transaction = _Connection.BeginTransaction();
            }

            return _Connection;
        }

        /// <summary>
        /// Commits the current transaction on the open connection, and then
        /// closes and disposes the transaction and connection.
        /// <see cref="UseConnectionAsync"/> must be called prior to calling
        /// this method.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="UseConnectionAsync"/> must be called prior to calling this method.
        /// </exception>
        public virtual void Commit()
        {
            if (_Connection == null)
            {
                throw new InvalidOperationException("No open connection. `UseConnectionAsync` must be called prior to calling this method.");
            }

            try
            {
                Transaction.Commit();
            }
            finally
            {
                Transaction.Dispose();
                _Connection.Dispose();
                Transaction = null;
                _Connection = null;
            }
        }

        /// <summary>
        /// Dispose and nulls <see cref="Transaction"/> and connection if the
        /// connection is not null.
        /// </summary>
        public void Dispose()
        {
            if (_Connection != null)
            {
                Transaction.Dispose();
                _Connection.Dispose();
                Transaction = null;
                _Connection = null;
            }
        }
    }
}
