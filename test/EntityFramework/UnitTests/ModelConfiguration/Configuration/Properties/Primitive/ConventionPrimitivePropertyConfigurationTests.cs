﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.ModelConfiguration.Configuration.Types;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Linq;
    using Moq;
    using Xunit;

    public class ConventionPrimitivePropertyConfigurationTests
    {
        [Fact]
        public void Ctor_does_not_invoke_delegate()
        {
            var initialized = false;

            new ConventionPrimitivePropertyConfiguration(
                new MockPropertyInfo(),
                () =>
                {
                    initialized = true;

                    return null;
                });

            Assert.False(initialized);
        }

        [Fact]
        public void Methods_dont_throw_if_configuration_is_null()
        {
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => null);

            config.HasColumnName("Column1");
            config.HasColumnOrder(0);
            config.HasColumnType("int");
            config.HasParameterName("Parameter1");
            config.HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            config.HasMaxLength(1);
            config.HasPrecision(1);
            config.HasPrecision(1, 1);
            config.IsConcurrencyToken(false);
            config.IsOptional();
            config.IsRequired();
            config.IsUnicode();
            config.IsVariableLength();
            config.IsFixedLength();
            config.IsMaxLength();
            config.IsRowVersion();
            config.IsKey();
            config.HasColumnAnnotation("Fox", "Like an angel in disguise");
        }

        [Fact]
        public void HasColumnName_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasColumnName("Column1");

            Assert.Equal("Column1", innerConfig.ColumnName);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasColumnName_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    ColumnName = "Column1"
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasColumnName("Column2");

            Assert.Equal("Column1", innerConfig.ColumnName);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasColumnName_evaluates_preconditions()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var ex = Assert.Throws<ArgumentException>(
                () => config.HasColumnName(""));

            Assert.Equal(Strings.ArgumentIsNullOrWhitespace("columnName"), ex.Message);
        }

        [Fact]
        public void HasAnnotation_configures_only_annotations_that_have_not_already_been_set()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            innerConfig.SetAnnotation("A1", "V1");
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasColumnAnnotation("A1", "V1B").HasColumnAnnotation("A2", "V2");

            Assert.Equal("V1", innerConfig.Annotations["A1"]);
            Assert.Equal("V2", innerConfig.Annotations["A2"]);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasAnnotation_evaluates_preconditions()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(() => config.HasColumnAnnotation(null, null)).Message);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(() => config.HasColumnAnnotation(" ", null)).Message);

            Assert.Equal(
                Strings.BadAnnotationName("Cheese:Pickle"),
                Assert.Throws<ArgumentException>(() => config.HasColumnAnnotation("Cheese:Pickle", null)).Message);
        }

        [Fact]
        public void HasParameterName_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasParameterName("Parameter1");

            Assert.Equal("Parameter1", innerConfig.ParameterName);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasParameterName_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    ParameterName = "Parameter1"
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasParameterName("Parameter2");

            Assert.Equal("Parameter1", innerConfig.ParameterName);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasParameterName_evaluates_preconditions()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var ex = Assert.Throws<ArgumentException>(
                () => config.HasParameterName(""));

            Assert.Equal(Strings.ArgumentIsNullOrWhitespace("parameterName"), ex.Message);
        }

        [Fact]
        public void HasColumnOrder_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasColumnOrder(1);

            Assert.Equal(1, innerConfig.ColumnOrder);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasColumnOrder_throws_on_negative_arguments()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
                "columnOrder",
                Assert.Throws<ArgumentOutOfRangeException>(() => config.HasColumnOrder(-1)).ParamName);
        }

        [Fact]
        public void HasColumnOrder_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    ColumnOrder = 1
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasColumnOrder(2);

            Assert.Equal(1, innerConfig.ColumnOrder);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasColumnType_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasColumnType("int");

            Assert.Equal("int", innerConfig.ColumnType);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasColumnType_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    ColumnType = "int"
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasColumnType("long");

            Assert.Equal("int", innerConfig.ColumnType);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasColumnType_evaluates_preconditions()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var ex = Assert.Throws<ArgumentException>(
                () => config.HasColumnType(""));

            Assert.Equal(Strings.ArgumentIsNullOrWhitespace("columnType"), ex.Message);
        }

        [Fact]
        public void IsConcurrencyToken_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsConcurrencyToken();

            Assert.Equal(ConcurrencyMode.Fixed, innerConfig.ConcurrencyMode);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsConcurrencyToken_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    ConcurrencyMode = ConcurrencyMode.None
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsConcurrencyToken();

            Assert.Equal(ConcurrencyMode.None, innerConfig.ConcurrencyMode);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsConcurrencyToken_with_parameter_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsConcurrencyToken(false);

            Assert.Equal(ConcurrencyMode.None, innerConfig.ConcurrencyMode);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsConcurrencyToken_with_parameter_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    ConcurrencyMode = ConcurrencyMode.Fixed
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsConcurrencyToken(false);

            Assert.Equal(ConcurrencyMode.Fixed, innerConfig.ConcurrencyMode);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasDatabaseGeneratedOption_evaluates_preconditions()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => config.HasDatabaseGeneratedOption((DatabaseGeneratedOption)(-1)));

            Assert.Equal("databaseGeneratedOption", ex.ParamName);
        }

        [Fact]
        public void HasDatabaseGeneratedOption_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);

            Assert.Equal(DatabaseGeneratedOption.Computed, innerConfig.DatabaseGeneratedOption);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasDatabaseGeneratedOption_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    DatabaseGeneratedOption = DatabaseGeneratedOption.Computed
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            Assert.Equal(DatabaseGeneratedOption.Computed, innerConfig.DatabaseGeneratedOption);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsOptional_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsOptional();

            Assert.Equal(true, innerConfig.IsNullable);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsOptional_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    IsNullable = false
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsOptional();

            Assert.Equal(false, innerConfig.IsNullable);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsOptional_throws_on_non_nullable_property()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(typeof(int), "IntProperty"), () => innerConfig);

            Assert.Equal(
                Strings.LightweightPrimitivePropertyConfiguration_NonNullableProperty("System.Object.IntProperty", typeof(int).Name),
                Assert.Throws<InvalidOperationException>(() => config.IsOptional()).Message);
        }

        [Fact]
        public void IsRequired_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsRequired();

            Assert.Equal(false, innerConfig.IsNullable);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsRequired_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    IsNullable = true
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsRequired();

            Assert.Equal(true, innerConfig.IsNullable);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsUnicode_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.StringPropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsUnicode();

            Assert.Equal(true, innerConfig.IsUnicode);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsUnicode_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.StringPropertyConfiguration
                {
                    IsUnicode = false
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsUnicode();

            Assert.Equal(false, innerConfig.IsUnicode);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsUnicode_throws_when_not_string()
        {
            var innerConfig = new Properties.Primitive.DateTimePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
               Strings.LightweightPrimitivePropertyConfiguration_IsUnicodeNonString("P"),
               Assert.Throws<InvalidOperationException>(() => config.IsUnicode()).Message);
        }

        [Fact]
        public void IsUnicode_with_parameter_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.StringPropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsUnicode(false);

            Assert.Equal(false, innerConfig.IsUnicode);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsUnicode_with_parameter_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.StringPropertyConfiguration
                {
                    IsUnicode = true
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsUnicode(false);

            Assert.Equal(true, innerConfig.IsUnicode);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsUnicode_with_parameter_throws_when_not_string()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var ex = Assert.Throws<InvalidOperationException>(() => config.IsUnicode(false));

            Assert.Equal(Strings.LightweightPrimitivePropertyConfiguration_IsUnicodeNonString("P"), ex.Message);
        }

        [Fact]
        public void IsFixedLength_configures_when_unset()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsFixedLength();

            Assert.Equal(true, innerConfig.IsFixedLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsFixedLength_is_noop_when_set()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            innerConfig.IsFixedLength = false;

            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsFixedLength();

            Assert.Equal(false, innerConfig.IsFixedLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsFixedLength_throws_when_not_length()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
               Strings.LightweightPrimitivePropertyConfiguration_NonLength("P"),
               Assert.Throws<InvalidOperationException>(() => config.IsFixedLength()).Message);
        }

        [Fact]
        public void IsVariableLength_configures_when_unset()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsVariableLength();

            Assert.Equal(false, innerConfig.IsFixedLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsVariableLength_is_noop_when_set()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            innerConfig.IsFixedLength = true;

            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsVariableLength();

            Assert.Equal(true, innerConfig.IsFixedLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsVariableLength_throws_when_not_length()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
               Strings.LightweightPrimitivePropertyConfiguration_NonLength("P"),
               Assert.Throws<InvalidOperationException>(() => config.IsVariableLength()).Message);
        }

        [Fact]
        public void HasMaxLength_configures_when_unset()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasMaxLength(256);

            Assert.Equal(256, innerConfig.MaxLength);
            Assert.Equal(false, innerConfig.IsFixedLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasMaxLength_does_not_configure_IsUnicode_when_unset()
        {
            var innerConfig = new Properties.Primitive.StringPropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasMaxLength(256);

            Assert.Equal(256, innerConfig.MaxLength);
            Assert.Equal(false, innerConfig.IsFixedLength);
            Assert.Equal(null, innerConfig.IsUnicode);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasMaxLength_does_not_configure_IsFixedLenth_when_set()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            innerConfig.IsFixedLength = true;

            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasMaxLength(256);

            Assert.Equal(256, innerConfig.MaxLength);
            Assert.Equal(true, innerConfig.IsFixedLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasMaxLength_does_not_configure_IsUnicode_when_set()
        {
            var innerConfig = new Properties.Primitive.StringPropertyConfiguration
                {
                    IsUnicode = false
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasMaxLength(256);

            Assert.Equal(256, innerConfig.MaxLength);
            Assert.Equal(false, innerConfig.IsFixedLength);
            Assert.Equal(false, innerConfig.IsUnicode);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasMaxLength_is_noop_when_set()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            innerConfig.MaxLength = 256;

            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasMaxLength(128);

            Assert.Equal(256, innerConfig.MaxLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasMaxLength_is_noop_when_IsMaxLength_set()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            innerConfig.IsMaxLength = true;

            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasMaxLength(256);

            Assert.Null(innerConfig.MaxLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasMaxLength_throws_when_not_length()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
               Strings.LightweightPrimitivePropertyConfiguration_NonLength("P"),
               Assert.Throws<InvalidOperationException>(() => config.HasMaxLength(256)).Message);
        }

        [Fact]
        public void HasMaxLength_evaluates_preconditions()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => config.HasMaxLength(0));

            Assert.Equal("maxLength", ex.ParamName);
        }

        [Fact]
        public void IsMaxLength_configures_when_unset()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsMaxLength();

            Assert.Equal(true, innerConfig.IsMaxLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsMaxLength_is_noop_when_set()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            innerConfig.IsMaxLength = false;

            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsMaxLength();

            Assert.Equal(false, innerConfig.IsMaxLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsMaxLength_is_noop_when_MaxLength_set()
        {
            var innerConfig = new Mock<Properties.Primitive.LengthPropertyConfiguration>().Object;
            innerConfig.MaxLength = 256;

            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsMaxLength();

            Assert.Null(innerConfig.IsMaxLength);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsMaxLength_throws_when_not_length()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
               Strings.LightweightPrimitivePropertyConfiguration_NonLength("P"),
               Assert.Throws<InvalidOperationException>(() => config.IsMaxLength()).Message);
        }

        [Fact]
        public void HasPrecision_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.DateTimePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasPrecision(8);

            Assert.Equal<byte?>(8, innerConfig.Precision);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasPrecision_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.DateTimePropertyConfiguration
                {
                    Precision = 8
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasPrecision(7);

            Assert.Equal<byte?>(8, innerConfig.Precision);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasPrecision_throws_when_not_DateTime()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
               Strings.LightweightPrimitivePropertyConfiguration_HasPrecisionNonDateTime("P"),
               Assert.Throws<InvalidOperationException>(() => config.HasPrecision(8)).Message);
        }

        [Fact]
        public void HasPrecision_throws_when_Decimal()
        {
            var innerConfig = new Properties.Primitive.DecimalPropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
                Strings.LightweightPrimitivePropertyConfiguration_DecimalNoScale("P"),
                Assert.Throws<InvalidOperationException>(() => config.HasPrecision(8)).Message);
        }

        [Fact]
        public void HasPrecision_with_scale_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.DecimalPropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasPrecision(8, 2);

            Assert.Equal<byte?>(8, innerConfig.Precision);
            Assert.Equal<byte?>(2, innerConfig.Scale);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasPrecision_with_scale_is_noop_when_precision_set()
        {
            var innerConfig = new Properties.Primitive.DecimalPropertyConfiguration
                {
                    Precision = 8
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasPrecision(7, 1);

            Assert.Equal<byte?>(8, innerConfig.Precision);
            Assert.Null(innerConfig.Scale);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasPrecision_with_scale_is_noop_when_scale_set()
        {
            var innerConfig = new Properties.Primitive.DecimalPropertyConfiguration
                {
                    Scale = 2
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.HasPrecision(7, 1);

            Assert.Null(innerConfig.Precision);
            Assert.Equal<byte?>(2, innerConfig.Scale);
            Assert.Same(config, result);
        }

        [Fact]
        public void HasPrecision_with_scale_throws_when_not_decimal()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
                Strings.LightweightPrimitivePropertyConfiguration_HasPrecisionNonDecimal("P"),
                Assert.Throws<InvalidOperationException>(() => config.HasPrecision(8, 2)).Message);
        }

        [Fact]
        public void HasPrecision_with_scale_throws_on_DateTime()
        {
            var innerConfig = new Properties.Primitive.DateTimePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
                Strings.LightweightPrimitivePropertyConfiguration_DateTimeScale("P"),
                Assert.Throws<InvalidOperationException>(() => config.HasPrecision(8, 2)).Message);
        }

        [Fact]
        public void IsRowVersion_configures_when_unset()
        {
            var innerConfig = new Properties.Primitive.BinaryPropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsRowVersion();

            Assert.Equal(true, innerConfig.IsRowVersion);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsRowVersion_is_noop_when_set()
        {
            var innerConfig = new Properties.Primitive.BinaryPropertyConfiguration
                {
                    IsRowVersion = false
                };
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            var result = config.IsRowVersion();

            Assert.Equal(false, innerConfig.IsRowVersion);
            Assert.Same(config, result);
        }

        [Fact]
        public void IsRowVersion_throws_when_not_binary()
        {
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(new MockPropertyInfo(), () => innerConfig);

            Assert.Equal(
               Strings.LightweightPrimitivePropertyConfiguration_IsRowVersionNonBinary("P"),
               Assert.Throws<InvalidOperationException>(() => config.IsRowVersion()).Message);
        }

        [Fact]
        public void IsKey_configures_when_unset()
        {
            var typeConfig = new EntityTypeConfiguration(typeof(AType1));
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    TypeConfiguration = typeConfig
                };
            var propertyInfo = typeof(AType1).GetDeclaredProperty("Property1");
            var config = new ConventionPrimitivePropertyConfiguration(propertyInfo, () => innerConfig);

            var result = config.IsKey();

            Assert.Equal(1, typeConfig.KeyProperties.Count());
            Assert.Contains(propertyInfo, typeConfig.KeyProperties);
            Assert.Same(config, result);
        }

        public class AType1
        {
            public int Property1 { get; set; }
        }

        [Fact]
        public void IsKey_is_noop_when_set()
        {
            var typeConfig = new EntityTypeConfiguration(typeof(AType2));
            typeConfig.Key(new[] { typeof(AType2).GetDeclaredProperty("Property1") });
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration
                {
                    TypeConfiguration = typeConfig
                };
            var propertyInfo = typeof(AType2).GetDeclaredProperty("Property2");
            var config = new ConventionPrimitivePropertyConfiguration(propertyInfo, () => innerConfig);

            var result = config.IsKey();

            Assert.DoesNotContain(propertyInfo, typeConfig.KeyProperties);
            Assert.Same(config, result);
        }

        public class AType2
        {
            public int Property1 { get; set; }
            public int Property2 { get; set; }
        }

        [Fact]
        public void ClrPropertyInfo_returns_propertyInfo()
        {
            var propertyInfo = new MockPropertyInfo();
            var innerConfig = new Properties.Primitive.PrimitivePropertyConfiguration();
            var config = new ConventionPrimitivePropertyConfiguration(propertyInfo, () => innerConfig);

            Assert.Same(propertyInfo.Object, config.ClrPropertyInfo);
        }
    }
}
