namespace StockAnalysis.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Update2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserProfile", "UserStockInformation", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserProfile", "UserStockInformation");
        }
    }
}
