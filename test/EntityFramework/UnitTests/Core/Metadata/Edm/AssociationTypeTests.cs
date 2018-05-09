﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Metadata.Edm
{
    using System.Linq;
    using Xunit;

    public class AssociationTypeTests
    {
        [Fact]
        public void Can_get_and_set_ends_via_wrapper_properties()
        {
            var associationType = new AssociationType("A", XmlConstants.ModelNamespace_3, false, DataSpace.CSpace);

            Assert.Null(associationType.SourceEnd);
            Assert.Null(associationType.TargetEnd);

            var sourceEnd = new AssociationEndMember("S", new EntityType("E", "N", DataSpace.CSpace));

            associationType.SourceEnd = sourceEnd;

            var targetEnd = new AssociationEndMember("T", new EntityType("E", "N", DataSpace.CSpace));

            associationType.TargetEnd = targetEnd;

            Assert.Same(sourceEnd, associationType.SourceEnd);
            Assert.Same(targetEnd, associationType.TargetEnd);
        }

        [Fact]
        public void Can_get_and_set_constraint_via_wrapper_property()
        {
            var associationType
                = new AssociationType("A", XmlConstants.ModelNamespace_3, false, DataSpace.CSpace)
                      {
                          SourceEnd = new AssociationEndMember("S", new EntityType("E", "N", DataSpace.CSpace)),
                          TargetEnd = new AssociationEndMember("T", new EntityType("E", "N", DataSpace.CSpace))
                      };

            Assert.Null(associationType.Constraint);
            Assert.False(associationType.IsForeignKey);

            var property
                = EdmProperty.CreatePrimitive("Fk", PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String));

            var referentialConstraint
                = new ReferentialConstraint(
                    associationType.SourceEnd,
                    associationType.TargetEnd,
                    new[] { property },
                    new[] { property });

            associationType.Constraint = referentialConstraint;

            Assert.Same(referentialConstraint, associationType.Constraint);
            Assert.True(associationType.IsForeignKey);
        }

        [Fact]
        public void AssociationEndMembers_returns_correct_ends_after_modifying_SourceEnd()
        {
            var associationType
                = new AssociationType("A", XmlConstants.ModelNamespace_3, false, DataSpace.CSpace)
                {
                    SourceEnd = new AssociationEndMember("S", new EntityType("E", "N", DataSpace.CSpace)),
                    TargetEnd = new AssociationEndMember("T", new EntityType("E", "N", DataSpace.CSpace))
                };

            var newSource = new AssociationEndMember("S1", new EntityType("E", "N", DataSpace.CSpace));
            associationType.SourceEnd = newSource;
            associationType.SetReadOnly();

            Assert.Same(associationType.SourceEnd, newSource);
            Assert.Same(associationType.AssociationEndMembers[0], newSource);
        }

        [Fact]
        public void AssociationEndMembers_returns_correct_ends_after_modifying_TargetEnd()
        {
            var associationType
                = new AssociationType("A", XmlConstants.ModelNamespace_3, false, DataSpace.CSpace)
                {
                    SourceEnd = new AssociationEndMember("S", new EntityType("E", "N", DataSpace.CSpace)),
                    TargetEnd = new AssociationEndMember("T", new EntityType("E", "N", DataSpace.CSpace))
                };

            var newTarget = new AssociationEndMember("T1", new EntityType("E", "N", DataSpace.CSpace));
            associationType.TargetEnd = newTarget;
            associationType.SetReadOnly();

            Assert.Same(associationType.TargetEnd, newTarget);
            Assert.Same(associationType.AssociationEndMembers[1], newTarget);
        }

        [Fact]
        public void Create_throws_argument_exception_when_called_with_invalid_arguments()
        {
            var source = new EntityType("Source", "Namespace", DataSpace.CSpace);
            var target = new EntityType("Target", "Namespace", DataSpace.CSpace);
            var sourceEnd = new AssociationEndMember("SourceEnd", source);
            var targetEnd = new AssociationEndMember("TargetEnd", target);
            var constraint =
                new ReferentialConstraint(
                    sourceEnd,
                    targetEnd,
                    new[] { new EdmProperty("SourceProperty") },
                    new[] { new EdmProperty("TargetProperty") });
            var metadataProperty =
                new MetadataProperty(
                    "MetadataProperty",
                    TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)),
                    "value");

            Assert.Throws<ArgumentException>(
                () => AssociationType.Create(
                    null,
                    "Namespace",
                    true,
                    DataSpace.CSpace,
                    sourceEnd,
                    targetEnd,
                    constraint,
                    new[] { metadataProperty }));

            Assert.Throws<ArgumentException>(
                () => AssociationType.Create(
                    String.Empty,
                    "Namespace",
                    true,
                    DataSpace.CSpace,
                    sourceEnd,
                    targetEnd,
                    constraint,
                    new[] { metadataProperty }));

            Assert.Throws<ArgumentException>(
                () => AssociationType.Create(
                    "AssociationType",
                    null,
                    true,
                    DataSpace.CSpace,
                    sourceEnd,
                    targetEnd,
                    constraint,
                    new[] { metadataProperty }));

            Assert.Throws<ArgumentException>(
                () => AssociationType.Create(
                    "AssociationType",
                    String.Empty,
                    true,
                    DataSpace.CSpace,
                    sourceEnd,
                    targetEnd,
                    constraint,
                    new[] { metadataProperty }));
        }

        [Fact]
        public void Create_sets_properties_and_seals_the_instance()
        {
            var source = new EntityType("Source", "Namespace", DataSpace.CSpace);
            var target = new EntityType("Target", "Namespace", DataSpace.CSpace);
            var sourceEnd = new AssociationEndMember("SourceEnd", source);
            var targetEnd = new AssociationEndMember("TargetEnd", target);
            var constraint = 
                new ReferentialConstraint(
                    sourceEnd,
                    targetEnd,
                    new[] { new EdmProperty("SourceProperty") },
                    new[] { new EdmProperty("TargetProperty") });
            var metadataProperty = 
                new MetadataProperty(
                    "MetadataProperty",
                    TypeUsage.CreateDefaultTypeUsage(PrimitiveType.GetEdmPrimitiveType(PrimitiveTypeKind.String)),
                    "value");
            var associationType = 
                AssociationType.Create(
                    "AssociationType",
                    "Namespace",
                    true,
                    DataSpace.CSpace,
                    sourceEnd,
                    targetEnd,
                    constraint,
                    new[] { metadataProperty });

            Assert.Equal("Namespace.AssociationType", associationType.FullName);
            Assert.Equal(true, associationType.IsForeignKey);
            Assert.Equal(DataSpace.CSpace, associationType.DataSpace);
            Assert.Same(sourceEnd, associationType.SourceEnd);
            Assert.Same(targetEnd, associationType.TargetEnd);
            Assert.Same(constraint, associationType.Constraint);
            Assert.Same(metadataProperty, associationType.MetadataProperties.SingleOrDefault(p => p.Name == "MetadataProperty"));
            Assert.True(associationType.IsReadOnly);
        }

        [Fact]
        public void Members_are_updated_when_KeyMembers_are_set()
        {
            var source = new EntityType("S", "N", DataSpace.CSpace);
            var target = new EntityType("T", "N", DataSpace.CSpace);
            var sourceEnd1 = new AssociationEndMember("SE1", source);
            var sourceEnd2 = new AssociationEndMember("SE2", source);
            var targetEnd1 = new AssociationEndMember("TE1", target);
            var targetEnd2 = new AssociationEndMember("TE2", target);

            var associationType = new AssociationType("AT", "N", true, DataSpace.CSpace);
            associationType.SourceEnd = sourceEnd1;
            associationType.TargetEnd = targetEnd1;

            Assert.True(associationType.Members.Contains(sourceEnd1));
            Assert.True(associationType.Members.Contains(targetEnd1));
            Assert.False(associationType.Members.Contains(sourceEnd2));
            Assert.False(associationType.Members.Contains(targetEnd2));

            associationType.SourceEnd = sourceEnd2;
            associationType.TargetEnd = targetEnd2;

            Assert.False(associationType.Members.Contains(sourceEnd1));
            Assert.False(associationType.Members.Contains(targetEnd1));
            Assert.True(associationType.Members.Contains(sourceEnd2));
            Assert.True(associationType.Members.Contains(targetEnd2));
        }
    }
}
