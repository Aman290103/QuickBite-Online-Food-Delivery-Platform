using QuickBite.Restaurant.Entities;
using QuickBite.Restaurant.Data;
using Microsoft.EntityFrameworkCore;

namespace QuickBite.Restaurant.Data
{
    public static class DbInitializer
    {
        public static void Seed(RestaurantDbContext context)
        {
            for (int i = 0; i < 15; i++)
            {
                try
                {
                    context.Database.Migrate();
                    break;
                }
                catch (Exception)
                {
                    if (i == 14) throw;
                    System.Threading.Thread.Sleep(3000);
                }
            }

            var restaurants = new List<(string Name, string Cuisine, double Rating, int Time, decimal Price, string Img)>
            {
                ("Chinese Wok", "Chinese", 4.5, 15, 150, "https://images.unsplash.com/photo-1585032226651-759b368d7246"),
                ("La Pino'z Pizza-Mathura", "Pizza", 4.1, 15, 150, "https://images.unsplash.com/photo-1604382354936-07c5d9983bd3"),
                ("On The Wok chinese Food", "Multi-Cuisine", 4.7, 15, 150, "https://images.unsplash.com/photo-1569050467447-ce54b3bbc37d"),
                ("The Pizza Empire Mathura", "Desserts", 4.0, 15, 150, "https://images.unsplash.com/photo-1593560708920-61dd98c46a4e"),
                ("Prasadam Restaurant", "Multi-Cuisine", 3.0, 15, 150, "https://images.unsplash.com/photo-1630431341973-02e1b662ec35"),
                ("Da Pizza Point", "Multi-Cuisine", 4.2, 30, 150, "https://images.unsplash.com/photo-1571066811602-71683a3f680d"),
                ("Rowdy Cafe", "Multi-Cuisine", 4.6, 15, 150, "https://images.unsplash.com/photo-1554118811-1e0d58224f24"),
                ("Shri Bihari Ji Cafe", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085"),
                ("The Foodies Bar", "Beverages", 4.2, 30, 150, "https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd"),
                ("Darshit Food Junction | Tiffin Service Mathura", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1546069901-ba9599a7e63c"),
                ("The Urban Terrace Restaurant", "Multi-Cuisine", 4.9, 30, 150, "https://images.unsplash.com/photo-1559339352-11d035aa65de"),
                ("Punjabi Tadka Family Restaurant", "Indian", 4.0, 30, 150, "https://images.unsplash.com/photo-1585937421612-70a008356fbe"),
                ("Gangasagar Food Corner", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1504674900247-0877df9cc836"),
                ("Gurjar Family Restaurant", "Multi-Cuisine", 4.5, 30, 150, "https://images.unsplash.com/photo-1567188040759-fb8a883dc6d8"),
                ("Do bhai pizza wale", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1513104890138-7c749659a591"),
                ("THE FOOD VILLA", "Multi-Cuisine, Beverages", 4.4, 30, 150, "https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b"),
                ("TAISTEE CHINESE FOOD CORNER", "Multi-Cuisine", 4.0, 30, 150, "https://images.unsplash.com/photo-1552611052-33e04de081de"),
                ("The Pizza Hub", "Pizza", 4.9, 30, 150, "https://images.unsplash.com/photo-1588315029754-2dd089d39a1a"),
                ("Brijwasi Mithai Wala", "Sweets, Indian, Veg, Desserts", 4.9, 15, 0, "https://images.unsplash.com/photo-1601050690597-df0568f70950"),
                ("Shankar Mithai Wala", "North Indian, Pure Veg, Desserts", 4.7, 30, 0, "https://images.unsplash.com/photo-1551024506-0bccd828d307"),
                ("Pizza Hut", "Pizza, Fast Food, Veg", 4.5, 30, 0, "https://images.unsplash.com/photo-1590947132387-155cc02f3212"),
                ("Sitara by Lotus Grand", "Indian", 5.0, 30, 150, "https://images.unsplash.com/photo-1543007630-9710e4a00a20"),
                ("Yadav Family Dhaba", "Indian", 4.3, 30, 150, "https://images.unsplash.com/photo-1631452180519-c014fe946bc7"),
                ("D V Foods", "Multi-Cuisine", 4.5, 30, 150, "https://images.unsplash.com/photo-1565557623262-b51c2513a641"),
                ("The Trunk Rooftop", "Chinese, Indian", 4.5, 30, 150, "https://images.unsplash.com/photo-1533777857889-4be7c70b33f7"),
                ("D V Caterers", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1555244162-803834f70033"),
                ("Suraaj food", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1505253716362-afaea1d3d1af"),
                ("Haldiram's", "Snacks, Indian, Veg", 4.8, 30, 0, "https://images.unsplash.com/photo-1558618666-fcd25c85cd64"),
                ("What A Sandwich", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1528735602780-2552fd46c7af"),
                ("आदर्श छोले भटूरे", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1589647363585-f4a7d3877b10"),
                ("The pizza point", "Multi-Cuisine", 4.7, 30, 150, "https://images.unsplash.com/photo-1511018556340-d16986a1c194"),
                ("Food Point", "Multi-Cuisine", 4.1, 30, 150, "https://images.unsplash.com/photo-1568901346375-23c9450c58cd"),
                ("Ekyam Sattvic Kitchen", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1512621776951-a57141f2eefd"),
                ("italian hue", "italian", 4.0, 30, 0, "https://images.unsplash.com/photo-1498579150354-977475b7ea0b"),
                ("Sagar Ratna", "Indian Cuisine", 4.0, 30, 0, "https://images.unsplash.com/photo-1668236543090-82eba5ee5976"),
                ("Agrawal Restaurant SINCE 1969", "Chinese, Indian", 4.0, 15, 150, "https://images.unsplash.com/photo-1589302168068-964664d93dc0"),
                ("Love wings cafe and restro", "Multi-Cuisine", 4.1, 30, 150, "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb"),
                ("Brajbhog dhaba", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1544025162-d76694265947"),
                ("Manhar Family Restaurant", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1414235077428-338989a2e8c0"),
                ("Pizza Cut & Slice", "Pizza", 4.9, 30, 150, "https://images.unsplash.com/photo-1534308983496-4fabb1a015ee"),
                ("AGRAWAL FAMILY DHABA", "Beverages, Indian", 3.0, 30, 150, "https://images.unsplash.com/photo-1564890369478-c89ca3d9cde5"),
                ("GARAM MASALA Food Station", "Multi-Cuisine", 4.9, 30, 150, "https://images.unsplash.com/photo-1596797038530-2c107229654b"),
                ("THE BRAJ SPICY", "Multi-Cuisine", 4.0, 30, 150, "https://images.unsplash.com/photo-1626777552726-4a6b54c97e46"),
                ("Fat Tiger Mathura", "Multi-Cuisine", 4.7, 30, 150, "https://images.unsplash.com/photo-1571091718767-18b5b1457add"),
                ("Royal Grand Restaurent", "Multi-Cuisine", 4.5, 30, 150, "https://images.unsplash.com/photo-1514933651103-005eec06c04b"),
                ("Hugs & Bite", "Beverages", 4.5, 30, 150, "https://images.unsplash.com/photo-1563805042-7684c019e1cb"),
                ("UP85 Chur Chur Naan", "Indian", 4.2, 30, 150, "https://images.unsplash.com/photo-1574673067736-1a27d3cee3bf"),
                ("Chaat Chaupal", "Multi-Cuisine", 4.5, 30, 150, "https://images.unsplash.com/photo-1603569283847-aa295f0d016a"),
                ("Munch Box", "Multi-Cuisine", 4.2, 30, 150, "https://images.unsplash.com/photo-1573080496219-bb080dd4f877"),
                ("Coco Chocolate Company", "Beverages, Desserts", 4.4, 30, 150, "https://images.unsplash.com/photo-1511381939415-e44015466834"),
                ("Bansal Restaurant", "Multi-Cuisine", 4.0, 30, 150, "https://images.unsplash.com/photo-1540420773420-3366772f4999"),
                ("Brijveggies", "Multi-Cuisine", 4.8, 30, 150, "https://images.unsplash.com/photo-1512058560541-628f32e927c3"),
                ("GREAT INDIAN FOOD AND PIZZA", "Multi-Cuisine", 5.0, 30, 150, "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38"),
                ("Pizza Hut Dwarkapuri", "Pizza", 4.7, 30, 150, "https://images.unsplash.com/photo-1544982503-9f984c14501a"),
                ("Punjabi Rasoi", "Indian", 4.1, 30, 150, "https://images.unsplash.com/photo-1546833999-b9f581a1996d"),
                ("Cafe The Heaven", "Multi-Cuisine, Beverages", 4.0, 30, 150, "https://images.unsplash.com/photo-1498804103079-a6351b050096")
            };

            foreach (var r in restaurants)
            {
                // Generate a consistent ID based on Name hash for cross-service sync
                var resId = GenerateGuid(r.Name);
                var uniqueImage = r.Img.Contains("?") ? r.Img : $"{r.Img}?auto=format&fit=crop&w=800&q=80";

                var existing = context.Restaurants.FirstOrDefault(res => res.RestaurantId == resId);
                if (existing != null)
                {
                    existing.ImageUrl = uniqueImage;
                }
                else
                {
                    context.Restaurants.Add(new QuickBite.Restaurant.Entities.Restaurant
                    {
                        RestaurantId = resId,
                        OwnerId = Guid.NewGuid(),
                        Name = r.Name,
                        Description = $"{r.Name} - Authentic {r.Cuisine} in Mathura. Serving the best local flavors with premium quality.",
                        Cuisine = r.Cuisine,
                        Address = "Mathura, Uttar Pradesh",
                        City = "Mathura",
                        AvgRating = r.Rating,
                        IsOpen = true,
                        IsApproved = true,
                        ImageUrl = uniqueImage,
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
