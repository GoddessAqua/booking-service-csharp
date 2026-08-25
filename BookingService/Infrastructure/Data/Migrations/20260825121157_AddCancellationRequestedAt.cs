using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationRequestedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "version",                                                                                                                                                                                                          
                table: "bookings",                                                                                                                                                                                                        
                type: "bigint",                                                                                                                                                                                                           
                nullable: false,                                                                                                                                                                                                          
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cancellation_requested_at",                                                                                                                                                                                        
                table: "bookings",                                                                                                                                                                                                        
                type: "timestamp with time zone",                                                                                                                                                                                         
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(                                                                                                                                                                                                  
                name: "cancellation_requested_at",                                                                                                                                                                                        
                table: "bookings");                                                                                                                                                                                                       
                                                                                                                                                                                                                                    
            migrationBuilder.DropColumn(                                                                                                                                                                                                  
                name: "version",                                                                                                                                                                                                          
                table: "bookings");
        }
    }
}
