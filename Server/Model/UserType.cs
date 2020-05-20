using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Model
{
    public class UserType
    {
        public int Id { get; set; }

        public string Type { get; set; }

        public virtual ObservableListSource<User> Users { get; set; } = new ObservableListSource<User>();
    }

    public class UserTypeConfig : EntityTypeConfiguration<UserType>
    {
        public UserTypeConfig()
        {
            Property(user => user.Type).HasMaxLength(10).IsRequired();
            HasIndex(user => user.Type).IsUnique();
        }
    }
}
