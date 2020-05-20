using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Model
{
    public class StudentMark
    {
        public int Id { get; set; }

        public int Mark { get; set; }

        public int StudentId { get; set; }
        public virtual User User { get; set; }
        public int TestId { get; set; }
        public virtual Test Test { get; set; }
    }

    public class StudentMarkConfig : EntityTypeConfiguration<StudentMark>
    {
        public StudentMarkConfig()
        {
            Property(mark => mark.Mark).IsOptional();
        }
    }
}
