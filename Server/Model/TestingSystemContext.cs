using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace Server.Model
{
    class TestingSystemContext : DbContext
    {
        static TestingSystemContext()
        {
            Database.SetInitializer<TestingSystemContext>(new CreateDatabaseIfNotExists<TestingSystemContext>());
        }
        public TestingSystemContext() : base("name=TestingSystemConnectionString")
        { }

        public virtual DbSet<UserType> UserTypes { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Test> Tests { get; set; }
        public virtual DbSet<StudentMark> StudentMarks { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new UserTypeConfig());
            modelBuilder.Configurations.Add(new UserConfig());
            modelBuilder.Configurations.Add(new TestConfig());
            modelBuilder.Configurations.Add(new StudentMarkConfig());

            modelBuilder.Entity<User>().HasRequired<UserType>(user => user.UserType)
                .WithMany(userType => userType.Users)
                .HasForeignKey<int>(user => user.UserTypeId);

            modelBuilder.Entity<StudentMark>().HasRequired<User>(mark => mark.User)
                .WithMany(user => user.StudentMarks)
                .HasForeignKey<int>(mark => mark.StudentId);

            modelBuilder.Entity<StudentMark>().HasRequired<Test>(mark => mark.Test)
                .WithMany(test => test.StudentMarks)
                .HasForeignKey<int>(mark => mark.TestId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
