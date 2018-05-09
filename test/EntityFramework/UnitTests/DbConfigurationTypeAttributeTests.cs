// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity
{
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class DbConfigurationTypeAttributeTests
    {
        [Fact]
        public void Attribute_can_be_created_using_type()
        {
            Assert.Same(
                typeof(FakeDbConfiguration),
                typeof(ContextWithTypeAttribute).GetCustomAttributes<DbConfigurationTypeAttribute>(inherit: true)
                    .Single()
                    .ConfigurationType);
        }

        [DbConfigurationType(typeof(FakeDbConfiguration))]
        public class ContextWithTypeAttribute : DbContext
        {
        }

        public class FakeDbConfiguration : DbConfiguration
        {
        }

        [Fact]
        public void Attribute_can_be_created_using_type_name()
        {
            Assert.Same(
                typeof(FakeDbConfiguration),
                typeof(ContextWithStringAttribute).GetCustomAttributes<DbConfigurationTypeAttribute>(inherit: true)
                    .Single()
                    .ConfigurationType);
        }

        [DbConfigurationType("System.Data.Entity.DbConfigurationTypeAttributeTests+FakeDbConfiguration, EntityFramework.UnitTests")]
        public class ContextWithStringAttribute : DbContext
        {
        }

        [Fact]
        public void Attribute_throws_if_type_is_null()
        {
            Assert.Equal(
                "configurationType",
                Assert.Throws<ArgumentNullException>(
                    () => typeof(ContextWithNullTypeAttribute).GetCustomAttributes<DbConfigurationTypeAttribute>(inherit: true)).ParamName);
        }

        [DbConfigurationType((Type)null)]
        public class ContextWithNullTypeAttribute : DbContext
        {
        }

        [Fact]
        public void Attribute_throws_if_type_name_is_null_or_whitespace()
        {
            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("configurationTypeName"),
                Assert.Throws<ArgumentException>(
                    () => typeof(ContextWithNullStringAttribute).GetCustomAttributes<DbConfigurationTypeAttribute>(inherit: true)).Message);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("configurationTypeName"),
                Assert.Throws<ArgumentException>(
                    () => typeof(ContextWithEmptyStringAttribute).GetCustomAttributes<DbConfigurationTypeAttribute>(inherit: true)).Message);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("configurationTypeName"),
                Assert.Throws<ArgumentException>(
                    () => typeof(ContextWithWhitespaceStringAttribute).GetCustomAttributes<DbConfigurationTypeAttribute>(inherit: true))
                    .Message);
        }

        [DbConfigurationType((string)null)]
        public class ContextWithNullStringAttribute : DbContext
        {
        }

        [DbConfigurationType("")]
        public class ContextWithEmptyStringAttribute : DbContext
        {
        }

        [DbConfigurationType(" ")]
        public class ContextWithWhitespaceStringAttribute : DbContext
        {
        }

        [Fact]
        public void Attribute_throws_if_type_name_cannot_be_loaded()
        {
            Assert.Equal(
                Strings.DbConfigurationTypeInAttributeNotFound("Pretty.Vacant"),
                Assert.Throws<InvalidOperationException>(
                    () => typeof(ContextWithBadAttribute).GetCustomAttributes<DbConfigurationTypeAttribute>(inherit: true)).Message);
        }

        [DbConfigurationType("Pretty.Vacant")]
        public class ContextWithBadAttribute : DbContext
        {
        }
    }
}
