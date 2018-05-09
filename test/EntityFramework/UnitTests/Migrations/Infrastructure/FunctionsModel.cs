﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Migrations.Infrastructure.FunctionsModel
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Infrastructure;
    using System.Data.Entity.TestHelpers;
    using System.Linq;

    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
    }

    public class Order
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Key { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Code { get; set; }

        public byte[] Signature { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string OrderNo { get; set; }

        [ConcurrencyCheck]
        public string Name { get; set; }

        public Customer Customer { get; set; }
        public Address Address { get; set; }
        public int OrderGroupId { get; set; }
        public byte[] RowVersion { get; set; }

        public ICollection<OrderThing> OrderThings { get; set; }
    }

    public class OrderThing
    {
        public byte[] Id { get; set; }
        public ICollection<Order> Orders { get; set; }
    }

    public class SpecialOrder : Order
    {
        public Address OtherAddress { get; set; }
        public Customer OtherCustomer { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Guid MagicOrderToken { get; set; }
    }

    public class ExtraSpecialOrder : SpecialOrder
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string FairyDust { get; set; }

        public int TheSpecialist { get; set; }
    }

    public class OrderGroup
    {
        public int OrderGroupId { get; set; }
        public ICollection<Order> Orders { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public CountryOrRegion CountryOrRegion { get; set; }
    }

    [ComplexType]
    public class CountryOrRegion
    {
        public string Name { get; set; }
    }

    public class Vehicle
    {
        public int Id { get; set; }
    }

    public class Car : Vehicle
    {
        public string Name { get; set; }
    }

    public class Person
    {
        public int Id { get; set; }

        [ConcurrencyCheck]
        public DateTime BirthDate { get; set; }
    }

    public class JobTask
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public virtual JobTaskState State { get; set; }
    }

    public class JobTaskState
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public virtual JobTask Task { get; set; }
    }

    public class TestContext : DbContext
    {
        static TestContext()
        {
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<TestContext>());
        }

        public static DbModel CreateDynamicUpdateModel()
        {
            using (var context = new TestContext())
            {
                var providerInfo = DatabaseTestHelpers.IsSqlAzure(context.Database.Connection.ConnectionString)
                                       ? ProviderRegistry.SqlAzure2012_ProviderInfo
                                       : ProviderRegistry.Sql2008_ProviderInfo;
                                                   
                return context
                    .InternalContext
                    .CodeFirstModel
                    .CachedModelBuilder
                    .BuildDynamicUpdateModel(providerInfo);
            }
        }

        internal static Tuple<EntityTypeModificationFunctionMapping, EntityContainerMapping>
            GetModificationFunctionMapping(string entityName)
        {
            MetadataWorkspace metadataWorkspace;

            using (var context = new TestContext())
            {
                metadataWorkspace = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;
            }

            var entityContainer
                = metadataWorkspace
                    .GetItems<EntityContainer>(DataSpace.CSpace)
                    .Single();

            var entityType
                = metadataWorkspace
                    .GetItem<EntityType>(typeof(TestContext).Namespace + "." + entityName, DataSpace.CSpace);

            var entityContainerMapping
                = (EntityContainerMapping)metadataWorkspace.GetMap(entityContainer, DataSpace.CSSpace);

            var modificationFunctionMapping
                = entityContainerMapping
                    .EntitySetMappings
                    .SelectMany(esm => esm.ModificationFunctionMappings)
                    .Single(mfm => mfm.EntityType == entityType);

            return Tuple.Create(modificationFunctionMapping, entityContainerMapping);
        }

        internal static Tuple<AssociationSetModificationFunctionMapping, EntityContainerMapping>
            GetAssociationModificationFunctionMapping(string associationName)
        {
            MetadataWorkspace metadataWorkspace;

            using (var context = new TestContext())
            {
                metadataWorkspace = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;
            }

            var entityContainer
                = metadataWorkspace
                    .GetItems<EntityContainer>(DataSpace.CSpace)
                    .Single();

            var associationType
                = metadataWorkspace
                    .GetItem<AssociationType>(typeof(TestContext).Namespace + "." + associationName, DataSpace.CSpace);

            var entityContainerMapping
                = (EntityContainerMapping)metadataWorkspace.GetMap(entityContainer, DataSpace.CSSpace);

            var modificationFunctionMapping
                = entityContainerMapping
                    .AssociationSetMappings
                    .Select(esm => esm.ModificationFunctionMapping)
                    .Single(
                        mfm => mfm != null
                               && mfm.AssociationSet.ElementType == associationType);

            return Tuple.Create(modificationFunctionMapping, entityContainerMapping);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Order>()
                .HasKey(
                    o => new
                             {
                                 o.Id,
                                 o.Key,
                                 o.Code,
                                 o.Signature
                             })
                .MapToStoredProcedures()
                .Property(so => so.Id)
                .HasColumnName("order_id");

            modelBuilder
                .Entity<Order>()
                .Property(o => o.RowVersion)
                .IsRowVersion();

            modelBuilder
                .Entity<OrderGroup>();

            modelBuilder
                .Entity<OrderThing>()
                .HasMany(ot => ot.Orders)
                .WithMany(o => o.OrderThings)
                .MapToStoredProcedures(
                    m =>
                        {
                            m.Insert(
                                c =>
                                    {
                                        c.LeftKeyParameter(o => o.Id, "order_thing_id");
                                        c.RightKeyParameter(o => o.Code, "teh_codez_bro");
                                    });

                            m.Delete(
                                c =>
                                    {
                                        c.LeftKeyParameter(o => o.Id, "order_thing_id");
                                        c.RightKeyParameter(o => o.Code, "teh_codez_bro");
                                    });
                        }
                );

            modelBuilder
                .Entity<SpecialOrder>()
                .ToTable("special_orders")
                .Property(so => so.Key)
                .HasColumnName("so_key");

            modelBuilder
                .Entity<ExtraSpecialOrder>()
                .ToTable("xspecial_orders")
                .MapToStoredProcedures(
                    m =>
                        {
                            m.Insert(
                                c =>
                                    {
                                        c.Parameter(o => o.Name, "the_name");
                                        c.Parameter(o => o.Code, "teh_codez");
                                        c.Result(o => o.Key, "key_result");
                                    });
                            m.Update(
                                c =>
                                    {
                                        c.Parameter(o => o.Key, "key_for_update");
                                        c.Result(o => o.OrderNo, "order_fu");
                                    });
                            m.Delete(c => c.Parameter(o => o.Key, "key_for_delete"));
                        })
                .Property(so => so.Id)
                .HasColumnName("xid");

            modelBuilder
                .Entity<Customer>()
                .MapToStoredProcedures();

            modelBuilder
                .Entity<Vehicle>()
                .MapToStoredProcedures();

            modelBuilder
                .Entity<Person>()
                .MapToStoredProcedures();

            modelBuilder.Entity<JobTask>().ToTable("Tasks").MapToStoredProcedures();
            modelBuilder.Entity<JobTaskState>().ToTable("Tasks").MapToStoredProcedures();
            modelBuilder.Entity<JobTask>().HasRequired(t => t.State).WithRequiredDependent(t => t.Task);
        }
    }

    internal class TestContext_v2 : TestContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .Entity<Order>()
                .MapToStoredProcedures(
                    m =>
                        {
                            m.Insert(c => c.HasName("sproc_A"));
                            m.Update(c => c.Parameter(o => o.Key, "key_for_update2"));
                            m.Delete(c => c.RowsAffectedParameter("affected_rows"));
                        });

            modelBuilder
                .Entity<SpecialOrder>()
                .MapToStoredProcedures(
                    m => m.Insert(c => c.Result(o => o.OrderNo, "order_fu2")));
        }
    }

    internal class TestContext_v2b : TestContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .Entity<OrderThing>()
                .HasMany(ot => ot.Orders)
                .WithMany(o => o.OrderThings)
                .MapToStoredProcedures(
                    m =>
                        {
                            m.Insert(
                                c =>
                                    {
                                        c.HasName("m2m_insert", "foo");
                                        c.LeftKeyParameter(o => o.Id, "order_thing_id2");
                                    });

                            m.Delete(
                                c =>
                                    {
                                        c.HasName("OrderThingOrder_Delete", "bar");
                                        c.RightKeyParameter(o => o.Id, "order_id");
                                    });
                        }
                );
        }
    }

    internal class TestContext_v2c : TestContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .Entity<Customer>()
                .Property(o => o.Name)
                .HasMaxLength(42);
        }
    }
}
