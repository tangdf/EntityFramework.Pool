// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations
{
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.Migrations.Design;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class NoTestInfraScenarios : TestBase
    {
        [Fact]
        public void Can_generate_migration_from_user_code()
        {
            var migrator
                = new DbMigrator(
                    new DbMigrationsConfiguration
                        {
                            ContextType = typeof(ShopContext_v1),
                            MigrationsAssembly = SystemComponentModelDataAnnotationsAssembly,
                            MigrationsNamespace = "Foo",
                            MigrationsDirectory = "Bar"
                        });

            var migration = new MigrationScaffolder(migrator.Configuration).Scaffold("Test");

            Assert.False(string.IsNullOrWhiteSpace(migration.DesignerCode));
            Assert.False(string.IsNullOrWhiteSpace(migration.Language));
            Assert.False(string.IsNullOrWhiteSpace(migration.MigrationId));
            Assert.False(string.IsNullOrWhiteSpace(migration.UserCode));
            Assert.False(string.IsNullOrWhiteSpace(migration.Directory));
        }

        [Fact] // CodePlex #2579
        public void Repro()
        {
            var connection1 = SimpleConnectionString("Db2579Aa");
            var connection2 = SimpleConnectionString("Db2579Bb");

            using (var context = new Context2579I(connection1))
            {
                context.Database.Delete();
                context.Database.Create();
                context.Entities.Add(new Entity2579());
                context.SaveChanges();
            }

            using (var context = new Context2579I(connection2))
            {
                context.Database.Delete();
            }

            var tests = new Action[100];
            for (var i = 0; i < 100; i++)
            {
                tests[i] = () =>
                {
                    using (var context = new Context2579(connection1))
                    {
                        Assert.Equal(1, context.Entities.Count());
                    }
                };
            }

            tests[25] = () =>
            {
                using (new Context2579(connection2))
                {
                    var migrationConfiguration = new Configuration2579(connection2);
                    var migrator = new DbMigrator(migrationConfiguration);

                    migrator.Update();
                }
            };

            Parallel.Invoke(tests);
        }

        public class Context2579 : DbContext
        {
            static Context2579()
            {
                Database.SetInitializer<Context2579>(null);
            }

            public Context2579()
            {
            }

            public Context2579(string nameOrConnectionString)
                : base(nameOrConnectionString)
            {
            }

            public DbSet<Entity2579> Entities { get; set; }
        }

        public class Context2579I : DbContext
        {
            static Context2579I()
            {
                Database.SetInitializer<Context2579I>(null);
            }

            public Context2579I(string nameOrConnectionString)
                : base(nameOrConnectionString)
            {
            }

            public DbSet<Entity2579> Entities { get; set; }
        }

        public class Entity2579
        {
            public int Id { get; set; }
        }

        internal sealed class Configuration2579 : DbMigrationsConfiguration<Context2579>
        {
            public Configuration2579()
            {
                AutomaticMigrationsEnabled = true;
            }

            public Configuration2579(string connectionString)
            {
                AutomaticMigrationsEnabled = true;
                TargetDatabase = new DbConnectionInfo(connectionString, "System.Data.SqlClient");
            }
        }
    }
}
