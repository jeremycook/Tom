using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tom
{
    public abstract class TomBase
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
                var root = Activator.CreateInstance(prop.PropertyType) as IRoot;
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
    }
}
