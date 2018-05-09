// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

[assembly:
    InternalsVisibleTo(
        "Microsoft.Data.Entity.Design.VersioningFacade"
        )]

#if !INTERNALS_INVISIBLE

[assembly:
    InternalsVisibleTo(
        "EntityFramework.UnitTests"
        )]
[assembly:
    InternalsVisibleTo(
        "EntityFramework.FunctionalTests.Transitional"
        )]
[assembly:
    InternalsVisibleTo(
        "EFDesigner.UnitTests"
        )]

// for Moq

[assembly:
    InternalsVisibleTo(
        "DynamicProxyGenAssembly2"
        )]

#endif
