using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.Menu.DTOs;
using QuickBite.Menu.Interfaces;
using System.Security.Claims;

namespace QuickBite.Menu.Controllers
{
    [ApiController]
    [Route("api/v1/menu")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpGet("{restaurantId}")]
        public async Task<IActionResult> GetMenu(Guid restaurantId)
        {
            var result = await _menuService.GetMenuAsync(restaurantId);
            return Ok(result);
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedMenu([FromQuery] Guid restaurantId, [FromQuery] string? cuisine = "Multi-Cuisine", [FromQuery] int dishCount = 50)
        {
            try
            {
                var ownerId = Guid.NewGuid();
                cuisine = cuisine?.ToLower() ?? "multi-cuisine";

                // 1. Create Categories
                var categories = new List<MenuCategoryResponseDto>();
                var categoryNames = new[] { "Main Course", "Starters", "Beverages", "Desserts", "Chef's Special" };
                
                foreach (var catName in categoryNames)
                {
                    var cat = await _menuService.AddCategoryAsync(ownerId, new AddCategoryDto(restaurantId, catName, $"Delicious {catName} items", 0));
                    categories.Add(cat);
                }

                // 2. Generate Items based on Cuisine
                var items = GenerateDishes(restaurantId, categories, cuisine, dishCount);

                foreach (var item in items)
                {
                    await _menuService.AddMenuItemAsync(ownerId, item);
                }

                return Ok(new { Message = $"{items.Count} items seeded successfully for {cuisine} restaurant {restaurantId}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Seeding failed", Error = ex.Message });
            }
        }

        private List<AddMenuItemDto> GenerateDishes(Guid restaurantId, List<MenuCategoryResponseDto> categories, string cuisine, int count)
        {
            var items = new List<AddMenuItemDto>();
            var random = new Random();

            var categoryMap = categories.ToDictionary(c => c.Name, c => c.CategoryId);

            // Item counts per category
            int starterCount = 10;
            int mainCount = 25;
            int drinkCount = 7;
            int dessertCount = 6;
            int specialCount = 2;

            var starterList = GetNamesByCuisine(cuisine, "Starters", starterCount, random);
            var mainList = GetNamesByCuisine(cuisine, "Main Course", mainCount, random);
            var drinkList = GetNamesByCuisine(cuisine, "Beverages", drinkCount, random);
            var dessertList = GetNamesByCuisine(cuisine, "Desserts", dessertCount, random);
            var specialList = GetNamesByCuisine(cuisine, "Chef's Special", specialCount, random);

            AddItems(items, restaurantId, categoryMap["Starters"], starterList, random, true);
            AddItems(items, restaurantId, categoryMap["Main Course"], mainList, random, false);
            AddItems(items, restaurantId, categoryMap["Beverages"], drinkList, random, true);
            AddItems(items, restaurantId, categoryMap["Desserts"], dessertList, random, true);
            AddItems(items, restaurantId, categoryMap["Chef's Special"], specialList, random, false);

            return items;
        }

        private void AddItems(List<AddMenuItemDto> items, Guid restaurantId, Guid categoryId, List<string> names, Random random, bool smallPrice)
        {
            foreach (var name in names)
            {
                // Ensure everything is marked as veg
                items.Add(new AddMenuItemDto {
                    RestaurantId = restaurantId,
                    CategoryId = categoryId,
                    Name = name,
                    Description = GetGenericDescription(name),
                    Price = smallPrice ? random.Next(40, 120) : random.Next(120, 350), // Reduced prices
                    IsVeg = true,
                    Calories = random.Next(80, 500),
                    ImageUrl = GetImageUrlForDish(name)
                });
            }
        }

        private string GetGenericDescription(string name)
        {
            return $"Freshly prepared {name} made with the finest ingredients and traditional techniques to ensure an authentic taste.";
        }

        private List<string> GetNamesByCuisine(string cuisine, string category, int count, Random random)
        {
            string[] pool = Array.Empty<string>();

            if (cuisine.Contains("indian") || cuisine.Contains("biryani") || cuisine.Contains("dhaba"))
            {
                pool = category switch {
                    "Starters" => new[] { "Paneer Tikka", "Hara Bhara Kabab", "Vegetable Samosa", "Onion Bhaji", "Aloo Tikki", "Dahi Ke Sholay", "Cocktail Samosa", "Paneer Pakora", "Tandoori Mushroom", "Veg Spring Rolls", "Crispy Corn", "Gobi 65" },
                    "Main Course" => new[] { "Paneer Butter Masala", "Dal Makhani", "Shahi Paneer", "Malai Kofta", "Mix Vegetable Curry", "Aloo Gobi Matar", "Palak Paneer", "Kadai Paneer", "Rajma Masala", "Chole Masala", "Dal Tadka", "Navratan Korma", "Jeera Aloo", "Baingan Bharta", "Dum Aloo", "Methi Matar Malai", "Kaju Curry", "Veg Kolhapuri" },
                    "Beverages" => new[] { "Masala Chai", "Sweet Lassi", "Salted Lassi", "Mango Lassi", "Cold Coffee", "Fresh Lime Soda", "Thandai", "Jal Jeera", "Butter Milk (Chaas)", "Rose Milk", "Badam Milk", "Iced Tea" },
                    "Desserts" => new[] { "Gulab Jamun", "Rasmalai", "Gajar Ka Halwa", "Kheer", "Kulfi", "Moong Dal Halwa", "Jalebi with Rabri", "Rasgulla", "Mishti Doi", "Shahi Tukda" },
                    "Chef's Special" => new[] { "Royal Thali Special", "Chef's Signature Veg Biryani", "Smoked Dal Bukhara" },
                    _ => new[] { "Signature Veg Dish" }
                };
            }
            else if (cuisine.Contains("pizza") || cuisine.Contains("italian") || cuisine.Contains("pasta"))
            {
                pool = category switch {
                    "Starters" => new[] { "Garlic Bread with Cheese", "Bruschetta", "Mozzarella Sticks", "Stuffed Mushrooms", "Caprese Salad", "Pesto Crostini", "Arancini Balls", "Focaccia Bread", "Minestrone Soup" },
                    "Main Course" => new[] { "Margherita Pizza", "Farmhouse Veggie Pizza", "Quattro Formaggi Pizza", "Penne Arrabbiata", "Fettuccine Alfredo", "Mushroom Risotto", "Spaghetti Aglio e Olio", "Ravioli Spinach & Ricotta", "Gnocchi Sorrentina", "Vegetable Calzone", "Truffle Oil Pasta" },
                    "Beverages" => new[] { "Italian Lemonade", "Sparkling Water", "Espresso", "Cappuccino", "Peach Iced Tea", "Aperol Spritz (Mocktail)", "Fruit Sangria (Mocktail)", "San Pellegrino", "Affogato", "Italian Soda" },
                    "Desserts" => new[] { "Tiramisu", "Panna Cotta", "Cannoli", "Gelato Trio", "Chocolate Fondant", "Ricotta Cheesecake", "Torta della Nonna", "Zuppa Inglese" },
                    "Chef's Special" => new[] { "Truffle Mushroom Pizza", "Gold Leaf Margherita" },
                    _ => new[] { "Chef's Veg Selection" }
                };
            }
            else if (cuisine.Contains("chinese") || cuisine.Contains("asian") || cuisine.Contains("thai"))
            {
                pool = category switch {
                    "Starters" => new[] { "Veg Spring Rolls", "Veg Momos", "Crispy Honey Chilli Potato", "Dim Sum Basket", "Veg Manchurian Dry", "Kimchi Salad", "Hot & Sour Soup", "Manchow Soup" },
                    "Main Course" => new[] { "Veg Hakka Noodles", "Veg Fried Rice", "Chilli Paneer Gravy", "Veg Manchurian Gravy", "Schezwan Noodles", "Thai Green Curry (Veg)", "Thai Red Curry (Veg)", "Pad Thai (Veg)", "Stir Fry Exotic Vegetables", "Mapo Tofu" },
                    "Beverages" => new[] { "Jasmine Tea", "Thai Iced Tea", "Bubble Milk Tea", "Green Tea", "Lychee Martini (Mocktail)", "Japanese Matcha", "Iced Peach Soda", "Mango Sticky Rice Shake", "Coconut Water", "Fruit Punch" },
                    "Desserts" => new[] { "Fried Ice Cream", "Darsaan with Vanilla Ice Cream", "Mango Sticky Rice", "Matcha Cheesecake", "Date Pancakes", "Lychees with Ice Cream", "Red Bean Buns", "Mochi Ice Cream" },
                    "Chef's Special" => new[] { "Chef's Signature Ramen (Veg)", "Spicy Dragon Veg Sushi Platter" },
                    _ => new[] { "Oriental Veg Delight" }
                };
            }
            else
            {
                pool = category switch {
                    "Starters" => new[] { "Garden Salad", "Nachos Supreme (Veg)", "French Fries", "Onion Rings", "Potato Skins", "Soup of the Day", "Cheese Quesadilla" },
                    "Main Course" => new[] { "Veggie Burger", "Pasta Primavera", "Falafel Wrap", "Mushroom Stroganoff", "Veggie Burrito", "Classic Grilled Cheese" },
                    "Beverages" => new[] { "Fresh Orange Juice", "Coke", "Pepsi", "Lemonade", "Iced Coffee", "Smoothie", "Mineral Water", "Milkshake", "Root Beer Float", "Ginger Ale" },
                    "Desserts" => new[] { "Apple Pie", "Chocolate Cake", "Ice Cream Sundae", "New York Cheesecake", "Brownie with Ice Cream", "Fruit Salad", "Lemon Tart", "Banoffee Pie" },
                    "Chef's Special" => new[] { "Truffle Mac and Cheese", "Chef's Garden Special Thali" },
                    _ => new[] { "House Veg Special" }
                };
            }

            // Shuffle and pick
            var list = pool.OrderBy(x => random.Next()).ToList();
            var finalNames = new List<string>();
            
            for (int i = 0; i < count; i++)
            {
                // If pool is smaller than requested count, we might have to reuse with a variation
                var name = list[i % list.Count];
                if (i >= list.Count)
                {
                    var suffix = (i / list.Count) + 1;
                    name = $"{name} (Extra {suffix})";
                }
                finalNames.Add(name);
            }

            return finalNames;
        }

        private string GetImageUrlForDish(string name)
        {
            var n = name.ToLower();
            if (n.Contains("margherita") || n.Contains("cheese pizza") || n.Contains("veg pizza")) return "https://images.unsplash.com/photo-1574071318508-1cdbad80ad38?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("paneer")) return "https://images.unsplash.com/photo-1567188040759-fb8a883dc6d8?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("dal") || n.Contains("lentil")) return "https://images.unsplash.com/photo-1546833999-b9f581a1996d?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("burger")) return "https://images.unsplash.com/photo-1550547660-d9450f859349?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("sandwich") || n.Contains("toast")) return "https://images.unsplash.com/photo-1528735602780-2552fd46c7af?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("pasta") || n.Contains("spaghetti") || n.Contains("penne")) return "https://images.unsplash.com/photo-1473093226795-af9932fe5856?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("noodles") || n.Contains("hakka") || n.Contains("chow mein")) return "https://images.unsplash.com/photo-1585032226651-759b368d7246?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("biryani") || n.Contains("pulao")) return "https://images.unsplash.com/photo-1589302168068-964664d93dc0?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("momos") || n.Contains("dim sum")) return "https://images.unsplash.com/photo-1534422298391-e4f8c170db76?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("sushi") || n.Contains("rolls")) return "https://images.unsplash.com/photo-1579871494447-9811cf80d66c?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("salad") || n.Contains("bowl")) return "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("fries") || n.Contains("potato") || n.Contains("nachos")) return "https://images.unsplash.com/photo-1573016608244-7d5cf347ed70?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("cake") || n.Contains("tiramisu") || n.Contains("cheesecake")) return "https://images.unsplash.com/photo-1565958011703-44f9829ba187?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("ice cream") || n.Contains("gelato") || n.Contains("kulfi")) return "https://images.unsplash.com/photo-1501443762994-82bd5dabb892?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("gulab jamun") || n.Contains("dessert") || n.Contains("rasmalai")) return "https://images.unsplash.com/photo-1589119908995-c6837fa14848?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("chai") || n.Contains("tea") || n.Contains("coffee")) return "https://images.unsplash.com/photo-1544145945-f904253d0c71?auto=format&fit=crop&w=600&q=80";
            if (n.Contains("lassi") || n.Contains("shake") || n.Contains("drink") || n.Contains("juice") || n.Contains("soda")) return "https://images.unsplash.com/photo-1553530666-ba11a7da3888?auto=format&fit=crop&w=600&q=80";
            
            return "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=600&q=80";
        }

        [HttpGet("{restaurantId}/search")]
        public async Task<IActionResult> Search(Guid restaurantId, [FromQuery] string name)
        {
            var result = await _menuService.SearchItemsAsync(restaurantId, name);
            return Ok(result);
        }

        [HttpGet("{restaurantId}/veg")]
        public async Task<IActionResult> GetVegItems(Guid restaurantId)
        {
            var result = await _menuService.GetVegItemsAsync(restaurantId);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPost("categories")]
        public async Task<IActionResult> AddCategory([FromBody] AddCategoryDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.AddCategoryAsync(ownerId, dto);
            return CreatedAtAction(nameof(GetMenu), new { restaurantId = dto.RestaurantId }, result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] AddCategoryDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _menuService.UpdateCategoryAsync(ownerId, id, dto);
            return Ok(new { Message = "Category updated" });
        }

        [Authorize(Roles = "OWNER")]
        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _menuService.DeleteCategoryAsync(ownerId, id);
            return Ok(new { Message = "Category deleted" });
        }

