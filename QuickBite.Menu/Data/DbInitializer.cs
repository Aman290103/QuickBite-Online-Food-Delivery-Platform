using QuickBite.Menu.Entities;
using QuickBite.Menu.Data;
using Microsoft.EntityFrameworkCore;

namespace QuickBite.Menu.Data
{
    public static class DbInitializer
    {
        public static void Seed(MenuDbContext context)
        {
            context.Database.Migrate();

            var restaurantNames = new List<string>
            {
                "Chinese Wok", "La Pino'z Pizza-Mathura", "On The Wok chinese Food", "The Pizza Empire Mathura",
                "Prasadam Restaurant", "Da Pizza Point", "Rowdy Cafe", "Shri Bihari Ji Cafe", "The Foodies Bar",
                "Darshit Food Junction | Tiffin Service Mathura", "The Urban Terrace Restaurant", "Punjabi Tadka Family Restaurant",
                "Gangasagar Food Corner", "Gurjar Family Restaurant", "Do bhai pizza wale", "THE FOOD VILLA",
                "TAISTEE CHINESE FOOD CORNER", "The Pizza Hub", "Brijwasi Mithai Wala", "Shankar Mithai Wala",
                "Pizza Hut", "Sitara by Lotus Grand", "Yadav Family Dhaba", "D V Foods", "The Trunk Rooftop",
                "D V Caterers", "Suraaj food", "Haldiram's", "What A Sandwich", "आदर्श छोले भटूरे",
                "The pizza point", "Food Point", "Ekyam Sattvic Kitchen", "italian hue", "Sagar Ratna",
                "Agrawal Restaurant SINCE 1969", "Love wings cafe and restro", "Brajbhog dhaba", "Manhar Family Restaurant",
                "Pizza Cut & Slice", "AGRAWAL FAMILY DHABA", "GARAM MASALA Food Station", "THE BRAJ SPICY",
                "Fat Tiger Mathura", "Royal Grand Restaurent", "Hugs & Bite", "UP85 Chur Chur Naan", "Chaat Chaupal",
                "Munch Box", "Coco Chocolate Company", "Bansal Restaurant", "Brijveggies", "GREAT INDIAN FOOD AND PIZZA",
                "Pizza Hut Dwarkapuri", "Punjabi Rasoi", "Cafe The Heaven"
            };

            foreach (var name in restaurantNames)
            {
                var restaurantId = GenerateGuid(name);
                if (!context.Categories.Any(c => c.RestaurantId == restaurantId))
                {
                    var catId = Guid.NewGuid();
                    context.Categories.Add(new MenuCategory
                    {
                        CategoryId = catId,
                        RestaurantId = restaurantId,
                        Name = "Specialties",
                        Description = "Our most popular dishes."
                    });

                    context.Items.Add(new MenuItem
                    {
                        ItemId = Guid.NewGuid(),
                        RestaurantId = restaurantId,
                        CategoryId = catId,
                        Name = $"Best of {name}",
                        Description = "A chef's special choice for you.",
                        Price = 199,
                        IsVeg = true,
                        IsAvailable = true,
                        ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=400"
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
