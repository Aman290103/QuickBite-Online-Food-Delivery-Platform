using QuickBite.Restaurant.Entities;
using QuickBite.Restaurant.Data;
using Microsoft.EntityFrameworkCore;

namespace QuickBite.Restaurant.Data
{
    public static class DbInitializer
    {
        public static void Seed(RestaurantDbContext context)
        {
            context.Database.Migrate();

            var restaurants = new List<(string Name, string Cuisine, double Rating, int Time, decimal Price, string Img)>
            {
                ("Chinese Wok", "Chinese", 4.5, 15, 150, "https://images.unsplash.com/photo-1585032226651-759b368d7246"),
                ("La Pino'z Pizza-Mathura", "Pizza", 4.1, 15, 150, "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3"),
                ("On The Wok chinese Food", "Multi-Cuisine", 4.7, 15, 150, "https://images.unsplash.com/photo-1512058560541-628f32e927c3"),
                ("The Pizza Empire Mathura", "Desserts", 4.0, 15, 150, "https://images.unsplash.com/photo-1541745537411-b8046dc6d66c"),
                ("Prasadam Restaurant", "Multi-Cuisine", 3.0, 15, 150, "https://images.unsplash.com/photo-1567306226416-28f0efdc88ce"),
                ("Da Pizza Point", "Multi-Cuisine", 4.2, 30, 150, "https://images.unsplash.com/photo-1593560708920-61dd98c46a4e"),
                ("Rowdy Cafe", "Multi-Cuisine", 4.6, 15, 150, "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085"),
                ("Shri Bihari Ji Cafe", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1502462041640-b3d78a82e174"),
                ("The Foodies Bar", "Beverages", 4.2, 30, 150, "https://images.unsplash.com/photo-1544145945-f904253d0c7e"),
                ("Darshit Food Junction | Tiffin Service Mathura", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1546069901-ba9599a7e63c"),
                ("The Urban Terrace Restaurant", "Multi-Cuisine", 4.9, 30, 150, "https://images.unsplash.com/photo-1559339352-11d035aa65de"),
                ("Punjabi Tadka Family Restaurant", "Indian", 4.0, 30, 150, "https://images.unsplash.com/photo-1626777553730-b8445582484e"),
                ("Gangasagar Food Corner", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1606491956689-2ea8c5119c85"),
                ("Gurjar Family Restaurant", "Multi-Cuisine", 4.5, 30, 150, "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4"),
                ("Do bhai pizza wale", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1513104890138-7c749659a591"),
                ("THE FOOD VILLA", "Multi-Cuisine, Beverages", 4.4, 30, 150, "https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b"),
                ("TAISTEE CHINESE FOOD CORNER", "Multi-Cuisine", 4.0, 30, 150, "https://images.unsplash.com/photo-1552611052-33e04de081de"),
                ("The Pizza Hub", "Pizza", 4.9, 30, 150, "https://images.unsplash.com/photo-1574071318508-1cdbad80ad38"),
                ("Brijwasi Mithai Wala", "Sweets, Indian, Veg, Desserts", 4.9, 15, 0, "https://images.unsplash.com/photo-1589113103503-49ef83d89e7c"),
                ("Shankar Mithai Wala", "North Indian, Pure Veg, Desserts", 4.7, 30, 0, "https://images.unsplash.com/photo-1601050690597-df0568f70950"),
                ("Pizza Hut", "Pizza, Fast Food, Veg", 4.5, 30, 0, "https://images.unsplash.com/photo-1513104890138-7c749659a591"),
                ("Sitara by Lotus Grand", "Indian", 5.0, 30, 150, "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4"),
                ("Yadav Family Dhaba", "Indian", 4.3, 30, 150, "https://images.unsplash.com/photo-1626777553730-b8445582484e"),
                ("D V Foods", "Multi-Cuisine", 4.5, 30, 150, "https://images.unsplash.com/photo-1567306226416-28f0efdc88ce"),
                ("The Trunk Rooftop", "Chinese, Indian", 4.5, 30, 150, "https://images.unsplash.com/photo-1559339352-11d035aa65de"),
                ("D V Caterers", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4"),
                ("Suraaj food", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1606491956689-2ea8c5119c85"),
                ("Haldiram's", "Snacks, Indian, Veg", 4.8, 30, 0, "https://images.unsplash.com/photo-1589647363585-f4a7d3877b10"),
                ("What A Sandwich", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1528735602780-2552fd46c7af"),
                ("आदर्श छोले भटूरे", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1626132646545-0d3290610313"),
                ("The pizza point", "Multi-Cuisine", 4.7, 30, 150, "https://images.unsplash.com/photo-1574071318508-1cdbad80ad38"),
                ("Food Point", "Multi-Cuisine", 4.1, 30, 150, "https://images.unsplash.com/photo-1552611052-33e04de081de"),
                ("Ekyam Sattvic Kitchen", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1512621776951-a57141f2eefd"),
                ("italian hue", "italian", 4.0, 30, 0, "https://images.unsplash.com/photo-1498579150354-977475b7ea0b"),
                ("Sagar Ratna", "Indian Cuisine", 4.0, 30, 0, "https://images.unsplash.com/photo-1589302168068-964664d93dc0"),
                ("Agrawal Restaurant SINCE 1969", "Chinese, Indian", 4.0, 15, 150, "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4"),
                ("Love wings cafe and restro", "Multi-Cuisine", 4.1, 30, 150, "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085"),
                ("Brajbhog dhaba", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1626777553730-b8445582484e"),
                ("Manhar Family Restaurant", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4"),
                ("Pizza Cut & Slice", "Pizza", 4.9, 30, 150, "https://images.unsplash.com/photo-1574071318508-1cdbad80ad38"),
                ("AGRAWAL FAMILY DHABA", "Beverages, Indian", 3.0, 30, 150, "https://images.unsplash.com/photo-1626777553730-b8445582484e"),
                ("GARAM MASALA Food Station", "Multi-Cuisine", 4.9, 30, 150, "https://images.unsplash.com/photo-1512621776951-a57141f2eefd"),
                ("THE BRAJ SPICY", "Multi-Cuisine", 4.0, 30, 150, "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4"),
                ("Fat Tiger Mathura", "Multi-Cuisine", 4.7, 30, 150, "https://images.unsplash.com/photo-1571091718767-18b5b1457add"),
                ("Royal Grand Restaurent", "Multi-Cuisine", 4.5, 30, 150, "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4"),
                ("Hugs & Bite", "Beverages", 4.5, 30, 150, "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085"),
                ("UP85 Chur Chur Naan", "Indian", 4.2, 30, 150, "https://images.unsplash.com/photo-1626777553730-b8445582484e"),
                ("Chaat Chaupal", "Multi-Cuisine", 4.5, 30, 150, "https://images.unsplash.com/photo-1606491956689-2ea8c5119c85"),
                ("Munch Box", "Multi-Cuisine", 4.2, 30, 150, "https://images.unsplash.com/photo-1571091718767-18b5b1457add"),
                ("Coco Chocolate Company", "Beverages, Desserts", 4.4, 30, 150, "https://images.unsplash.com/photo-1563805042-7684c019e1cb"),
                ("Bansal Restaurant", "Multi-Cuisine", 4.0, 30, 150, "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4"),
                ("Brijveggies", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1512621776951-a57141f2eefd"),
                ("GREAT INDIAN FOOD AND PIZZA", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4"),
                ("Pizza Hut Dwarkapuri", "Pizza", 4.7, 30, 150, "https://images.unsplash.com/photo-1513104890138-7c749659a591"),
                ("Punjabi Rasoi", "Indian", 4.1, 30, 150, "https://images.unsplash.com/photo-1626777553730-b8445582484e"),
                ("Cafe The Heaven", "Multi-Cuisine, Beverages", 4.0, 30, 150, "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085")
            };

            foreach (var r in restaurants)
            {
                // Generate a consistent ID based on Name hash for cross-service sync
                var id = GenerateGuid(r.Name);
                if (!context.Restaurants.Any(res => res.RestaurantId == id))
                {
                    context.Restaurants.Add(new Entities.Restaurant
                    {
                        RestaurantId = id,
                        OwnerId = Guid.NewGuid(),
                        Name = r.Name,
                        Description = $"{r.Name} - Authentic {r.Cuisine} in Mathura.",
                        Cuisine = r.Cuisine,
                        Address = "Mathura, Uttar Pradesh",
                        City = "Mathura",
                        AvgRating = r.Rating,
                        IsOpen = true,
                        IsApproved = true,
                        ImageUrl = r.Img + "?w=800",
                        MinOrderAmount = r.Price,
                        EstimatedDeliveryMin = r.Time,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            context.SaveChanges();
        }

        private static Guid GenerateGuid(string name)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.Default.GetBytes("QuickBite_" + name));
                return new Guid(hash);
            }
        }
    }
}
