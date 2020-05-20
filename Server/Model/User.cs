using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Server.Model
{
    public class User
    {
        public int Id { get; set; }

        public string Login { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }

        public int UserTypeId { get; set; }
        public virtual UserType UserType { get; set; }

        public virtual ObservableListSource<StudentMark> StudentMarks { get; set; } = new ObservableListSource<StudentMark>();
    }

    public class UserConfig : EntityTypeConfiguration<User>
    {
        public UserConfig()
        {
            Property(user => user.Login).HasMaxLength(20).IsRequired();
            HasIndex(user => user.Login).IsUnique();
            Property(user => user.Password).HasMaxLength(30).IsRequired();
            Property(user => user.FullName).HasMaxLength(60).IsRequired();
        }
    }
}
