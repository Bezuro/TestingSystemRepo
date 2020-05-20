﻿namespace Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class m2 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.StudentMarks", "Mark", c => c.Int());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.StudentMarks", "Mark", c => c.Int(nullable: false));
        }
    }
}
