// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.DependencyResolution
{
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.Common;
    using System.Data.Entity.Core.Common;
    using System.Data.Entity.Infrastructure.Interception;
    using System.Data.Entity.Internal;
    using System.Data.Entity.Internal.ConfigFile;
    using System.Data.Entity.ModelConfiguration.Internal.UnitTests;
    using System.Data.Entity.SqlServer;
    using System.Data.Entity.SqlServerCompact;
    using System.Data.Entity.Utilities;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Moq;
    using Xunit;

    public class AppConfigDependencyResolverTests : AppConfigTestBase
    {
        public interface IPilkington
        {
        }

        public class FakeConnectionFactory : IDbConnectionFactory
        {
            public DbConnection CreateConnection(string nameOrConnectionString)
            {
                throw new NotImplementedException();
            }
        }

        public class GetService : AppConfigTestBase
        {
            [Fact]
            public void GetService_returns_null_for_unknown_contract_type()
            {
                Assert.Null(
                    new AppConfigDependencyResolver(CreateAppConfig(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetService<IPilkington>("Karl"));
            }

            [Fact]
            public void GetService_returns_registered_provider()
            {
                Assert.Same(
                    ProviderServicesFactoryTests.FakeProviderWithPublicProperty.Instance,
                    new AppConfigDependencyResolver(
                        CreateAppConfigWithProvider(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetService<DbProviderServices>("Is.Ee.Avin.A.Larf"));
            }

            [Fact]
            public void GetService_returns_null_for_unregistered_provider_name()
            {
                Assert.Null(
                    new AppConfigDependencyResolver(
                        CreateAppConfigWithProvider(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetService<DbProviderServices>("Are.You.Avin.A.Larf"));
            }

            [Fact]
            public void GetService_returns_null_for_null_empty_or_whitespace_provider_name()
            {
                Assert.Null(
                    new AppConfigDependencyResolver(CreateAppConfig(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetService<DbProviderServices>(null));
                Assert.Null(
                    new AppConfigDependencyResolver(CreateAppConfig(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetService<DbProviderServices>(""));
                Assert.Null(
                    new AppConfigDependencyResolver(CreateAppConfig(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetService<DbProviderServices>(" "));
            }

            [Fact]
            public void GetService_caches_provider()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices)
                    .Returns(new[] { new NamedDbProviderService("Ask.Rhod.Gilbert", new Mock<DbProviderServices>().Object) });

                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object,
                    new Mock<InternalConfiguration>(null, null, null, null, null).Object,
                    new Mock<ProviderServicesFactory>().Object);

                var factoryInstance = resolver.GetService<DbProviderServices>("Ask.Rhod.Gilbert");

                Assert.NotNull(factoryInstance);
                mockConfig.Verify(m => m.DbProviderServices, Times.Once());
                Assert.Same(factoryInstance, resolver.GetService<DbProviderServices>("Ask.Rhod.Gilbert"));
                mockConfig.Verify(m => m.DbProviderServices, Times.Once());
            }

            [Fact]
            public void GetService_caches_the_fact_that_no_provider_is_registered()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);

                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                Assert.Null(resolver.GetService<DbProviderServices>("Ask.Rhod.Gilbert"));
                mockConfig.Verify(m => m.DbProviderServices, Times.Once());
                Assert.Null(resolver.GetService<DbProviderServices>("Ask.Rhod.Gilbert"));
                mockConfig.Verify(m => m.DbProviderServices, Times.Once());
            }

            [Fact]
            public void GetService_registers_all_providers_as_default_resolvers_in_order_the_first_time_any_service_is_requested()
            {
                var mockSection = CreateMockSectionWithProviders();
                var mockFactory = CreateMockFactory(mockSection.Object);
                var appConfig = new AppConfig(new ConnectionStringSettingsCollection(), null, mockSection.Object, mockFactory.Object);
                var mockConfiguration = new Mock<InternalConfiguration>(null, null, null, null, null);

                var resolvers = new List<IDbDependencyResolver>();

                mockConfiguration.Setup(
                    m => m.AddDefaultResolver(It.IsAny<IDbDependencyResolver>())).Callback<IDbDependencyResolver>(resolvers.Add);

                new AppConfigDependencyResolver(appConfig, mockConfiguration.Object, mockFactory.Object).GetService<IPilkington>();

                Assert.Equal(3, resolvers.Count);

                Assert.Equal("Around.The.World", resolvers[0].GetService<string>());
                Assert.Equal("One.More.Time", resolvers[1].GetService<string>());
                Assert.Equal("Robot.Rock", resolvers[2].GetService<string>());
            }

            [Fact]
            public void GetService_registers_all_providers_from_real_app_config_as_default_resolvers_in_correct_order()
            {
                var mockConfiguration = new Mock<InternalConfiguration>(null, null, null, null, null);
                var resolvers = new List<IDbDependencyResolver>();

                mockConfiguration.Setup(
                    m => m.AddDefaultResolver(It.IsAny<IDbDependencyResolver>())).Callback<IDbDependencyResolver>(resolvers.Add);

                new AppConfigDependencyResolver(AppConfig.DefaultInstance, mockConfiguration.Object).GetService<IPilkington>();

                Assert.Equal(3, resolvers.Count);

                Assert.IsType<FakeSqlProviderServices>(resolvers[0]);
                Assert.IsType<SqlCeProviderServices>(resolvers[1]);
                Assert.IsType<SqlProviderServices>(resolvers[2]);
            }

            [Fact]
            public void GetService_registers_all_providers_as_default_resolvers_only_once()
            {
                var mockSection = CreateMockSectionWithProviders();
                var mockFactory = CreateMockFactory(mockSection.Object);
                var appConfig = new AppConfig(new ConnectionStringSettingsCollection(), null, mockSection.Object, mockFactory.Object);
                var mockConfiguration = new Mock<InternalConfiguration>(null, null, null, null, null);

                var resolver = new AppConfigDependencyResolver(appConfig, mockConfiguration.Object, mockFactory.Object);

                resolver.GetService<IPilkington>();

                mockConfiguration.Verify(m => m.AddDefaultResolver(It.IsAny<DbProviderServices>()), Times.Exactly(3));

                resolver.GetService<IPilkington>();

                mockConfiguration.Verify(m => m.AddDefaultResolver(It.IsAny<DbProviderServices>()), Times.Exactly(3));
            }

            [Fact]
            public void GetService_registers_SQL_Server_as_a_fallback_if_it_is_not_already_registered()
            {
                var providerTypeName = string.Format(
                    CultureInfo.InvariantCulture,
                    "System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer, Version={0}, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                    new AssemblyName(typeof(DbContext).Assembly().FullName).Version);

                var mockSqlProvider = new Mock<DbProviderServices>();
                mockSqlProvider.Setup(m => m.GetService(typeof(string), null)).Returns("System.Data.SqlClient");

                var mockSection = CreateMockSectionWithProviders();
                var mockFactory = CreateMockFactory(mockSection.Object);
                mockFactory.Setup(m => m.TryGetInstance(providerTypeName))
                    .Returns(mockSqlProvider.Object);

                var appConfig = new AppConfig(new ConnectionStringSettingsCollection(), null, mockSection.Object, mockFactory.Object);

                var mockConfiguration = new Mock<InternalConfiguration>(null, null, null, null, null);

                var resolvers = new ResolverChain();
                mockConfiguration.Setup(m => m.AddDefaultResolver(It.IsAny<IDbDependencyResolver>()))
                    .Callback<IDbDependencyResolver>(resolvers.Add);

                new AppConfigDependencyResolver(appConfig, mockConfiguration.Object, mockFactory.Object).GetService<IPilkington>();

                mockConfiguration.Verify(m => m.AddDefaultResolver(It.IsAny<DbProviderServices>()), Times.Exactly(3));
                mockConfiguration.Verify(
                    m => m.AddDefaultResolver(It.IsAny<SingletonDependencyResolver<DbProviderServices>>()), Times.Never());
                mockConfiguration.Verify(
                    m => m.SetDefaultProviderServices(mockSqlProvider.Object, SqlProviderServices.ProviderInvariantName), Times.Once());

                Assert.Equal("Robot.Rock", resolvers.GetService<string>());
                Assert.Null(resolvers.GetService<DbProviderServices>("System.Data.SqlClient"));
            }

            [Fact]
            public void GetService_returns_connection_factory_set_in_config()
            {
                try
                {
                    Assert.IsType<FakeConnectionFactory>(
                        new AppConfigDependencyResolver(
                            new AppConfig(
                                CreateEmptyConfig().AddDefaultConnectionFactory(typeof(FakeConnectionFactory).AssemblyQualifiedName)),
                            new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                            .GetService<IDbConnectionFactory>());
                }
                finally
                {
                    Database.ResetDefaultConnectionFactory();
                }
            }

            [Fact]
            public void GetService_caches_connection_factory()
            {
                try
                {
                    var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                    mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                    mockConfig.Setup(m => m.TryGetDefaultConnectionFactory()).Returns(new FakeConnectionFactory());
                    var resolver = new AppConfigDependencyResolver(
                        mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                    var factoryInstance = resolver.GetService<IDbConnectionFactory>();

                    Assert.NotNull(factoryInstance);
                    mockConfig.Verify(m => m.TryGetDefaultConnectionFactory(), Times.Once());
                    Assert.Same(factoryInstance, resolver.GetService<IDbConnectionFactory>());
                    mockConfig.Verify(m => m.TryGetDefaultConnectionFactory(), Times.Once());
                }
                finally
                {
                    Database.ResetDefaultConnectionFactory();
                }
            }

            [Fact]
            public void GetService_caches_the_fact_that_no_connection_factory_is_set()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                mockConfig.Setup(m => m.TryGetDefaultConnectionFactory()).Returns((IDbConnectionFactory)null);
                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                Assert.Null(resolver.GetService<IDbConnectionFactory>());
                mockConfig.Verify(m => m.TryGetDefaultConnectionFactory(), Times.Once());
                Assert.Null(resolver.GetService<IDbConnectionFactory>());
                mockConfig.Verify(m => m.TryGetDefaultConnectionFactory(), Times.Once());
            }

            [Fact]
            public void GetService_returns_registered_database_initializer()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                mockConfig.Setup(m => m.Initializers).Returns(
                    new InitializerConfig(
                        CreateEfSection(initializerDisabled: false),
                        new KeyValueConfigurationCollection()));

                Assert.IsType<FakeInitializer<FakeContext>>(
                    new AppConfigDependencyResolver(mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetService<IDatabaseInitializer<FakeContext>>());
            }

            [Fact]
            public void GetService_returns_null_for_unregistered_database_initializer()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                mockConfig.Setup(m => m.Initializers).Returns(
                    new InitializerConfig(
                        CreateEfSection(initializerDisabled: false),
                        new KeyValueConfigurationCollection()));

                Assert.Null(
                    new AppConfigDependencyResolver(mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetService<IDatabaseInitializer<DbContext>>());
            }

            [Fact]
            public void GetService_caches_database_initializer()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                mockConfig.Setup(m => m.Initializers).Returns(
                    new InitializerConfig(
                        CreateEfSection(initializerDisabled: false),
                        new KeyValueConfigurationCollection()));

                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);
                var initializer = resolver.GetService<IDatabaseInitializer<FakeContext>>();

                Assert.NotNull(initializer);
                mockConfig.Verify(m => m.Initializers, Times.Once());
                Assert.Same(initializer, resolver.GetService<IDatabaseInitializer<FakeContext>>());
                mockConfig.Verify(m => m.Initializers, Times.Once());
            }

            [Fact]
            public void GetService_caches_the_fact_that_no_database_initializer_is_registered()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                mockConfig.Setup(m => m.Initializers).Returns(
                    new InitializerConfig(
                        CreateEfSection(initializerDisabled: false),
                        new KeyValueConfigurationCollection()));

                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                Assert.Null(resolver.GetService<IDatabaseInitializer<DbContext>>());
                mockConfig.Verify(m => m.Initializers, Times.Once());
                Assert.Null(resolver.GetService<IDatabaseInitializer<DbContext>>());
                mockConfig.Verify(m => m.Initializers, Times.Once());
            }

            [Fact]
            public void EF_provider_can_be_loaded_from_real_app_config()
            {
                Assert.Same(
                    FakeSqlProviderServices.Instance,
                    new AppConfigDependencyResolver(
                        AppConfig.DefaultInstance, new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetService<DbProviderServices>("System.Data.FakeSqlClient"));
            }

            /// <summary>
            /// This test makes calls from multiple threads such that we have at least some chance of finding threading
            /// issues. As with any test of this type just because the test passes does not mean that the code is
            /// correct. On the other hand if this test ever fails (EVEN ONCE) then we know there is a problem to
            /// be investigated. DON'T just re-run and think things are okay if the test then passes.
            /// </summary>
            [Fact]
            public void GetService_can_be_accessed_from_multiple_threads_concurrently()
            {
                try
                {
                    var appConfig = new AppConfig(
                        CreateEmptyConfig().AddDefaultConnectionFactory(typeof(FakeConnectionFactory).AssemblyQualifiedName));

                    for (var i = 0; i < 30; i++)
                    {
                        var bag = new ConcurrentBag<IDbConnectionFactory>();
                        var resolver = new AppConfigDependencyResolver(
                            appConfig, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                        ExecuteInParallel(() => bag.Add(resolver.GetService<IDbConnectionFactory>()));

                        Assert.Equal(20, bag.Count);
                        Assert.True(bag.All(c => resolver.GetService<IDbConnectionFactory>() == c));
                    }
                }
                finally
                {
                    Database.ResetDefaultConnectionFactory();
                }
            }
        }

        public class GetServices : AppConfigTestBase
        {
            [Fact]
            public void GetServices_returns_empty_list_for_unknown_contract_type()
            {
                Assert.Empty(
                    new AppConfigDependencyResolver(CreateAppConfig(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices(typeof(IPilkington), "Karl"));
            }

            [Fact]
            public void GetServices_returns_empty_list_for_unknown_contract_type_when_using_extension_method()
            {
                Assert.Empty(
                    new AppConfigDependencyResolver(CreateAppConfig(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<IPilkington>("Karl"));
            }

            [Fact]
            public void GetServices_returns_registered_provider()
            {
                Assert.Same(
                    ProviderServicesFactoryTests.FakeProviderWithPublicProperty.Instance,
                    new AppConfigDependencyResolver(
                        CreateAppConfigWithProvider(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<DbProviderServices>("Is.Ee.Avin.A.Larf").Single());
            }

            [Fact]
            public void GetServices_returns_empty_list_for_unregistered_provider_name()
            {
                Assert.Empty(
                    new AppConfigDependencyResolver(
                        CreateAppConfigWithProvider(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<DbProviderServices>("Are.You.Avin.A.Larf"));
            }

            [Fact]
            public void GetServices_returns_empty_list_for_null_empty_or_whitespace_provider_name()
            {
                Assert.Empty(
                    new AppConfigDependencyResolver(CreateAppConfig(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<DbProviderServices>(null));
                Assert.Empty(
                    new AppConfigDependencyResolver(CreateAppConfig(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<DbProviderServices>(""));
                Assert.Empty(
                    new AppConfigDependencyResolver(CreateAppConfig(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<DbProviderServices>(" "));
            }

            [Fact]
            public void GetServices_caches_provider()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices)
                    .Returns(new[] { new NamedDbProviderService("Ask.Rhod.Gilbert", new Mock<DbProviderServices>().Object) });

                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object,
                    new Mock<InternalConfiguration>(null, null, null, null, null).Object,
                    new Mock<ProviderServicesFactory>().Object);

                var factoryInstance = resolver.GetServices<DbProviderServices>("Ask.Rhod.Gilbert").Single();

                Assert.NotNull(factoryInstance);
                mockConfig.Verify(m => m.DbProviderServices, Times.Once());
                Assert.Same(factoryInstance, resolver.GetServices<DbProviderServices>("Ask.Rhod.Gilbert").Single());
                mockConfig.Verify(m => m.DbProviderServices, Times.Once());
            }

            [Fact]
            public void GetServices_caches_the_fact_that_no_provider_is_registered()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);

                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                Assert.Empty(resolver.GetServices<DbProviderServices>("Ask.Rhod.Gilbert"));
                mockConfig.Verify(m => m.DbProviderServices, Times.Once());
                Assert.Empty(resolver.GetServices<DbProviderServices>("Ask.Rhod.Gilbert"));
                mockConfig.Verify(m => m.DbProviderServices, Times.Once());
            }

            [Fact]
            public void GetServices_registers_all_providers_as_default_resolvers_in_order_the_first_time_any_service_is_requested()
            {
                var mockSection = CreateMockSectionWithProviders();
                var mockFactory = CreateMockFactory(mockSection.Object);
                var appConfig = new AppConfig(new ConnectionStringSettingsCollection(), null, mockSection.Object, mockFactory.Object);
                var mockConfiguration = new Mock<InternalConfiguration>(null, null, null, null, null);

                var resolvers = new List<IDbDependencyResolver>();

                mockConfiguration.Setup(
                    m => m.AddDefaultResolver(It.IsAny<IDbDependencyResolver>())).Callback<IDbDependencyResolver>(resolvers.Add);

                new AppConfigDependencyResolver(appConfig, mockConfiguration.Object, mockFactory.Object).GetServices<IPilkington>();

                Assert.Equal(3, resolvers.Count);

                Assert.Equal("Around.The.World", resolvers[0].GetService<string>());
                Assert.Equal("One.More.Time", resolvers[1].GetService<string>());
                Assert.Equal("Robot.Rock", resolvers[2].GetService<string>());
            }

            [Fact]
            public void GetServices_registers_all_providers_from_real_app_config_as_default_resolvers_in_correct_order()
            {
                var mockConfiguration = new Mock<InternalConfiguration>(null, null, null, null, null);
                var resolvers = new List<IDbDependencyResolver>();

                mockConfiguration.Setup(
                    m => m.AddDefaultResolver(It.IsAny<IDbDependencyResolver>())).Callback<IDbDependencyResolver>(resolvers.Add);

                new AppConfigDependencyResolver(AppConfig.DefaultInstance, mockConfiguration.Object).GetServices<IPilkington>();

                Assert.Equal(3, resolvers.Count);

                Assert.IsType<FakeSqlProviderServices>(resolvers[0]);
                Assert.IsType<SqlCeProviderServices>(resolvers[1]);
                Assert.IsType<SqlProviderServices>(resolvers[2]);
            }

            [Fact]
            public void GetServices_registers_all_providers_as_default_resolvers_only_once()
            {
                var mockSection = CreateMockSectionWithProviders();
                var mockFactory = CreateMockFactory(mockSection.Object);
                var appConfig = new AppConfig(new ConnectionStringSettingsCollection(), null, mockSection.Object, mockFactory.Object);
                var mockConfiguration = new Mock<InternalConfiguration>(null, null, null, null, null);

                var resolver = new AppConfigDependencyResolver(appConfig, mockConfiguration.Object, mockFactory.Object);

                resolver.GetServices<IPilkington>();

                mockConfiguration.Verify(m => m.AddDefaultResolver(It.IsAny<DbProviderServices>()), Times.Exactly(3));

                resolver.GetServices<IPilkington>();

                mockConfiguration.Verify(m => m.AddDefaultResolver(It.IsAny<DbProviderServices>()), Times.Exactly(3));
            }

            [Fact]
            public void GetServices_registers_SQL_Server_as_a_fallback_if_it_is_not_already_registered()
            {
                var providerTypeName = string.Format(
                    CultureInfo.InvariantCulture,
                    "System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer, Version={0}, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                    new AssemblyName(typeof(DbContext).Assembly().FullName).Version);

                var mockSqlProvider = new Mock<DbProviderServices>();
                mockSqlProvider.Setup(m => m.GetService(typeof(string), null)).Returns("System.Data.SqlClient");

                var mockSection = CreateMockSectionWithProviders();
                var mockFactory = CreateMockFactory(mockSection.Object);
                mockFactory.Setup(m => m.TryGetInstance(providerTypeName))
                    .Returns(mockSqlProvider.Object);

                var appConfig = new AppConfig(new ConnectionStringSettingsCollection(), null, mockSection.Object, mockFactory.Object);

                var mockConfiguration = new Mock<InternalConfiguration>(null, null, null, null, null);

                var resolvers = new ResolverChain();
                mockConfiguration.Setup(m => m.AddDefaultResolver(It.IsAny<IDbDependencyResolver>()))
                    .Callback<IDbDependencyResolver>(resolvers.Add);

                new AppConfigDependencyResolver(appConfig, mockConfiguration.Object, mockFactory.Object).GetServices<IPilkington>();

                mockConfiguration.Verify(m => m.AddDefaultResolver(It.IsAny<DbProviderServices>()), Times.Exactly(3));
                mockConfiguration.Verify(
                    m => m.AddDefaultResolver(It.IsAny<SingletonDependencyResolver<DbProviderServices>>()), Times.Never());
                mockConfiguration.Verify(
                    m => m.SetDefaultProviderServices(mockSqlProvider.Object, SqlProviderServices.ProviderInvariantName), Times.Once());

                Assert.Equal("Robot.Rock", resolvers.GetService<string>());
                Assert.Null(resolvers.GetService<DbProviderServices>("System.Data.SqlClient"));
            }

            [Fact]
            public void GetServices_returns_connection_factory_set_in_config()
            {
                try
                {
                    Assert.IsType<FakeConnectionFactory>(
                        new AppConfigDependencyResolver(
                            new AppConfig(
                                CreateEmptyConfig().AddDefaultConnectionFactory(typeof(FakeConnectionFactory).AssemblyQualifiedName)),
                            new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                            .GetServices<IDbConnectionFactory>().Single());
                }
                finally
                {
                    Database.ResetDefaultConnectionFactory();
                }
            }

            [Fact]
            public void GetServices_returns_empty_list_if_no_connection_factory_is_set_in_config()
            {
                Assert.Empty(
                    new AppConfigDependencyResolver(CreateAppConfig(), new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<IDbConnectionFactory>());
            }

            [Fact]
            public void GetServices_caches_connection_factory()
            {
                try
                {
                    var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                    mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                    mockConfig.Setup(m => m.TryGetDefaultConnectionFactory()).Returns(new FakeConnectionFactory());
                    var resolver = new AppConfigDependencyResolver(
                        mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                    var factoryInstance = resolver.GetServices<IDbConnectionFactory>().Single();

                    Assert.NotNull(factoryInstance);
                    mockConfig.Verify(m => m.TryGetDefaultConnectionFactory(), Times.Once());
                    Assert.Same(factoryInstance, resolver.GetServices<IDbConnectionFactory>().Single());
                    mockConfig.Verify(m => m.TryGetDefaultConnectionFactory(), Times.Once());
                }
                finally
                {
                    Database.ResetDefaultConnectionFactory();
                }
            }

            [Fact]
            public void GetServices_caches_the_fact_that_no_connection_factory_is_set()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                mockConfig.Setup(m => m.TryGetDefaultConnectionFactory()).Returns((IDbConnectionFactory)null);
                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                Assert.Empty(resolver.GetServices<IDbConnectionFactory>());
                mockConfig.Verify(m => m.TryGetDefaultConnectionFactory(), Times.Once());
                Assert.Empty(resolver.GetServices<IDbConnectionFactory>());
                mockConfig.Verify(m => m.TryGetDefaultConnectionFactory(), Times.Once());
            }

            [Fact]
            public void GetServices_returns_registered_database_initializer()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                mockConfig.Setup(m => m.Initializers).Returns(
                    new InitializerConfig(
                        CreateEfSection(initializerDisabled: false),
                        new KeyValueConfigurationCollection()));

                Assert.IsType<FakeInitializer<FakeContext>>(
                    new AppConfigDependencyResolver(mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<IDatabaseInitializer<FakeContext>>().Single());
            }

            [Fact]
            public void GetServices_returns_empty_list_for_unregistered_database_initializer()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                mockConfig.Setup(m => m.Initializers).Returns(
                    new InitializerConfig(
                        CreateEfSection(initializerDisabled: false),
                        new KeyValueConfigurationCollection()));

                Assert.Empty(
                    new AppConfigDependencyResolver(mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<IDatabaseInitializer<DbContext>>());
            }

            [Fact]
            public void GetServices_caches_database_initializer()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                mockConfig.Setup(m => m.Initializers).Returns(
                    new InitializerConfig(
                        CreateEfSection(initializerDisabled: false),
                        new KeyValueConfigurationCollection()));

                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);
                var initializer = resolver.GetServices<IDatabaseInitializer<FakeContext>>().Single();

                Assert.NotNull(initializer);
                mockConfig.Verify(m => m.Initializers, Times.Once());
                Assert.Same(initializer, resolver.GetServices<IDatabaseInitializer<FakeContext>>().Single());
                mockConfig.Verify(m => m.Initializers, Times.Once());
            }

            [Fact]
            public void GetServices_caches_the_fact_that_no_database_initializer_is_registered()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.DbProviderServices).Returns(new NamedDbProviderService[0]);
                mockConfig.Setup(m => m.Initializers).Returns(
                    new InitializerConfig(
                        CreateEfSection(initializerDisabled: false),
                        new KeyValueConfigurationCollection()));

                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                Assert.Empty(resolver.GetServices<IDatabaseInitializer<DbContext>>());
                mockConfig.Verify(m => m.Initializers, Times.Once());
                Assert.Empty(resolver.GetServices<IDatabaseInitializer<DbContext>>());
                mockConfig.Verify(m => m.Initializers, Times.Once());
            }

            [Fact]
            public void GetServices_returns_registered_interceptors()
            {
                var interceptor1 = new Mock<IDbInterceptor>().Object;
                var interceptor2 = new Mock<IDbCommandInterceptor>().Object;
                var interceptor3 = new Mock<IDbConfigurationInterceptor>().Object;

                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.Interceptors)
                    .Returns(new List<IDbInterceptor> { interceptor1, interceptor2, interceptor2, interceptor3 });

                var interceptors =
                    new AppConfigDependencyResolver(mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<IDbInterceptor>().ToList();

                Assert.Equal(4, interceptors.Count);
                Assert.Same(interceptor1, interceptors[0]);
                Assert.Same(interceptor2, interceptors[1]);
                Assert.Same(interceptor2, interceptors[2]);
                Assert.Same(interceptor3, interceptors[3]);
            }

            [Fact]
            public void GetServices_returns_empty_list_if_no_registered_interceptors()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.Interceptors).Returns(Enumerable.Empty<IDbInterceptor>);

                Assert.Empty(
                    new AppConfigDependencyResolver(mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object)
                        .GetServices<IDbInterceptor>());
            }

            [Fact]
            public void GetServices_caches_registered_interceptors()
            {
                var mockConfig = new Mock<AppConfig>(new ConnectionStringSettingsCollection());
                mockConfig.Setup(m => m.Interceptors).Returns(new List<IDbInterceptor> { new Mock<IDbInterceptor>().Object });

                var resolver = new AppConfigDependencyResolver(
                    mockConfig.Object, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                Assert.Equal(1, resolver.GetServices<IDbInterceptor>().Count());
                mockConfig.Verify(m => m.Interceptors, Times.Once());

                Assert.Equal(1, resolver.GetServices<IDbInterceptor>().Count());
                mockConfig.Verify(m => m.Interceptors, Times.Once());
            }

            /// <summary>
            /// This test makes calls from multiple threads such that we have at least some chance of finding threading
            /// issues. As with any test of this type just because the test passes does not mean that the code is
            /// correct. On the other hand if this test ever fails (EVEN ONCE) then we know there is a problem to
            /// be investigated. DON'T just re-run and think things are okay if the test then passes.
            /// </summary>
            [Fact]
            public void GetServices_can_be_accessed_from_multiple_threads_concurrently()
            {
                try
                {
                    var appConfig = new AppConfig(
                        CreateEmptyConfig().AddDefaultConnectionFactory(typeof(FakeConnectionFactory).AssemblyQualifiedName));

                    for (var i = 0; i < 30; i++)
                    {
                        var bag = new ConcurrentBag<IDbConnectionFactory>();
                        var resolver = new AppConfigDependencyResolver(
                            appConfig, new Mock<InternalConfiguration>(null, null, null, null, null).Object);

                        ExecuteInParallel(() => bag.Add(resolver.GetService<IDbConnectionFactory>()));

                        Assert.Equal(20, bag.Count);
                        Assert.True(bag.All(c => resolver.GetServices<IDbConnectionFactory>().Single() == c));
                    }
                }
                finally
                {
                    Database.ResetDefaultConnectionFactory();
                }
            }
        }

        private static EntityFrameworkSection CreateEfSection(bool initializerDisabled)
        {
            var mockDatabaseInitializerElement = new Mock<DatabaseInitializerElement>();
            mockDatabaseInitializerElement
                .Setup(m => m.InitializerTypeName)
                .Returns(typeof(FakeInitializer<FakeContext>).AssemblyQualifiedName);
            mockDatabaseInitializerElement.Setup(m => m.Parameters).Returns(new ParameterCollection());

            var mockContextElement = new Mock<ContextElement>();
            mockContextElement.Setup(m => m.IsDatabaseInitializationDisabled).Returns(initializerDisabled);
            mockContextElement.Setup(m => m.ContextTypeName).Returns(typeof(FakeContext).AssemblyQualifiedName);
            mockContextElement.Setup(m => m.DatabaseInitializer).Returns(mockDatabaseInitializerElement.Object);

            var mockContextCollection = new Mock<ContextCollection>();
            mockContextCollection.As<IEnumerable>().Setup(m => m.GetEnumerator()).Returns(
                new List<ContextElement>
                        {
                            mockContextElement.Object
                        }.GetEnumerator());

            var mockEfSection = new Mock<EntityFrameworkSection>();
            mockEfSection.Setup(m => m.Contexts).Returns(mockContextCollection.Object);

            return mockEfSection.Object;
        }

        public class FakeContext : DbContext
        {
        }

        public class FakeInitializer<TContext> : IDatabaseInitializer<TContext>
            where TContext : DbContext
        {
            public void InitializeDatabase(TContext context)
            {
            }
        }

        private static AppConfig CreateAppConfigWithProvider(string sqlGeneratorName = null)
        {
            return CreateAppConfig(
                "Is.Ee.Avin.A.Larf",
                typeof(ProviderServicesFactoryTests.FakeProviderWithPublicProperty).AssemblyQualifiedName);
        }

        private static Mock<EntityFrameworkSection> CreateMockSectionWithProviders()
        {
            var providers = new ProviderCollection();
            providers.AddProvider("Around.The.World", "Around.The.World.Type");
            providers.AddProvider("One.More.Time", "One.More.Time.Type");
            providers.AddProvider("Robot.Rock", "Robot.Rock.Type");

            var mockSection = new Mock<EntityFrameworkSection>();
            mockSection.Setup(m => m.Providers).Returns(providers);
            mockSection.Setup(m => m.DefaultConnectionFactory).Returns(new DefaultConnectionFactoryElement());

            return mockSection;
        }

        private static Mock<ProviderServicesFactory> CreateMockFactory(EntityFrameworkSection section)
        {
            var mockFactory = new Mock<ProviderServicesFactory>();
            section.Providers.OfType<ProviderElement>().Each(
                e =>
                {
                    var mockServices = new Mock<DbProviderServices>();
                    mockServices.Setup(m => m.GetService(typeof(string), null)).Returns((object)e.InvariantName);
                    mockFactory.Setup<DbProviderServices>(m => m.GetInstance(e.ProviderTypeName, e.InvariantName)).Returns(mockServices.Object);
                });
            return mockFactory;
        }
    }
}
