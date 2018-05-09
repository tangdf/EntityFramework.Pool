// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure.DependencyResolution
{
    using System.Collections.Concurrent;
    using System.Linq;
    using Moq;
    using Xunit;

    public class CachingDependencyResolverTests : TestBase
    {
        public interface IPilkington
        {
        }

        public interface IGervais
        {
        }

        public class GetService : TestBase
        {
            [Fact]
            public void GetService_caches_the_result_from_the_underlying_resolver_for_a_given_type_name_pair()
            {
                var mockResolver = new Mock<IDbDependencyResolver>();
                var cachingResolver = new CachingDependencyResolver(mockResolver.Object);

                VerifyServiceIsCached(new Mock<IPilkington>().Object, cachingResolver, mockResolver, "Foo");
                VerifyServiceIsCached(new Mock<IPilkington>().Object, cachingResolver, mockResolver, "Bar");
                VerifyServiceIsCached(new Mock<IPilkington>().Object, cachingResolver, mockResolver, null);

                VerifyServiceIsCached(new Mock<IGervais>().Object, cachingResolver, mockResolver, "Foo");
                VerifyServiceIsCached(new Mock<IGervais>().Object, cachingResolver, mockResolver, "Bar");
                VerifyServiceIsCached(new Mock<IGervais>().Object, cachingResolver, mockResolver, null);
            }

            private static void VerifyServiceIsCached<T>(
                T service,
                CachingDependencyResolver cachingResolver,
                Mock<IDbDependencyResolver> mockResolver,
                string name)
            {
                mockResolver.Setup(m => m.GetService(typeof(T), name)).Returns(service);

                Assert.Same(service, cachingResolver.GetService<T>(name));
                mockResolver.Verify(m => m.GetService(typeof(T), name), Times.Once());
                Assert.Same(service, cachingResolver.GetService<T>(name));
                mockResolver.Verify(m => m.GetService(typeof(T), name), Times.Once()); // Underlying resolver not called again
            }

            [Fact]
            public void A_service_that_resolves_to_null_is_still_cached()
            {
                var mockResolver = new Mock<IDbDependencyResolver>();

                VerifyServiceIsCached<IPilkington>(null, new CachingDependencyResolver(mockResolver.Object), mockResolver, null);
            }

            /// <summary>
            /// This test makes calls from multiple threads such that we have at least some chance of finding threading
            /// issues. As with any test of this type just because the test passes does not mean that the code is
            /// correct. On the other hand if this test ever fails (EVEN ONCE) then we know there is a problem to
            /// be investigated. DON'T just re-run and think things are okay if the test then passes.
            /// </summary>
            [Fact]
            public void GetService_can_be_accessed_from_multiple_threads_concurrently()
            {
                for (var i = 0; i < 30; i++)
                {
                    var bag = new ConcurrentBag<IPilkington>();
                    var karl = new Mock<IPilkington>().Object;
                    var resolver = new CachingDependencyResolver(new SingletonDependencyResolver<IPilkington>(karl));

                    ExecuteInParallel(() => bag.Add(resolver.GetService<IPilkington>()));

                    Assert.Equal(20, bag.Count);
                    Assert.True(bag.All(c => karl == c));
                }
            }
        }

        public class GetServices : TestBase
        {
            [Fact]
            public void GetServices_caches_the_result_from_the_underlying_resolver_for_a_given_type_name_pair()
            {
                var mockResolver = new Mock<IDbDependencyResolver>();
                var cachingResolver = new CachingDependencyResolver(mockResolver.Object);

                VerifyServicesAreCached(new Mock<IPilkington>().Object, cachingResolver, mockResolver, "Foo");
                VerifyServicesAreCached(new Mock<IPilkington>().Object, cachingResolver, mockResolver, "Bar");
                VerifyServicesAreCached(new Mock<IPilkington>().Object, cachingResolver, mockResolver, null);

                VerifyServicesAreCached(new Mock<IGervais>().Object, cachingResolver, mockResolver, "Foo");
                VerifyServicesAreCached(new Mock<IGervais>().Object, cachingResolver, mockResolver, "Bar");
                VerifyServicesAreCached(new Mock<IGervais>().Object, cachingResolver, mockResolver, null);
            }

            private static void VerifyServicesAreCached<T>(
                T service,
                CachingDependencyResolver cachingResolver,
                Mock<IDbDependencyResolver> mockResolver,
                string name)
            {
                mockResolver.Setup(m => m.GetServices(typeof(T), name)).Returns(new object[] { service });

                Assert.Same(service, cachingResolver.GetServices<T>(name).Single());
                mockResolver.Verify(m => m.GetServices(typeof(T), name), Times.Once());
                Assert.Same(service, cachingResolver.GetServices<T>(name).Single());
                mockResolver.Verify(m => m.GetServices(typeof(T), name), Times.Once()); // Underlying resolver not called again
            }

            [Fact]
            public void A_service_that_resolves_to_null_is_still_cached()
            {
                var mockResolver = new Mock<IDbDependencyResolver>();
                mockResolver.Setup(m => m.GetServices(typeof(IPilkington), null)).Returns(Enumerable.Empty<object>());

                var cachingResolver = new CachingDependencyResolver(mockResolver.Object);

                Assert.Empty(cachingResolver.GetServices<IPilkington>());
                mockResolver.Verify(m => m.GetServices(typeof(IPilkington), null), Times.Once());
                Assert.Empty(cachingResolver.GetServices<IPilkington>());
                mockResolver.Verify(m => m.GetServices(typeof(IPilkington), null), Times.Once()); // Underlying resolver not called again
            }

            /// <summary>
            /// This test makes calls from multiple threads such that we have at least some chance of finding threading
            /// issues. As with any test of this type just because the test passes does not mean that the code is
            /// correct. On the other hand if this test ever fails (EVEN ONCE) then we know there is a problem to
            /// be investigated. DON'T just re-run and think things are okay if the test then passes.
            /// </summary>
            [Fact]
            public void GetServices_can_be_accessed_from_multiple_threads_concurrently()
            {
                for (var i = 0; i < 30; i++)
                {
                    var bag = new ConcurrentBag<IPilkington>();
                    var karl = new Mock<IPilkington>().Object;
                    var resolver = new CachingDependencyResolver(new SingletonDependencyResolver<IPilkington>(karl));

                    ExecuteInParallel(() => bag.Add(resolver.GetServices<IPilkington>().Single()));

                    Assert.Equal(20, bag.Count);
                    Assert.True(bag.All(c => karl == c));
                }
            }
        }
    }
}
