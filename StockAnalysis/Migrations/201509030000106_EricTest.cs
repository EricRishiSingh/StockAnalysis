namespace StockAnalysis.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EricTest : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserProfile", "StockSymbols", c => c.String());
        }
        
        public override void Down()
        {
            //DropColumn("dbo.UserProfile", "StockSymbols");
        }
    }
}
