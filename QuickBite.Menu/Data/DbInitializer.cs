using QuickBite.Menu.Entities;
using QuickBite.Menu.Data;
using Microsoft.EntityFrameworkCore;

namespace QuickBite.Menu.Data
{
    public static class DbInitializer
    {
        record MenuSeed(string Cat, string Item, string Desc, decimal Price, bool IsVeg, string Img);

        public static void Seed(MenuDbContext context)
        {
            for (int i = 0; i < 15; i++)
            {
                try { context.Database.Migrate(); break; }
                catch (Exception) { if (i == 14) throw; System.Threading.Thread.Sleep(3000); }
            }

            var restaurantMenus = new Dictionary<string, List<MenuSeed>>
            {
                ["Chinese Wok"] = new() {
                    new("Starters","Veg Spring Rolls","Crispy fried rolls with veggies",120,true,"https://images.unsplash.com/photo-1548943487-a2e4e43b4853?w=400&q=80"),
                    new("Main Course","Veg Fried Rice","Wok-tossed rice with vegetables",150,true,"https://images.unsplash.com/photo-1516684732162-798a0062be99?w=400&q=80"),
                    new("Main Course","Chicken Chowmein","Stir-fried noodles with chicken",180,false,"https://images.unsplash.com/photo-1569050467447-ce54b3bbc37d?w=400&q=80"),
                    new("Main Course","Manchurian Gravy","Veg balls in spicy Chinese gravy",160,true,"https://images.unsplash.com/photo-1585032226651-759b368d7246?w=400&q=80"),
                    new("Soups","Hot & Sour Soup","Classic Chinese spicy sour soup",90,true,"https://images.unsplash.com/photo-1588566565463-180a5bf5b1a4?w=400&q=80"),
                },
                ["La Pino'z Pizza-Mathura"] = new() {
                    new("Pizza","Margherita Pizza","Classic tomato & mozzarella",199,true,"https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=400&q=80"),
                    new("Pizza","Paneer Tikka Pizza","Spicy paneer with bell peppers",249,true,"https://images.unsplash.com/photo-1513104890138-7c749659a591?w=400&q=80"),
                    new("Pizza","Farmhouse Pizza","Loaded veggie pizza",229,true,"https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=400&q=80"),
                    new("Sides","Garlic Bread","Crispy garlic butter bread",89,true,"https://images.unsplash.com/photo-1588315029754-2dd089d39a1a?w=400&q=80"),
                    new("Beverages","Cold Drink","Chilled soft beverage",49,true,"https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=400&q=80"),
                },
                ["Pizza Hut"] = new() {
                    new("Pizza","Veggie Supreme","Garden fresh vegetable pizza",299,true,"https://images.unsplash.com/photo-1571066811602-71683a3f680d?w=400&q=80"),
                    new("Pizza","Chicken Tikka Pizza","Tandoori chicken pizza",349,false,"https://images.unsplash.com/photo-1590947132387-155cc02f3212?w=400&q=80"),
                    new("Sides","Stuffed Garlic Bread","Cheese stuffed bread",149,true,"https://images.unsplash.com/photo-1534308983496-4fabb1a015ee?w=400&q=80"),
                    new("Pasta","Penne Arrabbiata","Spicy tomato pasta",199,true,"https://images.unsplash.com/photo-1498579150354-977475b7ea0b?w=400&q=80"),
                    new("Desserts","Choco Lava Cake","Warm chocolate molten cake",149,true,"https://images.unsplash.com/photo-1511381939415-e44015466834?w=400&q=80"),
                },
                ["Brijwasi Mithai Wala"] = new() {
                    new("Sweets","Mathura Peda","Famous Mathura special peda",20,true,"https://images.unsplash.com/photo-1601050690597-df0568f70950?w=400&q=80"),
                    new("Sweets","Gulab Jamun","Soft milk dumplings in sugar syrup",60,true,"https://images.unsplash.com/photo-1551024506-0bccd828d307?w=400&q=80"),
                    new("Sweets","Kaju Katli","Diamond-shaped cashew fudge",400,true,"https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=400&q=80"),
                    new("Sweets","Jalebi","Crispy syrup-soaked spirals",80,true,"https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=400&q=80"),
                    new("Namkeen","Mathura Ke Dubki Wale Aloo","Potato curry served with puri",60,true,"https://images.unsplash.com/photo-1596797038530-2c107229654b?w=400&q=80"),
                },
                ["Shankar Mithai Wala"] = new() {
                    new("Sweets","Gulab Jamun","Soft syrupy gulab jamun",60,true,"https://images.unsplash.com/photo-1551024506-0bccd828d307?w=400&q=80"),
                    new("Sweets","Rasgulla","Spongy white rasgulla",50,true,"https://images.unsplash.com/photo-1589113103503-49ef83d89e7c?w=400&q=80"),
                    new("Sweets","Barfi","Classic milk barfi",300,true,"https://images.unsplash.com/photo-1563805042-7684c019e1cb?w=400&q=80"),
                    new("Sweets","Halwa","Traditional semolina halwa",80,true,"https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=400&q=80"),
                    new("Namkeen","Kachori","Deep-fried spiced pastry",20,true,"https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&q=80"),
                },
                ["Haldiram's"] = new() {
                    new("Snacks","Samosa","Crispy fried pastry with spiced potato",20,true,"https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&q=80"),
                    new("Snacks","Aloo Tikki","Spiced potato patties",40,true,"https://images.unsplash.com/photo-1603569283847-aa295f0d016a?w=400&q=80"),
                    new("Snacks","Bhujia","Crispy sev bhujia",80,true,"https://images.unsplash.com/photo-1596797038530-2c107229654b?w=400&q=80"),
                    new("Sweets","Gulab Jamun","Classic soft gulab jamun",80,true,"https://images.unsplash.com/photo-1551024506-0bccd828d307?w=400&q=80"),
                    new("Main Course","Chole Bhature","Spiced chickpeas with fluffy bhatura",120,true,"https://images.unsplash.com/photo-1589647363585-f4a7d3877b10?w=400&q=80"),
                },
                ["UP85 Chur Chur Naan"] = new() {
                    new("Naan","Aloo Chur Chur Naan","Stuffed crispy potato naan",80,true,"https://images.unsplash.com/photo-1574673067736-1a27d3cee3bf?w=400&q=80"),
                    new("Naan","Paneer Chur Chur Naan","Stuffed crispy paneer naan",100,true,"https://images.unsplash.com/photo-1627308595229-7830a5c91f9f?w=400&q=80"),
                    new("Naan","Mixed Veg Naan","Stuffed mixed vegetable naan",90,true,"https://images.unsplash.com/photo-1585937421612-70a008356fbe?w=400&q=80"),
                    new("Sides","Dal Makhani","Creamy black lentil dal",120,true,"https://images.unsplash.com/photo-1546833999-b9f581a1996d?w=400&q=80"),
                    new("Sides","Raita","Chilled yogurt with spices",40,true,"https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=400&q=80"),
                },
                ["Chaat Chaupal"] = new() {
                    new("Chaat","Pani Puri","Crispy puris with spiced water",50,true,"https://images.unsplash.com/photo-1603569283847-aa295f0d016a?w=400&q=80"),
                    new("Chaat","Aloo Tikki Chaat","Tikki with chutneys & yogurt",60,true,"https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&q=80"),
                    new("Chaat","Papdi Chaat","Crispy papdi with tangy toppings",70,true,"https://images.unsplash.com/photo-1589647363585-f4a7d3877b10?w=400&q=80"),
                    new("Chaat","Dahi Bhalla","Soft vadas in yogurt",80,true,"https://images.unsplash.com/photo-1596797038530-2c107229654b?w=400&q=80"),
                    new("Beverages","Masala Chai","Spiced Indian tea",20,true,"https://images.unsplash.com/photo-1564890369478-c89ca3d9cde5?w=400&q=80"),
                },
                ["AGRAWAL FAMILY DHABA"] = new() {
                    new("Main Course","Dal Tadka","Yellow lentils with tempering",100,true,"https://images.unsplash.com/photo-1627308595229-7830a5c91f9f?w=400&q=80"),
                    new("Main Course","Aloo Paratha","Stuffed potato flatbread",60,true,"https://images.unsplash.com/photo-1585937421612-70a008356fbe?w=400&q=80"),
                    new("Main Course","Paneer Bhurji","Scrambled spiced cottage cheese",140,true,"https://images.unsplash.com/photo-1546833999-b9f581a1996d?w=400&q=80"),
                    new("Beverages","Lassi","Chilled sweet yogurt drink",50,true,"https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=400&q=80"),
                    new("Beverages","Masala Chai","Classic Indian spiced tea",20,true,"https://images.unsplash.com/photo-1564890369478-c89ca3d9cde5?w=400&q=80"),
                },
                ["Agrawal Restaurant SINCE 1969"] = new() {
                    new("Main Course","Paneer Butter Masala","Rich creamy paneer curry",200,true,"https://images.unsplash.com/photo-1589302168068-964664d93dc0?w=400&q=80"),
                    new("Main Course","Dal Makhani","Slow-cooked black lentils",180,true,"https://images.unsplash.com/photo-1627308595229-7830a5c91f9f?w=400&q=80"),
                    new("Breads","Butter Naan","Soft buttered naan bread",40,true,"https://images.unsplash.com/photo-1574673067736-1a27d3cee3bf?w=400&q=80"),
                    new("Rice","Veg Fried Rice","Chinese-style vegetable fried rice",150,true,"https://images.unsplash.com/photo-1585032226651-759b368d7246?w=400&q=80"),
                    new("Starters","Veg Manchurian","Crispy veg balls in manchurian sauce",160,true,"https://images.unsplash.com/photo-1569050467447-ce54b3bbc37d?w=400&q=80"),
                },
            };

            // Generic menus for remaining restaurants
            var genericPizzaMenu = new List<MenuSeed> {
                new("Pizza","Margherita Pizza","Classic tomato & cheese pizza",199,true,"https://images.unsplash.com/photo-1604382354936-07c5d9983bd3?w=400&q=80"),
                new("Pizza","Veggie Delight","Garden fresh vegetable pizza",229,true,"https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=400&q=80"),
                new("Pizza","Paneer Pizza","Spicy paneer pizza",249,true,"https://images.unsplash.com/photo-1513104890138-7c749659a591?w=400&q=80"),
                new("Sides","Garlic Bread","Crispy buttered garlic bread",89,true,"https://images.unsplash.com/photo-1571066811602-71683a3f680d?w=400&q=80"),
                new("Beverages","Cold Coffee","Chilled coffee shake",99,true,"https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=400&q=80"),
            };
            var genericIndianMenu = new List<MenuSeed> {
                new("Main Course","Paneer Butter Masala","Creamy paneer curry",200,true,"https://images.unsplash.com/photo-1585937421612-70a008356fbe?w=400&q=80"),
                new("Main Course","Dal Tadka","Tempered yellow lentils",120,true,"https://images.unsplash.com/photo-1627308595229-7830a5c91f9f?w=400&q=80"),
                new("Breads","Butter Naan","Soft buttered naan",40,true,"https://images.unsplash.com/photo-1574673067736-1a27d3cee3bf?w=400&q=80"),
                new("Rice","Jeera Rice","Cumin flavored basmati rice",120,true,"https://images.unsplash.com/photo-1546833999-b9f581a1996d?w=400&q=80"),
                new("Starters","Paneer Tikka","Grilled spiced cottage cheese",180,true,"https://images.unsplash.com/photo-1567188040759-fb8a883dc6d8?w=400&q=80"),
            };
            var genericChineseMenu = new List<MenuSeed> {
                new("Main Course","Veg Fried Rice","Wok-tossed vegetable rice",150,true,"https://images.unsplash.com/photo-1585032226651-759b368d7246?w=400&q=80"),
                new("Main Course","Chowmein","Stir-fried noodles",160,true,"https://images.unsplash.com/photo-1569050467447-ce54b3bbc37d?w=400&q=80"),
                new("Main Course","Manchurian","Veg balls in spicy gravy",170,true,"https://images.unsplash.com/photo-1552611052-33e04de081de?w=400&q=80"),
                new("Starters","Spring Rolls","Crispy vegetable spring rolls",120,true,"https://images.unsplash.com/photo-1512058560541-628f32e927c3?w=400&q=80"),
                new("Soups","Sweet Corn Soup","Creamy corn vegetable soup",90,true,"https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400&q=80"),
            };
            var genericCafeMenu = new List<MenuSeed> {
                new("Beverages","Cappuccino","Rich Italian-style coffee",120,true,"https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?w=400&q=80"),
                new("Beverages","Cold Coffee","Blended iced coffee",110,true,"https://images.unsplash.com/photo-1513558161293-cdaf765ed2fd?w=400&q=80"),
                new("Snacks","Veg Sandwich","Fresh grilled vegetable sandwich",120,true,"https://images.unsplash.com/photo-1528735602780-2552fd46c7af?w=400&q=80"),
                new("Snacks","Loaded Fries","Crispy fries with toppings",130,true,"https://images.unsplash.com/photo-1573080496219-bb080dd4f877?w=400&q=80"),
                new("Desserts","Brownie","Warm chocolate fudge brownie",120,true,"https://images.unsplash.com/photo-1511381939415-e44015466834?w=400&q=80"),
            };
            var genericMultiMenu = new List<MenuSeed> {
                new("Starters","Veg Platter","Assorted vegetable starters",199,true,"https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=400&q=80"),
                new("Main Course","Paneer Masala","Cottage cheese in spiced gravy",200,true,"https://images.unsplash.com/photo-1585937421612-70a008356fbe?w=400&q=80"),
                new("Main Course","Veg Biryani","Aromatic vegetable biryani",180,true,"https://images.unsplash.com/photo-1516684732162-798a0062be99?w=400&q=80"),
                new("Breads","Butter Roti","Soft whole wheat roti",30,true,"https://images.unsplash.com/photo-1574673067736-1a27d3cee3bf?w=400&q=80"),
                new("Desserts","Gulab Jamun","Classic Indian sweet",60,true,"https://images.unsplash.com/photo-1551024506-0bccd828d307?w=400&q=80"),
            };

            // Assign generic menus to remaining restaurants
            var allRestaurants = new List<string> {
                "On The Wok chinese Food","The Pizza Empire Mathura","Prasadam Restaurant","Da Pizza Point",
                "Rowdy Cafe","Shri Bihari Ji Cafe","The Foodies Bar","Darshit Food Junction | Tiffin Service Mathura",
                "The Urban Terrace Restaurant","Punjabi Tadka Family Restaurant","Gangasagar Food Corner",
                "Gurjar Family Restaurant","Do bhai pizza wale","THE FOOD VILLA","TAISTEE CHINESE FOOD CORNER",
                "The Pizza Hub","Pizza Hut Dwarkapuri","Pizza Cut & Slice","Sitara by Lotus Grand",
                "Yadav Family Dhaba","D V Foods","The Trunk Rooftop","D V Caterers","Suraaj food",
                "What A Sandwich","आदर्श छोले भटूरे","The pizza point","Food Point","Ekyam Sattvic Kitchen",
                "italian hue","Sagar Ratna","Love wings cafe and restro","Brajbhog dhaba","Manhar Family Restaurant",
                "GARAM MASALA Food Station","THE BRAJ SPICY","Fat Tiger Mathura","Royal Grand Restaurent",
                "Hugs & Bite","Munch Box","Coco Chocolate Company","Bansal Restaurant","Brijveggies",
                "GREAT INDIAN FOOD AND PIZZA","Punjabi Rasoi","Cafe The Heaven"
            };

            foreach (var rName in allRestaurants)
            {
                var n = rName.ToLower();
                List<MenuSeed> seeds;
                if (n.Contains("pizza") || n.Contains("hue")) seeds = genericPizzaMenu;
                else if (n.Contains("chinese") || n.Contains("wok") || n.Contains("taistee")) seeds = genericChineseMenu;
                else if (n.Contains("cafe") || n.Contains("hugs") || n.Contains("coco") || n.Contains("munch") || n.Contains("foodies")) seeds = genericCafeMenu;
                else if (n.Contains("punjabi") || n.Contains("dhaba") || n.Contains("sitara") || n.Contains("yadav") || n.Contains("rasoi") || n.Contains("braj")) seeds = genericIndianMenu;
                else seeds = genericMultiMenu;

                if (!restaurantMenus.ContainsKey(rName))
                    restaurantMenus[rName] = seeds;
            }

            foreach (var (name, items) in restaurantMenus)
            {
                var restaurantId = GenerateGuid(name);
                if (context.Categories.Any(c => c.RestaurantId == restaurantId)) continue;

                var categories = new Dictionary<string, Guid>();
                foreach (var item in items)
                {
                    if (!categories.TryGetValue(item.Cat, out var catId))
                    {
                        catId = Guid.NewGuid();
                        categories[item.Cat] = catId;
                        context.Categories.Add(new MenuCategory {
                            CategoryId = catId, RestaurantId = restaurantId,
                            Name = item.Cat, Description = $"Our {item.Cat} selection."
                        });
                    }
                    context.Items.Add(new MenuItem {
                        ItemId = Guid.NewGuid(), RestaurantId = restaurantId, CategoryId = catId,
                        Name = item.Item, Description = item.Desc, Price = item.Price,
                        IsVeg = item.IsVeg, IsAvailable = true, ImageUrl = item.Img
                    });
                }
            }
            context.SaveChanges();
        }

        private static Guid GenerateGuid(string name)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            return new Guid(md5.ComputeHash(System.Text.Encoding.Default.GetBytes("QuickBite_" + name)));
        }
    }
}
