using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Model
{
    public class Test
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }

        public virtual ObservableListSource<StudentMark> StudentMarks { get; set; } = new ObservableListSource<StudentMark>();
    }

    public class TestConfig : EntityTypeConfiguration<Test>
    {
        public TestConfig()
        {
            Property(user => user.Name).HasMaxLength(50).IsRequired();
            HasIndex(user => user.Name).IsUnique();
            Property(user => user.Description).HasMaxLength(100).IsRequired();
            Property(user => user.Path).HasMaxLength(200).IsRequired();
            HasIndex(user => user.Path).IsUnique();
        }
    }
}
