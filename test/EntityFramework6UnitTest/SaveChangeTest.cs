using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EntityFrameworkCoreUnitTest;
using Xunit;

namespace EntityFramework6UnitTest
{
 

    public class SaveChangeTest
    {

        public SaveChangeTest()
        {
            //using (var dataContext = new SampleDbContext()) {
            //    List<Category> lst = dataContext.Categories.ToList();
            //}
        }

        [Fact]
        public void SaveChage()
        {
            using (var dataContext = new SampleDbContext())
            {
                List<Category> lst = dataContext.Categories.ToList();
                //foreach (var item in lst) {
                //    item.CategoryName = item.CategoryName+" ";
                //}
                //dataContext.SaveChanges();
            }

            //dotMemory.Check(
            //    memory =>
            //    {
            //        //  Assert.th
            //    });


        }

       
    }
}
 
