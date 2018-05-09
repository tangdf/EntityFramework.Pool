﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Core.Objects.ELinq
{
    using Xunit;

    public class InitializerMetadataTests
    {
        [Fact]
        public void MethodInfo_fields_are_initialized()
        {
            Assert.NotNull(InitializerMetadata.EntityCollectionInitializerMetadata.CreateEntityCollectionMethod);
        }
    }
}