        [Authorize(Roles = "OWNER")]
        [HttpPost("items")]
        public async Task<IActionResult> AddMenuItem([FromBody] AddMenuItemDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.AddMenuItemAsync(ownerId, dto);
            return CreatedAtAction(nameof(GetMenu), new { restaurantId = dto.RestaurantId }, result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPut("items/{id}")]
        public async Task<IActionResult> UpdateMenuItem(Guid id, [FromBody] UpdateMenuItemDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.UpdateMenuItemAsync(ownerId, id, dto);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPut("items/{id}/toggle")]
        public async Task<IActionResult> ToggleAvailability(Guid id)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _menuService.ToggleItemAvailabilityAsync(ownerId, id);
            return Ok(new { Message = "Availability toggled" });
        }

        [Authorize(Roles = "OWNER")]
        [HttpDelete("items/{id}")]
        public async Task<IActionResult> DeleteMenuItem(Guid id)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _menuService.DeleteMenuItemAsync(ownerId, id);
            return Ok(new { Message = "Item deleted" });
        }

        // --- Item Review Endpoints ---

        [Authorize]
        [HttpPost("items/{itemId}/reviews")]
        public async Task<IActionResult> SubmitReview(Guid itemId, [FromBody] SubmitMenuItemReviewDto dto)
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.SubmitItemReviewAsync(itemId, customerId, dto);
            return CreatedAtAction(nameof(GetItemReviews), new { itemId = itemId }, result);
        }

        [HttpGet("items/{itemId}/reviews")]
        public async Task<IActionResult> GetItemReviews(Guid itemId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _menuService.GetItemReviewsAsync(itemId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("items/{itemId}/reviews/avg")]
        public async Task<IActionResult> GetAvgRating(Guid itemId)
        {
            var result = await _menuService.GetAvgItemRatingAsync(itemId);
            return Ok(new { AverageRating = result });
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("items/reviews/{reviewId}")]
        public async Task<IActionResult> ModerateReview(Guid reviewId)
        {
            await _menuService.ModerateItemReviewAsync(reviewId);
            return Ok(new { Message = "Review moderated/deleted" });
        }
    }
}
