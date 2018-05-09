namespace EntityFramework.Pool.Test
{
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration;
    using System.Data.SqlClient;

    public class SampleDbContext : DbContext
    {
        private static string s_connectionString;
        static SampleDbContext()
        {
            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "10.0.2.229",
                InitialCatalog = "Heap_Record",
                UserID = "sa",
                Password = "w1!"
            };
            s_connectionString = sqlConnectionStringBuilder.ToString();
        }
        public SampleDbContext():base(s_connectionString)
        {
            //this.Database.Log+= (string message) =>
            //{
                
            //}
        }


        public virtual DbSet<Category> Categories { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
           

            base.OnModelCreating(modelBuilder);
            EntityTypeConfiguration<Category> entityTypeConfiguration = modelBuilder.Entity<Category>();
            entityTypeConfiguration.ToTable("Category");
            entityTypeConfiguration.HasKey(e => e.CategoryID);
            entityTypeConfiguration.Property(t => t.CategoryName);
            //modelBuilder.Types< Category>().Configure(item=>item.Ignore(e=>e.CategoryName));
            //entityTypeConfiguration.Property(e => e.CategoryID).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        }
        
    }
}