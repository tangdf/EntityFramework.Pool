// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure.Annotations;
    using System.Data.Entity.Migrations.Builders;
    using System.Data.Entity.Migrations.Infrastructure;
    using System.Data.Entity.Migrations.Model;
    using System.Data.Entity.Resources;
    using System.Linq;
    using Moq;
    using Xunit;
    using System.IO;
    using System.Reflection;

    public class DbMigrationTests
    {
        [Fact]
        public void AddPrimaryKey_single_column_creates_add_primary_key_operation()
        {
            var migration = new TestMigration();

            migration.AddPrimaryKey("t", "c", "pk");

            var addPrimaryKeyOperation = migration.Operations.Cast<AddPrimaryKeyOperation>().Single();

            Assert.Equal("t", addPrimaryKeyOperation.Table);
            Assert.Equal("c", addPrimaryKeyOperation.Columns.Single());
            Assert.Equal("pk", addPrimaryKeyOperation.Name);
            Assert.True(addPrimaryKeyOperation.IsClustered);
        }

        [Fact]
        public void AddPrimaryKey_multiple_columns_creates_add_primary_key_operation()
        {
            var migration = new TestMigration();

            migration.AddPrimaryKey("t", new[] { "c1", "c2" }, "pk");

            var addPrimaryKeyOperation = migration.Operations.Cast<AddPrimaryKeyOperation>().Single();

            Assert.Equal("t", addPrimaryKeyOperation.Table);
            Assert.Equal("c1", addPrimaryKeyOperation.Columns.First());
            Assert.Equal("c2", addPrimaryKeyOperation.Columns.Last());
            Assert.Equal("pk", addPrimaryKeyOperation.Name);
            Assert.True(addPrimaryKeyOperation.IsClustered);
        }

        [Fact]
        public void AddPrimaryKey_can_set_clustered_parameter()
        {
            var migration = new TestMigration();

            migration.AddPrimaryKey("t", "c", "pk", clustered: false);

            var addPrimaryKeyOperation = migration.Operations.Cast<AddPrimaryKeyOperation>().Single();

            Assert.False(addPrimaryKeyOperation.IsClustered);
        }

        [Fact]
        public void DropPrimaryKey_by_name_creates_drop_primary_key_operation()
        {
            var migration = new TestMigration();

            migration.DropPrimaryKey("t", "pk");

            var dropForeignKeyOperation = migration.Operations.Cast<DropPrimaryKeyOperation>().Single();

            Assert.Equal("t", dropForeignKeyOperation.Table);
            Assert.Equal("pk", dropForeignKeyOperation.Name);
        }

        [Fact]
        public void DropPrimaryKey_by_table_name_creates_drop_primary_key_operation()
        {
            var migration = new TestMigration();

            migration.DropPrimaryKey("t");

            var dropForeignKeyOperation = migration.Operations.Cast<DropPrimaryKeyOperation>().Single();

            Assert.Equal("t", dropForeignKeyOperation.Table);
        }

        [Fact]
        public void AddForeignKey_creates_add_foreign_key_operation()
        {
            var migration = new TestMigration();

            migration.AddForeignKey("d", "dc", "p", "pc", true, "fk");

            var addForeignKeyOperation = migration.Operations.Cast<AddForeignKeyOperation>().Single();

            Assert.Equal("d", addForeignKeyOperation.DependentTable);
            Assert.Equal("dc", addForeignKeyOperation.DependentColumns.Single());
            Assert.Equal("p", addForeignKeyOperation.PrincipalTable);
            Assert.Equal("pc", addForeignKeyOperation.PrincipalColumns.Single());
            Assert.Equal("fk", addForeignKeyOperation.Name);
            Assert.True(addForeignKeyOperation.CascadeDelete);
        }

        [Fact]
        public void AddForeignKey_creates_add_foreign_key_operation_when_composite_key()
        {
            var migration = new TestMigration();

            migration.AddForeignKey("d", new[] { "dc1", "dc2" }, "p", new[] { "pc1", "pc2" }, true, "fk");

            var addForeignKeyOperation = migration.Operations.Cast<AddForeignKeyOperation>().Single();

            Assert.Equal("d", addForeignKeyOperation.DependentTable);
            Assert.Equal("dc1", addForeignKeyOperation.DependentColumns.First());
            Assert.Equal("dc2", addForeignKeyOperation.DependentColumns.Last());
            Assert.Equal("p", addForeignKeyOperation.PrincipalTable);
            Assert.Equal("pc1", addForeignKeyOperation.PrincipalColumns.First());
            Assert.Equal("pc2", addForeignKeyOperation.PrincipalColumns.Last());
            Assert.Equal("fk", addForeignKeyOperation.Name);
            Assert.True(addForeignKeyOperation.CascadeDelete);
        }

        [Fact]
        public void DropForeignKey_creates_drop_foreign_key_operation()
        {
            var migration = new TestMigration();

            migration.DropForeignKey("d", "dc", "p");

            var dropForeignKeyOperation = migration.Operations.Cast<DropForeignKeyOperation>().Single();

            Assert.Equal("d", dropForeignKeyOperation.DependentTable);
            Assert.Equal("dc", dropForeignKeyOperation.DependentColumns.Single());
            Assert.Equal("p", dropForeignKeyOperation.PrincipalTable);
            Assert.Equal(dropForeignKeyOperation.DefaultName, dropForeignKeyOperation.Name);
        }

        [Fact]
        public void DropForeignKey_creates_drop_foreign_key_operation_when_composite_key()
        {
            var migration = new TestMigration();

            migration.DropForeignKey("d", new[] { "dc1", "dc2" }, "p", new[] { "pc1", "pc2" });

            var dropForeignKeyOperation = migration.Operations.Cast<DropForeignKeyOperation>().Single();

            Assert.Equal("d", dropForeignKeyOperation.DependentTable);
            Assert.Equal("dc1", dropForeignKeyOperation.DependentColumns.First());
            Assert.Equal("dc2", dropForeignKeyOperation.DependentColumns.Last());
            Assert.Equal("p", dropForeignKeyOperation.PrincipalTable);
            Assert.Equal(dropForeignKeyOperation.DefaultName, dropForeignKeyOperation.Name);
        }

        [Fact]
        public void DropForeignKey_creates_drop_foreign_key_operation_with_name()
        {
            var migration = new TestMigration();

            migration.DropForeignKey("Foo", "fk");

            var dropForeignKeyOperation = migration.Operations.Cast<DropForeignKeyOperation>().Single();

            Assert.Equal("Foo", dropForeignKeyOperation.DependentTable);
            Assert.Equal("fk", dropForeignKeyOperation.Name);
        }

        [Fact]
        public void DropColumn_creates_drop_column_operation()
        {
            var migration = new TestMigration();

            migration.DropColumn("Customers", "OldColumn");

            var dropColumnOperation = migration.Operations.Cast<DropColumnOperation>().Single();

            Assert.Equal("Customers", dropColumnOperation.Table);
            Assert.Equal("OldColumn", dropColumnOperation.Name);
        }

        [Fact]
        public void DropColumn_can_build_operation_with_name_and_annotations()
        {
            var migration = new TestMigration();

            migration.DropColumn(
                "Customers",
                "Foo",
                new Dictionary<string, object>
                {
                    { "Blue", "Lips" }
                });

            var operation = migration.Operations.Cast<DropColumnOperation>().Single();

            Assert.Equal("Customers", operation.Table);
            Assert.Equal("Foo", operation.Name);

            Assert.Equal(1, operation.RemovedAnnotations.Count);
            Assert.Equal("Lips", operation.RemovedAnnotations["Blue"]);
        }

        [Fact]
        public void DropColumn_can_build_operation_with_name_and_null_annotations()
        {
            var migration = new TestMigration();

            migration.DropColumn(
                "Customers",
                "Foo",
                null);

            var operation = migration.Operations.Cast<DropColumnOperation>().Single();

            Assert.Equal("Customers", operation.Table);
            Assert.Equal("Foo", operation.Name);

            Assert.Equal(0, operation.RemovedAnnotations.Count);
        }

        [Fact]
        public void DropColumn_with_annotations_checks_arguments()
        {
            var migration = new TestMigration();

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("table"),
                Assert.Throws<ArgumentException>(
                    () => migration.DropColumn(null, "C", null)).Message);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(
                    () => migration.DropColumn("T", null, null)).Message);
        }

        [Fact]
        public void Can_call_create_table_with_anonymous_arguments()
        {
            var migration = new TestMigration();

            migration.CreateTable(
                "Foo", cs => new
                                 {
                                     Id = cs.Int()
                                 }, new
                                        {
                                            Foo = 123
                                        });

            var createTableOperation = migration.Operations.Cast<CreateTableOperation>().Single();

            Assert.Equal(123, createTableOperation.AnonymousArguments["Foo"]);
        }

        [Fact]
        public void AddColumn_creates_add_column_operation_with_column_model()
        {
            var migration = new TestMigration();

            migration.AddColumn("Customers", "NewColumn", c => c.Byte(nullable: false));

            var addColumnOperation = migration.Operations.Cast<AddColumnOperation>().Single();

            Assert.Equal("Customers", addColumnOperation.Table);
            Assert.Equal("NewColumn", addColumnOperation.Column.Name);
            Assert.Equal(PrimitiveTypeKind.Byte, addColumnOperation.Column.Type);
            Assert.False(addColumnOperation.Column.IsNullable.Value);
        }

        [Fact]
        public void CreateStoredProcedure_can_build_procedure_with_parameters()
        {
            var migration = new TestMigration();

            migration.CreateStoredProcedure(
                "Customers_Insert",
                p => new
                         {
                             Id = p.Int(),
                             Name = p.String()
                         },
                "insert into customers...");

            var createProcedureOperation 
                = migration.Operations.Cast<CreateProcedureOperation>().Single();

            Assert.Equal("Customers_Insert", createProcedureOperation.Name);
            Assert.Equal("insert into customers...", createProcedureOperation.BodySql);
            Assert.Equal(2, createProcedureOperation.Parameters.Count());

            var parameterModel = createProcedureOperation.Parameters.First();

            Assert.Equal("Id", parameterModel.Name);
            Assert.Equal(PrimitiveTypeKind.Int32, parameterModel.Type);

            parameterModel = createProcedureOperation.Parameters.Last();

            Assert.Equal("Name", parameterModel.Name);
            Assert.Equal(PrimitiveTypeKind.String, parameterModel.Type);
        }

        [Fact]
        public void CreateStoredProcedure_can_build_procedure_without_parameters()
        {
            var migration = new TestMigration();

            migration.CreateStoredProcedure(
                "Customers_Insert",
                "insert into customers...");

            var createProcedureOperation
                = migration.Operations.Cast<CreateProcedureOperation>().Single();

            Assert.Equal("Customers_Insert", createProcedureOperation.Name);
            Assert.Equal("insert into customers...", createProcedureOperation.BodySql);
            Assert.Equal(0, createProcedureOperation.Parameters.Count());
        }

        [Fact]
        public void CreateStoredProcedure_can_build_procedure_without_body()
        {
            var migration = new TestMigration();

            migration.CreateStoredProcedure(
                "Customers_Insert",
                "");

            var createProcedureOperation
                = migration.Operations.Cast<CreateProcedureOperation>().Single();

            Assert.Equal("Customers_Insert", createProcedureOperation.Name);
            Assert.Equal("", createProcedureOperation.BodySql);
        }

        [Fact]
        public void AlterStoredProcedure_can_build_procedure_with_parameters()
        {
            var migration = new TestMigration();

            migration.AlterStoredProcedure(
                "Customers_Insert",
                p => new
                {
                    Id = p.Int(),
                    Name = p.String()
                },
                "insert into customers...");

            var alterProcedureOperation
                = migration.Operations.Cast<AlterProcedureOperation>().Single();

            Assert.Equal("Customers_Insert", alterProcedureOperation.Name);
            Assert.Equal("insert into customers...", alterProcedureOperation.BodySql);
            Assert.Equal(2, alterProcedureOperation.Parameters.Count());

            var parameterModel = alterProcedureOperation.Parameters.First();

            Assert.Equal("Id", parameterModel.Name);
            Assert.Equal(PrimitiveTypeKind.Int32, parameterModel.Type);

            parameterModel = alterProcedureOperation.Parameters.Last();

            Assert.Equal("Name", parameterModel.Name);
            Assert.Equal(PrimitiveTypeKind.String, parameterModel.Type);
        }

        [Fact]
        public void AlterStoredProcedure_can_build_procedure_without_parameters()
        {
            var migration = new TestMigration();

            migration.AlterStoredProcedure(
                "Customers_Insert",
                "insert into customers...");

            var alterProcedureOperation
                = migration.Operations.Cast<AlterProcedureOperation>().Single();

            Assert.Equal("Customers_Insert", alterProcedureOperation.Name);
            Assert.Equal("insert into customers...", alterProcedureOperation.BodySql);
            Assert.Equal(0, alterProcedureOperation.Parameters.Count());
        }

        [Fact]
        public void AlterStoredProcedure_can_build_procedure_without_body()
        {
            var migration = new TestMigration();

            migration.CreateStoredProcedure(
                "Customers_Insert",
                null);

            var alterProcedureOperation
                = migration.Operations.Cast<CreateProcedureOperation>().Single();

            Assert.Equal("Customers_Insert", alterProcedureOperation.Name);
            Assert.Null(alterProcedureOperation.BodySql);
        }

        [Fact]
        public void RenameStoredProcedure_should_add_rename_procedure_operation()
        {
            var migration = new TestMigration();

            migration.RenameStoredProcedure("old", "new");

            var renameProcedureOperation = migration.Operations.Cast<RenameProcedureOperation>().Single();

            Assert.NotNull(renameProcedureOperation);
            Assert.Equal("old", renameProcedureOperation.Name);
            Assert.Equal("new", renameProcedureOperation.NewName);
        }

        [Fact]
        public void DropStoredProcedure_should_add_drop_procedure_operation()
        {
            var migration = new TestMigration();

            migration.DropStoredProcedure("Customers_Insert");

            var dropProcedureOperation = migration.Operations.Cast<DropProcedureOperation>().Single();

            Assert.NotNull(dropProcedureOperation);
            Assert.Equal("Customers_Insert", dropProcedureOperation.Name);
        }

        [Fact]
        public void CreateTable_can_build_table_with_columns()
        {
            var migration = new TestMigration();

            migration.CreateTable(
                "Customers",
                cs => new
                          {
                              Id = cs.Int(),
                              Name = cs.String()
                          });

            var createTableOperation = migration.Operations.Cast<CreateTableOperation>().Single();

            Assert.Equal("Customers", createTableOperation.Name);
            Assert.Equal(2, createTableOperation.Columns.Count());

            var column = createTableOperation.Columns.First();

            Assert.Equal("Id", column.Name);
            Assert.Equal(PrimitiveTypeKind.Int32, column.Type);

            column = createTableOperation.Columns.Last();

            Assert.Equal("Name", column.Name);
            Assert.Equal(PrimitiveTypeKind.String, column.Type);
        }

        [Fact]
        public void CreateTable_can_build_table_with_custom_column_name()
        {
            var migration = new TestMigration();

            migration.CreateTable(
                "Customers",
                cs => new
                          {
                              Id = cs.Int(name: "Customer Id")
                          });

            var createTableOperation = migration.Operations.Cast<CreateTableOperation>().Single();

            var column = createTableOperation.Columns.Single();

            Assert.Equal("Customer Id", column.Name);
            Assert.Equal(PrimitiveTypeKind.Int32, column.Type);
        }

        [Fact]
        public void CreateTable_can_build_table_pk_with_custom_column_name()
        {
            var migration = new TestMigration();

            migration.CreateTable(
                "Customers",
                cs => new
                {
                    Id = cs.Int(name: "Customer Id")
                })
                .PrimaryKey(t => t.Id);

            var createTableOperation 
                = migration.Operations
                    .Cast<CreateTableOperation>()
                    .Single();

            Assert.Equal("Customer Id", createTableOperation.PrimaryKey.Columns.Single());
        }

        [Fact]
        public void CreateTable_can_build_table_with_index()
        {
            var migration = new TestMigration();

            migration.CreateTable(
                "Customers",
                cs => new
                          {
                              Id = cs.Int(),
                              Name = cs.String()
                          })
                     .Index(
                         t => new
                                  {
                                      t.Id,
                                      t.Name
                                  }, unique: true, clustered: true);

            var createIndexOperation = migration.Operations.OfType<CreateIndexOperation>().Single();

            Assert.NotNull(createIndexOperation.Table);
            Assert.Equal(2, createIndexOperation.Columns.Count());
            Assert.True(createIndexOperation.IsUnique);
            Assert.True(createIndexOperation.IsClustered);
        }

        [Fact]
        public void CreateTable_can_build_operation_with_name_columns_and_annotations()
        {
            var migration = new TestMigration();

            migration.CreateTable(
                "Customers",
                cs => new
                {
                    Id = cs.Int()
                },
                new Dictionary<string, object>
                {
                    { "Blue", "Lips" }
                });

            var operation = migration.Operations.Cast<CreateTableOperation>().Single();

            Assert.Equal("Customers", operation.Name);

            Assert.Equal("Id", operation.Columns.Single().Name);

            Assert.Equal(1, operation.Annotations.Count);
            Assert.Equal("Lips", operation.Annotations["Blue"]);
        }

        [Fact]
        public void CreateTable_can_build_operation_with_name_columns_and_null_annotations()
        {
            var migration = new TestMigration();

            migration.CreateTable(
                "Customers",
                cs => new
                {
                    Id = cs.Int()
                },
                null);

            var operation = migration.Operations.Cast<CreateTableOperation>().Single();

            Assert.Equal("Customers", operation.Name);

            Assert.Equal("Id", operation.Columns.Single().Name);

            Assert.Equal(0, operation.Annotations.Count);
        }

        [Fact]
        public void CreateTable_with_annotations_checks_arguments()
        {
            var migration = new TestMigration();

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(
                    () => migration.CreateTable(null, ct => new { }, null)).Message);

            Assert.Equal(
                "columnsAction",
                Assert.Throws<ArgumentNullException>(() => migration.CreateTable<object>("Customers", null, null)).ParamName);
        }

        [Fact]
        public void AlterTableAnnotations_can_build_operation_with_name_columns_and_annotations()
        {
            var migration = new TestMigration();

            migration.AlterTableAnnotations(
                "Customers",
                cs => new
                {
                    Id = cs.Int()
                },
                new Dictionary<string, AnnotationValues>
                {
                    { "Everyone's", new AnnotationValues("At", "It") }
                });

            var operation = migration.Operations.Cast<AlterTableOperation>().Single();

            Assert.Equal("Customers", operation.Name);

            Assert.Equal("Id", operation.Columns.Single().Name);

            Assert.Equal(1, operation.Annotations.Count);
            Assert.Equal("At", operation.Annotations["Everyone's"].OldValue);
            Assert.Equal("It", operation.Annotations["Everyone's"].NewValue);
        }


        [Fact]
        public void AlterTableAnnotations_can_build_operation_with_name_columns_and_null_annotations()
        {
            var migration = new TestMigration();

            migration.AlterTableAnnotations(
                "Customers",
                cs => new
                {
                    Id = cs.Int()
                },
                null);

            var operation = migration.Operations.Cast<AlterTableOperation>().Single();

            Assert.Equal("Customers", operation.Name);

            Assert.Equal("Id", operation.Columns.Single().Name);

            Assert.Equal(0, operation.Annotations.Count);
        }

        [Fact]
        public void AlterTableAnnotations_with_annotations_checks_arguments()
        {
            var migration = new TestMigration();

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(
                    () => migration.AlterTableAnnotations(null, ct => new { }, null)).Message);

            Assert.Equal(
                "columnsAction",
                Assert.Throws<ArgumentNullException>(() => migration.AlterTableAnnotations<object>("Customers", null, null)).ParamName);
        }

        [Fact]
        public void DropTable_should_add_drop_table_operation()
        {
            var migration = new TestMigration();

            migration.DropTable("Customers");

            var dropTableOperation = migration.Operations.Cast<DropTableOperation>().Single();

            Assert.NotNull(dropTableOperation);
            Assert.Equal("Customers", dropTableOperation.Name);
        }

        [Fact]
        public void DropTable_can_build_operation_with_name_and_annotations()
        {
            var migration = new TestMigration();

            migration.DropTable(
                "Customers",
                new Dictionary<string, object>
                {
                    { "Blue", "Lips" }
                },
                new Dictionary<string, IDictionary<string, object>>
                {
                    { "Everyone's", new Dictionary<string, object> { { "At", "It" } } }
                });

            var operation = migration.Operations.Cast<DropTableOperation>().Single();

            Assert.Equal("Customers", operation.Name);

            Assert.Equal(1, operation.RemovedAnnotations.Count);
            Assert.Equal("Lips", operation.RemovedAnnotations["Blue"]);

            Assert.Equal(1, operation.RemovedColumnAnnotations.Count);
            Assert.Equal("It", operation.RemovedColumnAnnotations["Everyone's"]["At"]);
        }

        [Fact]
        public void DropTable_can_build_operation_with_name_and_just_table_annotations()
        {
            var migration = new TestMigration();

            migration.DropTable(
                "Customers",
                new Dictionary<string, object>
                {
                    { "Blue", "Lips" }
                });

            var operation = migration.Operations.Cast<DropTableOperation>().Single();

            Assert.Equal("Customers", operation.Name);

            Assert.Equal(1, operation.RemovedAnnotations.Count);
            Assert.Equal("Lips", operation.RemovedAnnotations["Blue"]);

            Assert.Equal(0, operation.RemovedColumnAnnotations.Count);
        }

        [Fact]
        public void DropTable_can_build_operation_with_name_and_just_column_annotations()
        {
            var migration = new TestMigration();

            migration.DropTable(
                "Customers",
                new Dictionary<string, IDictionary<string, object>>
                {
                    { "Everyone's", new Dictionary<string, object> { { "At", "It" } } }
                });

            var operation = migration.Operations.Cast<DropTableOperation>().Single();

            Assert.Equal("Customers", operation.Name);

            Assert.Equal(0, operation.RemovedAnnotations.Count);

            Assert.Equal(1, operation.RemovedColumnAnnotations.Count);
            Assert.Equal("It", operation.RemovedColumnAnnotations["Everyone's"]["At"]);
        }

        [Fact]
        public void DropTable_can_build_operation_with_name_and_all_null_annotations()
        {
            var migration = new TestMigration();

            migration.DropTable(
                "Customers",
                null,
                null);

            var operation = migration.Operations.Cast<DropTableOperation>().Single();

            Assert.Equal("Customers", operation.Name);

            Assert.Equal(0, operation.RemovedAnnotations.Count);

            Assert.Equal(0, operation.RemovedColumnAnnotations.Count);
        }

        [Fact]
        public void DropTable_with_annotations_checks_arguments()
        {
            var migration = new TestMigration();

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(
                    () => migration.DropTable(null, (IDictionary<string, object>)null)).Message);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(
                    () => migration.DropTable(null, (IDictionary<string, IDictionary<string, object>>)null)).Message);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(
                    () => migration.DropTable(null, null, null)).Message);
        }

        [Fact]
        public void MoveStoredProcedure_should_add_move_procedure_operation()
        {
            var migration = new TestMigration();

            migration.MoveStoredProcedure("old", "new");

            var moveProcedureOperation = migration.Operations.Cast<MoveProcedureOperation>().Single();

            Assert.NotNull(moveProcedureOperation);
            Assert.Equal("old", moveProcedureOperation.Name);
            Assert.Equal("new", moveProcedureOperation.NewSchema);
        }

        [Fact]
        public void RenameTable_should_add_rename_table_operation()
        {
            var migration = new TestMigration();

            migration.RenameTable("old", "new");

            var renameTableOperation = migration.Operations.Cast<RenameTableOperation>().Single();

            Assert.NotNull(renameTableOperation);
            Assert.Equal("old", renameTableOperation.Name);
            Assert.Equal("new", renameTableOperation.NewName);
        }

        [Fact]
        public void RenameColumn_should_add_rename_column_operation()
        {
            var migration = new TestMigration();

            migration.RenameColumn("table", "old", "new");

            var renameColumnOperation = migration.Operations.Cast<RenameColumnOperation>().Single();

            Assert.NotNull(renameColumnOperation);
            Assert.Equal("table", renameColumnOperation.Table);
            Assert.Equal("old", renameColumnOperation.Name);
            Assert.Equal("new", renameColumnOperation.NewName);
        }

        [Fact]
        public void RenameIndex_should_add_rename_index_operation()
        {
            var migration = new TestMigration();

            migration.RenameIndex("table", "old", "new");

            var renameIndexOperation = (RenameIndexOperation)migration.Operations.Single();

            Assert.NotNull(renameIndexOperation);
            Assert.Equal("table", renameIndexOperation.Table);
            Assert.Equal("old", renameIndexOperation.Name);
            Assert.Equal("new", renameIndexOperation.NewName);
        }

        [Fact]
        public void CreateIndex_should_add_create_index_operation()
        {
            var migration = new TestMigration();

            migration.CreateIndex("table", new[] { "Foo", "Bar" }, true);

            var createIndexOperation = migration.Operations.Cast<CreateIndexOperation>().Single();

            Assert.Equal("table", createIndexOperation.Table);
            Assert.Equal("Foo", createIndexOperation.Columns.First());
            Assert.Equal("Bar", createIndexOperation.Columns.Last());
            Assert.True(createIndexOperation.IsUnique);
            Assert.False(createIndexOperation.IsClustered);
        }

        [Fact]
        public void CreateIndex_can_set_clustered_parameter()
        {
            var migration = new TestMigration();

            migration.CreateIndex("table", new[] { "Foo", "Bar" }, clustered: true);

            var createIndexOperation = migration.Operations.Cast<CreateIndexOperation>().Single();

            Assert.True(createIndexOperation.IsClustered);
        }

        [Fact]
        public void Sql_should_add_sql_operation()
        {
            var migration = new TestMigration();

            migration.Sql("foo");

            var sqlOperation = migration.Operations.Cast<SqlOperation>().Single();

            Assert.Equal("foo", sqlOperation.Sql);
        }

        [Fact]
        public void SqlFile_should_add_sql_operation()
        {
            var migration = new TestMigration();

            migration.SqlFile("TestDataFiles/SqlOperation_Basic.sql");

            var sqlOperation = migration.Operations.Cast<SqlOperation>().Single();

            Assert.Equal("insert into foo", sqlOperation.Sql);
        }

        [Fact]
        public void SqlFile_should_add_sql_operation_with_rooted_path()
        {
            var migration = new TestMigration();
            var rootedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestDataFiles/SqlOperation_Basic.sql");

            migration.SqlFile(rootedPath);

            var sqlOperation = migration.Operations.Cast<SqlOperation>().Single();

            Assert.Equal("insert into foo", sqlOperation.Sql);
        }

        [Fact]
        public void SqlResource_should_add_sql_operation()
        {
            var migration = new TestMigration();

            migration.SqlResource("System.Data.Entity.TestDataFiles.SqlOperation_Basic.sql");

            var sqlOperation = migration.Operations.Cast<SqlOperation>().Single();

            Assert.Equal("insert into foo", sqlOperation.Sql);
        }

        [Fact]
        public void SqlResource_should_add_sql_operation_from_specific_assembly()
        {
            var migration = new TestMigration();

            migration.SqlResource("System.Data.Entity.TestDataFiles.SqlOperation_Basic.sql", Assembly.GetExecutingAssembly());

            var sqlOperation = migration.Operations.Cast<SqlOperation>().Single();

            Assert.Equal("insert into foo", sqlOperation.Sql);
        }

        [Fact]
        public void Explictly_calling_IDbMigration_should_add_operation()
        {
            var migration = new TestMigration();
            var operation = new Mock<MigrationOperation>(null).Object;

            ((IDbMigration)migration).AddOperation(operation);

            Assert.Equal(1, migration.Operations.Count());
            Assert.Same(operation, migration.Operations.Single());
        }
    }
}
