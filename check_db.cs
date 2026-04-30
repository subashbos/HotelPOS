using System;
using System.Linq;
using HotelPOS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

var options = new DbContextOptionsBuilder<HotelDbContext>()
    .UseSqlServer(config.GetConnectionString("DefaultConnection"))
    .Options;

using var context = new HotelDbContext(options);
try {
    var userCount = context.Users.Count();
    var catCount = context.Categories.Count();
    Console.WriteLine($"USERS: {userCount}");
    Console.WriteLine($"CATEGORIES: {catCount}");
    if (userCount > 0) {
        Console.WriteLine($"First User: {context.Users.First().Username}");
    }
} catch (Exception ex) {
    Console.WriteLine($"ERROR: {ex.Message}");
}
