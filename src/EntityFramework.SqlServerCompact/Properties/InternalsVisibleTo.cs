// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

#if !INTERNALS_INVISIBLE

using System.Runtime.CompilerServices;

[assembly:
    InternalsVisibleTo(
        "EntityFramework.UnitTests"
        )]
[assembly:
    InternalsVisibleTo(
        "EntityFramework.FunctionalTests"
        )]

// for Moq

[assembly:
    InternalsVisibleTo(
        "DynamicProxyGenAssembly2"
        )]

#endif
