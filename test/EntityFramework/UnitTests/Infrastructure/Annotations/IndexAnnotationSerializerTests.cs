﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.Annotations
{
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Resources;
    using System.Linq;
    using Xunit;

    public class IndexAnnotationSerializerTests
    {
        [Fact]
        public void Annotations_containing_single_indexes_are_serialized_to_expected_format()
        {
            Assert.Equal(
                "{ }",
                Serialize(new IndexAttribute()));

            Assert.Equal(
                "{ Name: EekyBear }",
                Serialize(new IndexAttribute("EekyBear")));

            Assert.Equal(
                "{ Name: EekyBear, Order: 7 }",
                Serialize(new IndexAttribute("EekyBear", 7)));

            Assert.Equal(
                "{ Order: 8 }",
                Serialize(new IndexAttribute { Order = 8 }));

            Assert.Equal(
                "{ IsClustered: True }",
                Serialize(new IndexAttribute { IsClustered = true }));

            Assert.Equal(
                "{ IsUnique: True }",
                Serialize(new IndexAttribute { IsUnique = true }));

            Assert.Equal(
                "{ Name: EekyBear, Order: 7, IsClustered: False, IsUnique: False }",
                Serialize(new IndexAttribute("EekyBear", 7) { IsClustered = false, IsUnique = false }));
        }

        [Fact]
        public void Can_roundtrip_index_names_having_special_characters()
        {
            Assert.Equal(
                "{ Name: \"\\,'<>[]\\,'\\} }{ Name: \"\\,'<foo>[]\\,'\\} }",
                Serialize(Deserialize("{Name:\"\\,'<>[]\\,'\\}}{Name:\"\\,'<foo>[]\\,'\\}}")));

            Assert.Equal(
                "{ Name: \"\\,'<>[]\\,'\\}, Order: 42 }{ Name: \"\\,'<foo>[]\\,'\\}, Order: 42 }",
                Serialize(Deserialize("{Name:\"\\,'<>[]\\,'\\},Order: 42}  { Name: \"\\,'<foo>[]\\,'\\},  Order: 42 }")));

            var indexAnnotation1 = new IndexAnnotation(new IndexAttribute("\",'<>[],'"));

            var indexAnnotation2 = Deserialize(Serialize(indexAnnotation1));

            Assert.NotSame(indexAnnotation1, indexAnnotation2);
            Assert.Equal("\",'<>[],'", indexAnnotation2.Indexes.Single().Name);

            indexAnnotation1 = new IndexAnnotation(new IndexAttribute("\",'<>[],'", 42));

            indexAnnotation2 = Deserialize(Serialize(indexAnnotation1));

            Assert.NotSame(indexAnnotation1, indexAnnotation2);
            Assert.Equal("\",'<>[],'", indexAnnotation2.Indexes.Single().Name);
            Assert.Equal(42, indexAnnotation2.Indexes.Single().Order);
            
            indexAnnotation1 = new IndexAnnotation(new IndexAttribute("\",'<>[]',"));

            indexAnnotation2 = Deserialize(Serialize(indexAnnotation1));

            Assert.NotSame(indexAnnotation1, indexAnnotation2);
            Assert.Equal("\",'<>[]',", indexAnnotation2.Indexes.Single().Name);

            indexAnnotation1 = new IndexAnnotation(new IndexAttribute("\",'<>[]',", 42));

            indexAnnotation2 = Deserialize(Serialize(indexAnnotation1));

            Assert.NotSame(indexAnnotation1, indexAnnotation2);
            Assert.Equal("\",'<>[]',", indexAnnotation2.Indexes.Single().Name);
            Assert.Equal(42, indexAnnotation2.Indexes.Single().Order);
        }

        [Fact]
        public void Annotations_containing_multiple_indexes_are_serialized_to_expected_format()
        {
            Assert.Equal(
                "{ }"
                + "{ Name: MrsPandy }"
                + "{ Name: EekyBear, Order: 7 }"
                + "{ Name: Splash, Order: 8 }"
                + "{ Name: Tarquin, IsClustered: False }"
                + "{ Name: MrsKoalie, IsUnique: False }"
                + "{ Name: EekyJnr, Order: 7, IsClustered: True, IsUnique: True }",
                Serialize(
                    new IndexAttribute(),
                    new IndexAttribute("MrsPandy"),
                    new IndexAttribute("EekyBear", 7),
                    new IndexAttribute("Splash") { Order = 8 },
                    new IndexAttribute("Tarquin") { IsClustered = false },
                    new IndexAttribute("MrsKoalie") { IsUnique = false },
                    new IndexAttribute("EekyJnr", 7) { IsClustered = true, IsUnique = true }));
        }

        [Fact]
        public void SerializeValue_checks_its_arguments()
        {
            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(
                    () => new IndexAnnotationSerializer().Serialize(null, new IndexAnnotation(new IndexAttribute()))).Message);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(
                    () => new IndexAnnotationSerializer().Serialize(" ", new IndexAnnotation(new IndexAttribute()))).Message);

            Assert.Equal(
                "value",
                Assert.Throws<ArgumentNullException>(
                    () => new IndexAnnotationSerializer().Serialize("Index", null)).ParamName);

            Assert.Equal(
                Strings.AnnotationSerializeWrongType("Random", "IndexAnnotationSerializer", "IndexAnnotation"),
                Assert.Throws<ArgumentException>(
                    () => new IndexAnnotationSerializer().Serialize("Index", new Random())).Message);
        }

        private static string Serialize(params IndexAttribute[] indexAttributes)
        {
            return new IndexAnnotationSerializer().Serialize("Index", new IndexAnnotation(indexAttributes));
        }

        [Fact]
        public void Strings_containing_single_indexes_are_deserialized_to_expected_annotation()
        {
            Assert.Equal(
                "{ }",
                Serialize(Deserialize("{ }")));

            Assert.Equal(
                "{ Name: EekyBear }",
                Serialize(Deserialize("{ Name: EekyBear }")));

            Assert.Equal(
                "{ Name: EekyBear, Order: 7 }",
                Serialize(Deserialize("{ Name: EekyBear, Order: 7 }")));

            Assert.Equal(
                "{ Order: 8 }",
                Serialize(Deserialize("{ Order: 8 }")));

            Assert.Equal(
                "{ IsClustered: True }",
                Serialize(Deserialize("{ IsClustered: True }")));

            Assert.Equal(
                "{ IsUnique: True }",
                Serialize(Deserialize("{ IsUnique: True }")));

            Assert.Equal(
                "{ Name: EekyBear, Order: 7, IsClustered: False, IsUnique: False }",
                Serialize(Deserialize("{ Name: EekyBear, Order: 7, IsClustered: False, IsUnique: False }")));

            Assert.Equal(
                "{ Name: EekyBear, Order: 7, IsClustered: False, IsUnique: False }",
                Serialize(Deserialize(" {  Name:  EekyBear ,  Order:  7 ,  IsClustered:  False ,  IsUnique:  False  } ")));
        }

        [Fact]
        public void Strings_containing_multiple_indexes_are_deserialized_to_expected_annotation()
        {
            Assert.Equal(
                "{ }"
                + "{ Name: MrsPandy }"
                + "{ Name: EekyBear, Order: 7 }"
                + "{ Name: Splash, Order: 8 }"
                + "{ Name: Tarquin, IsClustered: False }"
                + "{ Name: MrsKoalie, IsUnique: False }"
                + "{ Name: EekyJnr, Order: 7, IsClustered: True, IsUnique: True }",
                Serialize(
                    Deserialize(
                        "{ }"
                        + "{ Name: MrsPandy }"
                        + "{ Name: EekyBear, Order: 7 }"
                        + "{ Name: Splash, Order: 8 }"
                        + "{ Name: Tarquin, IsClustered: False }"
                        + "{ Name: MrsKoalie, IsUnique: False }"
                        + "{ Name: EekyJnr, Order: 7, IsClustered: True, IsUnique: True }")));

            Assert.Equal(
                "{ }"
                + "{ Name: MrsPandy }"
                + "{ Name: EekyBear, Order: 7 }"
                + "{ Name: Splash, Order: 88 }"
                + "{ Name: Tarquin, IsClustered: False }"
                + "{ Name: MrsKoalie, IsUnique: False }"
                + "{ Name: EekyJnr, Order: 7, IsClustered: True, IsUnique: True }",
                Serialize(
                    Deserialize(
                        " {} "
                        + " { Name: MrsPandy } "
                        + " { Name: EekyBear, Order: 7 } "
                        + " { Name: Splash, Order: 88 } "
                        + " { Name: Tarquin, IsClustered: False } "
                        + " { Name: MrsKoalie, IsUnique: False } "
                        + " { Name: EekyJnr, Order: 7, IsClustered: True, IsUnique: True } ")));
        }

        [Fact]
        public void DeserializeValue_checks_its_arguments()
        {
            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(
                    () => new IndexAnnotationSerializer().Deserialize(null, "{}")).Message);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("name"),
                Assert.Throws<ArgumentException>(
                    () => new IndexAnnotationSerializer().Deserialize(" ", "{}")).Message);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("value"),
                Assert.Throws<ArgumentException>(
                    () => new IndexAnnotationSerializer().Deserialize("Index", null)).Message);

            Assert.Equal(
                Strings.ArgumentIsNullOrWhitespace("value"),
                Assert.Throws<ArgumentException>(
                    () => new IndexAnnotationSerializer().Deserialize("Index", " ")).Message);
        }

        [Fact]
        public void DeserializeValue_throws_on_invalid_formats()
        {
            TestBadDeserialize("{ Name: }");
            TestBadDeserialize("{ Name: 'EekyBear', Name: 'EekyBear' }");
            TestBadDeserialize("{ Order: 7, Order: 7 }");
            TestBadDeserialize("{ IsClustered: True, IsClustered: True }");
            TestBadDeserialize("{ IsUnique: True, IsUnique: True }");
            TestBadDeserialize("{ Order: 7a7 }");
            TestBadDeserialize("{ Name: Eeky,Bear }");
            TestBadDeserialize("{ Name: Eeky} {Bear }");
            TestBadDeserialize("{ Order: }");
            TestBadDeserialize("{ IsClustered:");
            TestBadDeserialize("{ IsUnique:}");
            TestBadDeserialize("{ Order: 9876543210 }");
        }

        private static void TestBadDeserialize(string value)
        {
            Assert.Equal(
                Strings.AnnotationSerializeBadFormat(value, "IndexAnnotationSerializer", IndexAnnotationSerializer.FormatExample),
                Assert.Throws<FormatException>(
                    () => new IndexAnnotationSerializer().Deserialize("Index", value)).Message);
        }

        private static IndexAnnotation Deserialize(string annotation)
        {
            return (IndexAnnotation)new IndexAnnotationSerializer().Deserialize("Index", annotation);
        }

        private static string Serialize(IndexAnnotation annotation)
        {
            return new IndexAnnotationSerializer().Serialize("Index", annotation);
        }
    }
}
