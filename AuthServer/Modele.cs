using Microsoft.Extensions.Logging;

namespace AuthServer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

public class DatabaseContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Channel> Channels { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // connect mariaDB
        optionsBuilder.UseMySql("server=localhost;database=test;user=test;password=test1234",
            new MySqlServerVersion(new Version(11, 7, 2))).LogTo(Console.WriteLine, LogLevel.Information);
    }
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class Channel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public List<User> Users { get; set; } = [];
    public User CreateBy { get; set; } = null!; // User who created the channel
    public DateTime CreateAt { get; set; }
}

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public User User { get; set; } = null!;
    public Channel Channel { get; set; } = null!;
}