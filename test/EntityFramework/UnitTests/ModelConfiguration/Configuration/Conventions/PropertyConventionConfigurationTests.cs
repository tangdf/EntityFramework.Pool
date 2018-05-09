﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.ModelConfiguration.Configuration
{
    using System.Data.Entity.ModelConfiguration.Configuration.Properties.Primitive;
    using System.Data.Entity.ModelConfiguration.Conventions;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class PropertyConventionConfigurationTests
    {
        [Fact]
        public void Where_evaluates_preconditions()
        {
            var conventions = new ConventionsConfiguration();
            var properties = new PropertyConventionConfiguration(conventions);

            var ex = Assert.Throws<ArgumentNullException>(
                () => properties.Where(null));
            Assert.Equal("predicate", ex.ParamName);
        }

        [Fact]
        public void Where_configures_predicates()
        {
            Func<PropertyInfo, bool> predicate1 = p => true;
            Func<PropertyInfo, bool> predicate2 = p => false;
            var conventions = new ConventionsConfiguration();
            var properties = new PropertyConventionConfiguration(conventions);

            var config = properties
                .Where(predicate1)
                .Where(predicate2);

            Assert.NotNull(config);
            Assert.Same(conventions, config.ConventionsConfiguration);
            Assert.Equal(2, config.Predicates.Count());
            Assert.Same(predicate2, config.Predicates.Last());
        }

        [Fact]
        public void Configure_evaluates_preconditions()
        {
            var conventions = new ConventionsConfiguration();
            var properties = new PropertyConventionConfiguration(conventions);

            var ex = Assert.Throws<ArgumentNullException>(
                () => properties.Configure(null));
            Assert.Equal("propertyConfigurationAction", ex.ParamName);
        }

        [Fact]
        public void Configure_adds_convention()
        {
            Func<PropertyInfo, bool> predicate = p => true;
            Action<ConventionPrimitivePropertyConfiguration> configurationAction = c => { };
            var conventions = new ConventionsConfiguration();
            var properties = new PropertyConventionConfiguration(conventions);

            Assert.Equal(16, conventions.ConfigurationConventions.Count());

            properties
                .Where(predicate)
                .Configure(configurationAction);

            Assert.Equal(17, conventions.ConfigurationConventions.Count());

            var convention = (PropertyConvention)conventions.ConfigurationConventions.First();
            Assert.Equal(1, convention.Predicates.Count());
            Assert.Same(predicate, convention.Predicates.Single());
            Assert.Same(configurationAction, convention.PropertyConfigurationAction);
        }

        [Fact]
        public void Having_evaluates_preconditions()
        {
            var conventions = new ConventionsConfiguration();
            var properties = new PropertyConventionConfiguration(conventions);

            var ex = Assert.Throws<ArgumentNullException>(
                () => properties.Having<object>(null));
            Assert.Equal("capturingPredicate", ex.ParamName);
        }

        [Fact]
        public void Having_configures_capturing_predicate()
        {
            var conventions = new ConventionsConfiguration();
            var properties = new PropertyConventionConfiguration(conventions);
            Func<PropertyInfo, bool> predicate = p => true;
            Func<PropertyInfo, object> capturingPredicate = p => null;

            var config = properties
                .Where(predicate)
                .Having(capturingPredicate);

            Assert.NotNull(config);
            Assert.Same(conventions, config.ConventionsConfiguration);
            Assert.Equal(1, config.Predicates.Count());
            Assert.Same(predicate, config.Predicates.Single());
            Assert.Same(capturingPredicate, config.CapturingPredicate);
        }
    }
}
