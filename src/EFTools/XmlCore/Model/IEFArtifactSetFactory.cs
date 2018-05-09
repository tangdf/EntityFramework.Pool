﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Model
{
    internal interface IEFArtifactSetFactory
    {
        EFArtifactSet CreateArtifactSet(EFArtifact artifact);
    }
}
