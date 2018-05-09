namespace EntityFramework.Pool.Test
{
    using System.Linq;
    using Xunit;
    using Xunit.Abstractions;

    public class LikeTest
    {
        private  readonly ITestOutputHelper _testOutputHelper;
        
        public LikeTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }


        string searchKey = "[A-F]%";

        [Fact]
        public void StartsWith()
        {
           

            using (var dataContext = new SampleDbContext()) {
                //dataContext.Database.Log += (string message) =>
                //{
                //    //_testOutputHelper.WriteLine(message);
                //};

                string key = "[a-b]";
                var query = dataContext.Categories.Where(item => item.CategoryName.StartsWith(key));

            

                var result = dataContext.Categories.Where(item => item.CategoryName.StartsWith(key)).ToList();

                foreach (var item in result)
                {
                    _testOutputHelper.WriteLine(item.CategoryName);
                }

            }
        }


        [Fact]
        public void Contains()
        {
            using (var dataContext = new SampleDbContext())
            {
                var result = dataContext.Categories.Where(item => item.CategoryName.Contains("[a-b]")).ToList();

                foreach (var item in result)
                {
                    _testOutputHelper.WriteLine(item.CategoryName);
                }

            }
        }


        [Fact]
        public void EndsWith()
        {
            using (var dataContext = new SampleDbContext())
            {
                var result = dataContext.Categories.Where(item => item.CategoryName.EndsWith("[a-b]")).ToList();

                foreach (var item in result)
                {
                    _testOutputHelper.WriteLine(item.CategoryName);
                }

            }
        }


    }
}
