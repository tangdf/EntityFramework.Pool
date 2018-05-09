using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    using EntityFramework6UnitTest;
    using EntityFrameworkCoreUnitTest;

    class Program
    {
        static void Main(string[] args)
        {
            using (var dataContext0 = new SampleDbContext())
            {
                List<Category> lst = dataContext0.Categories.ToList();
                //foreach (var item in lst) {
                //    item.CategoryName = item.CategoryName+" ";
                //}
                //dataContext.SaveChanges();
            }
            Console.WriteLine("0");
            Console.ReadLine();
            using (var dataContext1 = new SampleDbContext())
            {
                List<Category> lst = dataContext1.Categories.ToList();
                //foreach (var item in lst) {
                //    item.CategoryName = item.CategoryName+" ";
                //}
                //dataContext.SaveChanges();
            }
            Console.WriteLine("1");
            Console.ReadLine();
            var dataContext2 = new SampleDbContext();
            Console.WriteLine("2");
            Console.ReadLine();
        }
    }
}
