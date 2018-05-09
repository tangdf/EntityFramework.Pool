// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Edm.Services
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.ModelConfiguration.Configuration.Types;
    using System.Linq;
    using Xunit;

    public sealed class DatabaseMappingGeneratorTests
    {
        [Fact]
        public void Generate_should_initialize_mapping_model()
        {
            var model = new EdmModel(DataSpace.CSpace);

            var databaseMapping = CreateDatabaseMappingGenerator().Generate(model);

            Assert.NotNull(databaseMapping);
            Assert.NotNull(databaseMapping.Database);
            Assert.Same(model.Containers.Single(), databaseMapping.EntityContainerMappings.Single().EdmEntityContainer);
        }

        [Fact]
        public void Generate_can_map_a_simple_entity_type_and_set()
        {
            var model = new EdmModel(DataSpace.CSpace);
            var entityType = model.AddEntityType("E");
            var type = typeof(object);

            entityType.GetMetadataProperties().SetClrType(type);
            var property = EdmProperty.CreatePrimitive("P1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType.AddMember(property);
            var property1 = EdmProperty.CreatePrimitive("P2", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType.AddMember(property1);
            var entitySet = model.AddEntitySet("ESet", entityType);

            var databaseMapping = CreateDatabaseMappingGenerator().Generate(model);

            var entitySetMapping = databaseMapping.GetEntitySetMapping(entitySet);

            Assert.NotNull(entitySetMapping);
            Assert.Same(entitySet, entitySetMapping.EntitySet);

            var entityTypeMapping = entitySetMapping.EntityTypeMappings.Single();

            Assert.Same(entityType, entityTypeMapping.EntityType);
            Assert.NotNull(entityTypeMapping.MappingFragments.Single().Table);
            Assert.Equal("E", entityTypeMapping.MappingFragments.Single().Table.Name);
            Assert.Equal(2, entityTypeMapping.MappingFragments.Single().Table.Properties.Count);
            Assert.Equal(typeof(object), entityTypeMapping.GetClrType());
        }

        [Fact]
        public void Generate_should_correctly_map_string_primitive_property_facets()
        {
            var model = new EdmModel(DataSpace.CSpace);
            var entityType = model.AddEntityType("E");
            var type = typeof(object);

            entityType.GetMetadataProperties().SetClrType(type);
            model.AddEntitySet("ESet", entityType);

            var property
                = EdmProperty
                    .CreatePrimitive("P", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType.AddMember(property);

            property.Nullable = false;
            property.IsFixedLength = true;
            property.IsMaxLength = true;
            property.IsUnicode = true;
            property.MaxLength = 42;
            property.Precision = 23;
            property.Scale = 77;
            property.SetStoreGeneratedPattern(StoreGeneratedPattern.Identity);

            var databaseMapping = CreateDatabaseMappingGenerator().Generate(model);

            var column = databaseMapping.GetEntityTypeMapping(entityType).MappingFragments.Single().ColumnMappings.Single().ColumnProperty;

            Assert.False(column.Nullable);
            Assert.True(column.IsFixedLengthConstant);
            Assert.False(column.IsMaxLength);
            Assert.True(column.IsUnicodeConstant);
            Assert.Equal(42, column.MaxLength);
            Assert.Null(column.Precision);
            Assert.Null(column.Scale);
            Assert.Equal(StoreGeneratedPattern.Identity, column.StoreGeneratedPattern);
        }

        [Fact]
        public void Generate_should_correctly_map_time_primitive_property_facets()
        {
            var model = new EdmModel(DataSpace.CSpace);
            var entityType = model.AddEntityType("E");
            var type = typeof(object);

            entityType.GetMetadataProperties().SetClrType(type);
            model.AddEntitySet("ESet", entityType);

            var property
                = EdmProperty
                    .CreatePrimitive("P", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Time));

            entityType.AddMember(property);

            property.Nullable = false;
            property.IsFixedLength = true;
            property.IsMaxLength = true;
            property.IsUnicode = false;
            property.MaxLength = 42;
            property.Precision = 23;
            property.Scale = 77;
            property.SetStoreGeneratedPattern(StoreGeneratedPattern.Identity);

            var databaseMapping = CreateDatabaseMappingGenerator().Generate(model);

            var column = databaseMapping.GetEntityTypeMapping(entityType).MappingFragments.Single().ColumnMappings.Single().ColumnProperty;

            Assert.False(column.Nullable);
            Assert.Null(column.IsFixedLength);
            Assert.False(column.IsMaxLength);
            Assert.Null(column.IsUnicode);
            Assert.Null(column.MaxLength);
            Assert.Equal<byte?>(23, column.Precision);
            Assert.Null(column.Scale);
            Assert.Equal(StoreGeneratedPattern.Identity, column.StoreGeneratedPattern);
        }

        [Fact]
        public void Generate_should_correctly_map_decimal_primitive_property_facets()
        {
            var model = new EdmModel(DataSpace.CSpace);
            var entityType = model.AddEntityType("E");
            var type = typeof(object);

            entityType.GetMetadataProperties().SetClrType(type);
            model.AddEntitySet("ESet", entityType);

            var property
                = EdmProperty
                    .CreatePrimitive("P", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Decimal));

            entityType.AddMember(property);

            property.Nullable = false;
            property.IsFixedLength = true;
            property.IsMaxLength = true;
            property.IsUnicode = false;
            property.MaxLength = 42;
            property.Precision = 23;
            property.Scale = 77;
            property.SetStoreGeneratedPattern(StoreGeneratedPattern.Identity);

            var databaseMapping = CreateDatabaseMappingGenerator().Generate(model);

            var column = databaseMapping.GetEntityTypeMapping(entityType).MappingFragments.Single().ColumnMappings.Single().ColumnProperty;

            Assert.False(column.Nullable);
            Assert.Null(column.IsFixedLength);
            Assert.False(column.IsMaxLength);
            Assert.Null(column.IsUnicode);
            Assert.Null(column.MaxLength);
            Assert.Equal<byte?>(23, column.Precision);
            Assert.Equal<byte?>(77, column.Scale);
            Assert.Equal(StoreGeneratedPattern.Identity, column.StoreGeneratedPattern);
        }

        [Fact]
        public void Generate_should_map_entity_keys_to_primary_keys()
        {
            var model = new EdmModel(DataSpace.CSpace);
            var entityType = model.AddEntityType("E");
            var type = typeof(object);

            entityType.GetMetadataProperties().SetClrType(type);
            var property = EdmProperty.CreatePrimitive("Id", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType.AddMember(property);
            var idProperty = property;
            entityType.AddKeyMember(idProperty);
            var entitySet = model.AddEntitySet("ESet", entityType);

            var databaseMapping = CreateDatabaseMappingGenerator().Generate(model);

            var entitySetMapping = databaseMapping.GetEntitySetMapping(entitySet);
            var entityTypeMapping = entitySetMapping.EntityTypeMappings.Single();

            Assert.Equal(1, entityTypeMapping.MappingFragments.Single().Table.KeyProperties.Count());
            Assert.Equal("Id", entityTypeMapping.MappingFragments.Single().Table.KeyProperties.Single().Name);
            Assert.True(entityTypeMapping.MappingFragments.Single().Table.KeyProperties.Single().IsPrimaryKeyColumn);
        }

        [Fact]
        public void Generate_can_map_independent_association_type()
        {
            var model = new EdmModel(DataSpace.CSpace);
            var principalEntityType = model.AddEntityType("P");
            var type = typeof(object);

            principalEntityType.GetMetadataProperties().SetClrType(type);
            var property = EdmProperty.CreatePrimitive("Id1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            principalEntityType.AddMember(property);
            var idProperty1 = property;
            principalEntityType.AddKeyMember(idProperty1);
            var property1 = EdmProperty.CreatePrimitive("Id2", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            principalEntityType.AddMember(property1);
            var idProperty2 = property1;
            principalEntityType.AddKeyMember(idProperty2);
            var dependentEntityType = model.AddEntityType("D");
            var type1 = typeof(string);

            dependentEntityType.GetMetadataProperties().SetClrType(type1);
            model.AddEntitySet("PSet", principalEntityType);
            model.AddEntitySet("DSet", dependentEntityType);
            var associationType
                = model.AddAssociationType(
                    "P_D",
                    principalEntityType, RelationshipMultiplicity.One,
                    dependentEntityType, RelationshipMultiplicity.Many);
            model.AddAssociationSet("P_DSet", associationType);
            associationType.SourceEnd.DeleteBehavior = OperationAction.Cascade;

            var databaseMapping = CreateDatabaseMappingGenerator().Generate(model);

            var foreignKeyConstraint
                =
                databaseMapping.GetEntityTypeMapping(dependentEntityType).MappingFragments.Single().Table.ForeignKeyBuilders.Single();

            Assert.Equal(2, foreignKeyConstraint.DependentColumns.Count());
            Assert.Equal(associationType.Name, foreignKeyConstraint.Name);
            Assert.Equal(1, databaseMapping.EntityContainerMappings.Single().AssociationSetMappings.Count());
            Assert.Equal(OperationAction.Cascade, foreignKeyConstraint.DeleteAction);

            var foreignKeyColumn = foreignKeyConstraint.DependentColumns.First();

            Assert.False(foreignKeyColumn.Nullable);
            Assert.Equal("P_Id1", foreignKeyColumn.Name);
        }

        [Fact]
        public void Generate_can_map_foreign_key_association_type()
        {
            var model = new EdmModel(DataSpace.CSpace);

            var principalEntityType = model.AddEntityType("P");
            principalEntityType.GetMetadataProperties().SetClrType(typeof(object));

            var dependentEntityType = model.AddEntityType("D");
            dependentEntityType.GetMetadataProperties().SetClrType(typeof(string));

            var dependentProperty1 = EdmProperty.CreatePrimitive("FK1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.Int32));
            dependentProperty1.Nullable = false;
            dependentEntityType.AddMember(dependentProperty1);

            var dependentProperty2 = EdmProperty.CreatePrimitive("FK2", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));
            dependentEntityType.AddMember(dependentProperty2);

            model.AddEntitySet("PSet", principalEntityType);
            model.AddEntitySet("DSet", dependentEntityType);

            var associationType
                = model.AddAssociationType(
                    "P_D",
                    principalEntityType, RelationshipMultiplicity.One,
                    dependentEntityType, RelationshipMultiplicity.Many);

            associationType.Constraint
                = new ReferentialConstraint(
                    associationType.SourceEnd,
                    associationType.TargetEnd,
                    principalEntityType.KeyProperties,
                    new[] { dependentProperty1, dependentProperty2 });

            associationType.SourceEnd.DeleteBehavior = OperationAction.Cascade;

            var databaseMapping
                = CreateDatabaseMappingGenerator().Generate(model);

            var dependentTable = databaseMapping.GetEntityTypeMapping(dependentEntityType).MappingFragments.Single().Table;
            var foreignKeyConstraint = dependentTable.ForeignKeyBuilders.Single();

            Assert.Equal(2, dependentTable.Properties.Count());
            Assert.Equal(2, foreignKeyConstraint.DependentColumns.Count());
            Assert.Equal(OperationAction.Cascade, foreignKeyConstraint.DeleteAction);
            Assert.Equal(associationType.Name, foreignKeyConstraint.Name);

            var foreignKeyColumn = foreignKeyConstraint.DependentColumns.First();

            Assert.False(foreignKeyColumn.Nullable);
            Assert.Equal("FK1", foreignKeyColumn.Name);
        }

        [Fact]
        public void Generate_can_map_type_hierarchies_using_Tph()
        {
            var model = new EdmModel(DataSpace.CSpace);
            var rootEntityType = model.AddEntityType("E");
            var type = typeof(object);

            rootEntityType.GetMetadataProperties().SetClrType(type);
            var property = EdmProperty.CreatePrimitive("P1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            rootEntityType.AddMember(property);
            var property1 = EdmProperty.CreatePrimitive("P2", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            rootEntityType.AddMember(property1);
            var entitySet = model.AddEntitySet("ESet", rootEntityType);
            var entityType2 = model.AddEntityType("E2");
            var property2 = EdmProperty.CreatePrimitive("P3", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType2.AddMember(property2);
            var type1 = typeof(string);

            entityType2.GetMetadataProperties().SetClrType(type1);
            entityType2.BaseType = rootEntityType;
            var entityType3 = model.AddEntityType("E3");
            var type2 = typeof(int);

            entityType3.GetMetadataProperties().SetClrType(type2);
            var property3 = EdmProperty.CreatePrimitive("P4", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType3.AddMember(property3);
            entityType3.BaseType = entityType2;

            var databaseMapping = CreateDatabaseMappingGenerator().Generate(model);

            var entitySetMapping = databaseMapping.GetEntitySetMapping(entitySet);

            Assert.NotNull(entitySetMapping);
            var entityTypeMappings = entitySetMapping.EntityTypeMappings;

            Assert.Equal(3, entityTypeMappings.Count());

            var entityType1Mapping = databaseMapping.GetEntityTypeMapping(rootEntityType);
            var entityType2Mapping = databaseMapping.GetEntityTypeMapping(entityType2);
            var entityType3Mapping = databaseMapping.GetEntityTypeMapping(entityType3);

            Assert.Equal(2, entityType1Mapping.MappingFragments.Single().ColumnMappings.Count());
            Assert.Equal(3, entityType2Mapping.MappingFragments.Single().ColumnMappings.Count());
            Assert.Equal(4, entityType3Mapping.MappingFragments.Single().ColumnMappings.Count());

            var table = entityType1Mapping.MappingFragments.Single().Table;
            Assert.Same(table, entityType2Mapping.MappingFragments.Single().Table);
            Assert.Same(table, entityType3Mapping.MappingFragments.Single().Table);
            Assert.Equal(5, table.Properties.Count);
            Assert.Equal("P1", table.Properties[0].Name);
            Assert.Equal("P2", table.Properties[1].Name);
            Assert.Equal("P3", table.Properties[2].Name);
            Assert.Equal("P4", table.Properties[3].Name);
            Assert.Equal("Discriminator", table.Properties[4].Name);
        }

        [Fact]
        public void Generate_maps_abstract_type_hierarchies_correctly()
        {
            var model = new EdmModel(DataSpace.CSpace);

            var rootEntityType = model.AddEntityType("E");

            rootEntityType.GetMetadataProperties().SetClrType(typeof(object));

            var property0
                = EdmProperty.CreatePrimitive("P1", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            rootEntityType.AddMember(property0);
            rootEntityType.AddKeyMember(rootEntityType.Properties.First());

            var property1
                = EdmProperty.CreatePrimitive("P2", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            rootEntityType.AddMember(property1);

            model.AddEntitySet("ESet", rootEntityType);

            var entityType2 = model.AddEntityType("E2");

            var property2
                = EdmProperty.CreatePrimitive("P3", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType2.AddMember(property2);
            entityType2.GetMetadataProperties().SetClrType(typeof(string));
            entityType2.Abstract = true;
            entityType2.BaseType = rootEntityType;

            var entityType3 = model.AddEntityType("E3");

            entityType3.GetMetadataProperties().SetClrType(typeof(int));

            var property3
                = EdmProperty.CreatePrimitive("P4", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            entityType3.AddMember(property3);
            entityType3.BaseType = entityType2;

            var databaseMapping = CreateDatabaseMappingGenerator().Generate(model);

            var entityType1Mapping = databaseMapping.GetEntityTypeMapping(rootEntityType);
            var entityType3Mapping = databaseMapping.GetEntityTypeMapping(entityType3);

            Assert.Equal(2, entityType1Mapping.MappingFragments.Single().ColumnMappings.Count());
            Assert.Equal("P1", entityType1Mapping.MappingFragments.Single().ColumnMappings.ElementAt(0).ColumnProperty.Name);
            Assert.Equal("P2", entityType1Mapping.MappingFragments.Single().ColumnMappings.ElementAt(1).ColumnProperty.Name);

            Assert.Equal(4, entityType3Mapping.MappingFragments.Single().ColumnMappings.Count());
            Assert.Equal("P1", entityType3Mapping.MappingFragments.Single().ColumnMappings.ElementAt(0).ColumnProperty.Name);
            Assert.Equal("P2", entityType3Mapping.MappingFragments.Single().ColumnMappings.ElementAt(1).ColumnProperty.Name);
            Assert.Equal("P3", entityType3Mapping.MappingFragments.Single().ColumnMappings.ElementAt(2).ColumnProperty.Name);
            Assert.Equal("P4", entityType3Mapping.MappingFragments.Single().ColumnMappings.ElementAt(3).ColumnProperty.Name);

            var table = entityType1Mapping.MappingFragments.Single().Table;

            Assert.Equal(5, table.Properties.Count);
        }

        [Fact]
        public void Generate_should_not_generate_modification_function_mappings_when_entity_abstract()
        {
            var model = new EdmModel(DataSpace.CSpace);
            var entityType = model.AddEntityType("E");
            entityType.Abstract = true;
            entityType.GetMetadataProperties().SetClrType(typeof(string));
            
            var derivedType = model.AddEntityType("D");
            derivedType.BaseType = entityType;
            derivedType.GetMetadataProperties().SetClrType(typeof(object));

            model.AddEntitySet("ESet", entityType);

            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));
            entityTypeConfiguration.MapToStoredProcedures();
            entityType.SetConfiguration(entityTypeConfiguration);

            var databaseMapping = CreateDatabaseMappingGenerator().Generate(model);

            Assert.Equal(0, databaseMapping.Database.Functions.Count());
        }

        [Fact]
        public void Generate_should_not_generate_modification_function_mappings_when_ends_not_mapped_to_functions()
        {
            var model = new EdmModel(DataSpace.CSpace);

            var entityType1 = model.AddEntityType("E1");
            entityType1.GetMetadataProperties().SetClrType(typeof(string));
            model.AddEntitySet("E1Set", entityType1);

            var entityType2 = model.AddEntityType("E2");
            entityType2.GetMetadataProperties().SetClrType(typeof(string));
            model.AddEntitySet("E2Set", entityType2);

            var entityTypeConfiguration = new EntityTypeConfiguration(typeof(object));
            entityType1.SetConfiguration(entityTypeConfiguration);
            entityType2.SetConfiguration(entityTypeConfiguration);

            model.AddAssociationSet(
                "M2MSet",
                model.AddAssociationType(
                    "M2M",
                    entityType1,
                    RelationshipMultiplicity.Many,
                    entityType2,
                    RelationshipMultiplicity.Many));

            var databaseMapping
                = CreateDatabaseMappingGenerator().Generate(model);

            Assert.Equal(0, databaseMapping.Database.Functions.Count());
        }

        private DatabaseMappingGenerator CreateDatabaseMappingGenerator()
        {
            return new DatabaseMappingGenerator(
                ProviderRegistry.Sql2008_ProviderInfo,
                ProviderRegistry.Sql2008_ProviderManifest);
        }
    }
}
