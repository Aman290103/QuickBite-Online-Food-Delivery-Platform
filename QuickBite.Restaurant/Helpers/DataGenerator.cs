using QuickBite.Restaurant.Entities;
using System;
using System.Collections.Generic;

namespace QuickBite.Restaurant.Helpers
{
    public static class DataGenerator
    {
        public static List<Entities.Restaurant> GenerateRestaurantsAroundLocation(Guid ownerId, double lat, double lon)
        {
            var cuisines = new[] { "Italian", "Indian", "Chinese", "Fast Food", "Mexican", "Japanese" };
            var names = new[] { "The Golden Spoon", "Pizza Palace", "Spice Route", "Burger Barn", "Sushi Sun", "Taco Town", "Curry Castle", "Wok World" };
            var random = new Random();
            var restaurants = new List<Entities.Restaurant>();

            for (int i = 1; i <= 50; i++)
            {
                restaurants.Add(new Entities.Restaurant
                {
                    RestaurantId = Guid.NewGuid(),
                    OwnerId = ownerId,
                    Name = $"{names[random.Next(names.Length)]} {i}",
                    Description = "Automatically seeded premium restaurant.",
                    Cuisine = cuisines[random.Next(cuisines.Length)],
                    Address = $"{i * 10}, Main Street, Near You",
                    City = "User's Area",
                    Latitude = lat + (random.NextDouble() - 0.5) * 0.05, // Seed within ~5km of user
                    Longitude = lon + (random.NextDouble() - 0.5) * 0.05,
                    Phone = $"98765432{i:D2}",
                    AvgRating = 3.5 + random.NextDouble() * 1.5,
                    IsOpen = true,
                    IsApproved = true,
                    MinOrderAmount = 100 + (random.Next(0, 10) * 10),
                    EstimatedDeliveryMin = 15 + random.Next(0, 20)
                });
            }

            return restaurants;
        }
    }
}
