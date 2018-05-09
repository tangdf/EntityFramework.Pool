namespace EntityFramework.Pool.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Xunit;
    using Xunit.Abstractions;

    public class SaveChangeTest
    {

        private ITestOutputHelper _testOutputHelper;

        public SaveChangeTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            ObjectAssert.TestOutputHelper = _testOutputHelper;
        }

        [Fact]
        public void SaveChage()
        {

            var dataContext1 = new SampleDbContext();

            var dataContext2 = new SampleDbContext();
            
                List<Category> lst = dataContext2.Categories.ToList();
                 
                
            ObjectAssert.Equal(dataContext1,dataContext2);


        }


        


    }
}
 
