using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
        /// Create <see cref="Roots"/> with default configurations.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<ITable> CreateRoots()
        {
            var roots = new List<ITable>();

            // Step one: create roots.
            var rootProps = GetType().GetProperties()
                .Where(p => typeof(ITable).IsAssignableFrom(p.PropertyType));
            foreach (var prop in rootProps)
            {
                ITable root;
                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(ITable<>))
                {
                    Type concretePropertyType = typeof(Table<>).MakeGenericType(prop.PropertyType.GetGenericArguments()[0]);
                    root = Activator.CreateInstance(concretePropertyType, args: new object[] { this }) as ITable;
                }
                else
                {
                    root = Activator.CreateInstance(prop.PropertyType, args: new object[] { this }) as ITable;
                }
                prop.SetValue(this, root);
                roots.Add(root);
            }

            // Step two: configure roots and columns based on other roots and columns.
            var modelTypes = roots.ToDictionary(o => o.ModelType);
            foreach (var root in roots)
            {
                foreach (var column in root.Columns)
                {
                    Type modelType = column.Field.Type;
                    if (column.Field.Type.IsGenericType)
                    {
                        if (column.Field.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                        {
                            modelType = column.Field.Type.GetGenericArguments()[0];
                        }
                        else if (column.Field.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            modelType = column.Field.Type.GetGenericArguments()[0];
                        }
                    }
                    else
                    {
                        Type ienumOf = column.Field.Type.GetInterface("System.Collections.Generic.IEnumerable`1");
                        if (ienumOf != null)
                        {
                            modelType = ienumOf.GetGenericArguments()[0];
                        }
                    }

                    // References to a root model type are not mapped by
                    // default. Instead it is expected that there will be a
                    // foreign key of some sort.
                    //
                    // Example where ReferenceToSomeRootId will be mapped but
                    // the ReferenceToSomeRootId property will not:
                    // public class SomeRoot { Guid Id }
                    // public class OtherRoot { Guid Id, Guid ReferenceToSomeRootId, SomeRoot ReferenceToSomeRoot }
                    if (modelTypes.ContainsKey(modelType))
                    {
                        column.Field.IsMapped = false;
                    }
                }
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
        public IEnumerable<ITable> Roots { get; private set; }
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
