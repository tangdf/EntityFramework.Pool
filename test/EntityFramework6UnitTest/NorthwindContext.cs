using System.Data.Entity;
using System.Data.SqlClient;

namespace EntityFramework6UnitTest
{
    public class NorthwindContext : DbContext
    {
        private static string s_connectionString;
        static NorthwindContext()
        {
            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "10.0.2.229",
                InitialCatalog = "Northwind",
                UserID = "sa",
                Password = "w1!"
            };
            s_connectionString = sqlConnectionStringBuilder.ToString();
        }
        public NorthwindContext() : base(s_connectionString)
        {

        }



        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>();

            modelBuilder.Entity<OrderDetail>().HasKey(od => new {
                od.OrderID,
                od.ProductID
            });
            modelBuilder.Entity<OrderDetail>().ToTable("Order Details");

        }


    }
}
