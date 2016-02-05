namespace Tom.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SchemaGenie
    {
        public SchemaGenie(TomBase tomBase)
        {
            TomBase = tomBase;
        }

        public TomBase TomBase { get; private set; }

        /// <summary>
        /// Generate the schema of <see cref="TomBase"/>.
        /// </summary>
        /// <returns></returns>
        public string CreateSchema()
        {
            var sb = new StringBuilder();

            sb.AppendFormat(
@"-- Generated {0} by {1}

SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON
GO
", DateTimeOffset.Now, Environment.UserName);

            foreach (var root in TomBase.Roots)
            {
                sb.AppendLine(CreateTable(root));
            }

            return sb.ToString();
        }

        public string CreateTable(ITable root)
        {
            var fieldDeclarations = root.Columns.Select(o => CreateField(root, o));
            string fieldsText = string.Join(",\n    ", fieldDeclarations);

            string primaryKeyText = string.Join(", ", root.PrimaryKey.Select(o => o.Field.Name));

            string sql = string.Format(@"
CREATE TABLE [dbo].[{0}] (
    {1},
    CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED (
        {2}
    )
    WITH (
        PAD_INDEX = OFF, 
        STATISTICS_NORECOMPUTE = OFF, 
        IGNORE_DUP_KEY = OFF, 
        ALLOW_ROW_LOCKS = ON, 
        ALLOW_PAGE_LOCKS = ON
    ) ON [PRIMARY]
) ON [PRIMARY]
GO", root.TableName, fieldsText, primaryKeyText);
            return sql;
        }

        public string CreateField(ITable table, Column column)
        {
            string declaration = string.Format("[{0}] [{1}]{2} {3} {4}",
                column.Field.Name,
                column.Field.SqlDbType.ToString().ToLower(),
                column.FieldArguments,
                column.Field.IsNullable ? "NULL" : "NOT NULL",
                column.FieldDefault == null ? null : ("CONSTRAINT [DF_" + table.TableName + "_" + column.Field.Name + "]  DEFAULT " + column.FieldDefault));

            return declaration;
        }
    }
}
