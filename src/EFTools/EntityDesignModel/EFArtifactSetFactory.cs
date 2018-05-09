﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model
{
    internal class EFArtifactSetFactory : IEFArtifactSetFactory
    {
        public EFArtifactSet CreateArtifactSet(EFArtifact artifact)
        {
            return new EntityDesignArtifactSet(artifact);
        }
    }
}
