using Microsoft.EntityFrameworkCore;

using MondayMinuteApi.Models;

namespace MondayMinuteApi.Data
{
    public class MondayMinuteContext : DbContext
    {
        public MondayMinuteContext(DbContextOptions<MondayMinuteContext> options) : base(options) 
        { 
        }

        public DbSet<MondayMinute> MondayMinutes { get; set; }

    }
}
