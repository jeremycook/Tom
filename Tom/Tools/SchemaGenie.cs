using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tom.Tools
{
    public class SchemaGenie
    {
        public SchemaGenie(TomBase tomBase)
        {
            TomBase = tomBase;
        }

        public TomBase TomBase { get; private set; }

        /// <summary>
        /// Example:
        /// GO
        /// SET ANSI_NULLS ON
        /// SET QUOTED_IDENTIFIER ON
        /// SET ANSI_PADDING ON
        /// GO
        /// CREATE TABLE[dbo].[Foo] (
        ///     [Id]
        ///         [uniqueidentifier]
        ///         NOT NULL CONSTRAINT[DF_Foo_Id] DEFAULT(newid()),
        ///     [Guid]
        ///         [uniqueidentifier]
        ///         NOT NULL CONSTRAINT[DF_Foo_Guid] DEFAULT(newid()),
        ///     [Int]
        ///         [int] NOT NULL CONSTRAINT[DF_Foo_Int] DEFAULT((0)),
        ///     [Decimal]
        ///         [decimal](18, 0) NOT NULL CONSTRAINT[DF_Foo_Decimal] DEFAULT((0)),
        ///     [Float]
        ///         [float] NOT NULL CONSTRAINT[DF_Foo_Float] DEFAULT((0)),
        ///     [DateTime2]
        ///         [datetime2](7) NOT NULL CONSTRAINT[DF_Foo_DateTime2] DEFAULT(getutcdate()),
        ///     [DateTimeOffset]
        ///         [datetimeoffset](7) NOT NULL CONSTRAINT[DF_Foo_DateTimeOffset] DEFAULT(getutcdate()),
        ///     [Nvarchar]
        ///         [nvarchar](50) NOT NULL CONSTRAINT[DF_Foo_Nvarchar] DEFAULT(''),
        ///     [Varbinary]
        ///         [varbinary](50) NULL,
        ///     CONSTRAINT[PK_Foo] PRIMARY KEY CLUSTERED
        ///    (
        ///        [Id] ASC
        ///    ) WITH (
        ///         PAD_INDEX = OFF, 
        ///         STATISTICS_NORECOMPUTE = OFF, 
        ///         IGNORE_DUP_KEY = OFF, 
        ///         ALLOW_ROW_LOCKS = ON, 
        ///         ALLOW_PAGE_LOCKS = ON
        ///     ) ON [PRIMARY]
        /// ) ON[PRIMARY]
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

        public string CreateTable(IRoot root)
        {
            var fieldDeclarations = root.Columns.Select(o => o.Declaration());
            string fieldsText = string.Join(",\n    ", fieldDeclarations);

            string primaryKeyText = string.Join(", ", root.PrimaryKey.Select(o => o.FieldName));

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
    }
}
