using FluentMigrator;

namespace CS2_Admin.Database.Migrations;

[Migration(2026062101)]
public class AddAdminTagsTable : Migration
{
    public override void Up()
    {
        if (Schema.Table("admin_tags").Exists())
        {
            return;
        }

        Create.Table("admin_tags")
            .WithColumn("id").AsInt64().PrimaryKey().Identity().NotNullable()
            .WithColumn("group_name").AsString(64).NotNullable()
            .WithColumn("tag_text").AsString(64).NotNullable().WithDefaultValue("")
            .WithColumn("tag_color").AsString(32).NotNullable().WithDefaultValue("[default]")
            .WithColumn("chat_color").AsString(32).NotNullable().WithDefaultValue("[white]")
            .WithColumn("name_color").AsString(32).NotNullable().WithDefaultValue("[default]")
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().NotNullable().WithDefaultValue(SystemMethods.CurrentDateTime);

        Create.Index("idx_admin_tags_group_name").OnTable("admin_tags").OnColumn("group_name").Unique();
    }

    public override void Down()
    {
        Delete.Table("admin_tags");
    }
}
