﻿using System.Data.Entity;
using EntityFrameworkCoreUnitTest;
using Xunit;

namespace EntityFramework6UnitTest
{
   public sealed class LazyLoading_Test
    {

        [Fact]

        public async void Query_Test()
        {

            using (NorthwindContext context = new NorthwindContext()) {

                Order order = await context.Orders.SingleAsync(item => item.OrderID == 10253);

                Assert.NotNull(order);

                Assert.NotNull(order.OrderDetails);

                Assert.Equal(3, order.OrderDetails.Count);
            }
        }
    }
}
