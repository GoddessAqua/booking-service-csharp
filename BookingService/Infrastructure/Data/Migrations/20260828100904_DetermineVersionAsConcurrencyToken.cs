using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DetermineVersionAsConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(                                                                                                                                                                                                  
                name: "version",                                                                                                                                                                                                          
                table: "bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(                                                                                                                                                                                             
                name: "version",                                                                                                                                                                                                          
                table: "bookings",                                                                                                                                                                                                        
                type: "bigint",                                                                                                                                                                                                           
                nullable: false,                                                                                                                                                                                                          
                defaultValue: 0L);
        }
    }
}
