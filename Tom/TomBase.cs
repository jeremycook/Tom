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
        public Work Work { get; private set; }

        /// <summary>
        /// Returns the open connection, or opens a connection and begins a
        /// <see cref="Tom.Work"/> if a connection is not open. Call <see cref="Commit"/>
        /// after performing create, update, delete operations to save changes.
        /// </summary>
        /// <returns></returns>
        public async Task<Work> WorkAsync()
        {
            if (Work == null)
            {
                Work = await Work.CreateAsync(ConnectionString);
            }

            return Work;
        }

        /// <summary>
        /// Commits the current transaction on the open connection, and then
        /// closes and disposes the transaction and connection.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// <see cref="WorkAsync"/> must be called prior to calling this method.
        /// </exception>
        public virtual void Commit()
        {
            if (Work == null)
            {
                throw new InvalidOperationException("No open connection. `WorkAsync` must be called prior to calling this method.");
            }

            // Commit...
            Work.Transaction.Commit();
            // and cleanup.
            Work.Dispose();
            Work = null;
        }

        /// <summary>
        /// Dispose and nulls <see cref="Work"/> and connection if the
        /// connection is not null.
        /// </summary>
        public void Dispose()
        {
            if (Work != null)
            {
                Work.Dispose();
                Work = null;
            }
        }
    }
}
