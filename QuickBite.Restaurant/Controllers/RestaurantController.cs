using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuickBite.Restaurant.DTOs;
using QuickBite.Restaurant.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace QuickBite.Restaurant.Controllers
{
    [ApiController]
    [Route("api/v1/restaurants")]
    public class RestaurantController : ControllerBase
    {
        private readonly IRestaurantService _restaurantService;
        private readonly IGooglePlacesService _placesService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;

        public RestaurantController(IRestaurantService restaurantService, IGooglePlacesService placesService, HttpClient httpClient, IConfiguration configuration, IDistributedCache cache)
        {
            _restaurantService = restaurantService;
            _placesService = placesService;
            _httpClient = httpClient;
            _configuration = configuration;
            _cache = cache;
        }

        [Authorize(Roles = "OWNER")]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRestaurantDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _restaurantService.RegisterRestaurantAsync(ownerId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("mock-data")]
        public async Task<IActionResult> ClearMockData()
        {
            var all = await _restaurantService.GetNearbyRestaurantsAsync(0, 0, 100000); // Get all
            int count = 0;
            foreach (var r in all)
            {
                // Identify mock data by the "Name Index" pattern or "Automatically seeded" description
                if (System.Text.RegularExpressions.Regex.IsMatch(r.Name, @"\s\d+$") || r.Description.Contains("Automatically seeded"))
                {
                    await _restaurantService.DeleteRestaurantAsync(r.Id);
                    count++;
                }
            }
            return Ok(new { Message = $"Cleaned up {count} mock restaurants.", Count = count });
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedNearby([FromQuery] double lat, [FromQuery] double lon)
        {
            try
            {
                // 1. Fetch real restaurants from Google Places (50km Wide Search)
                var placesResult = await _placesService.GetNearbyRestaurantsAsync(lat, lon, 50000); 
                var nearbyRestaurants = placesResult.Restaurants;
                
                if (placesResult.Status != "OK") throw new Exception(placesResult.ErrorMessage);

                var ownerId = Guid.NewGuid();
                var seededIds = new List<Guid>();

                foreach (var dto in nearbyRestaurants)
                {
                    // Strict Duplicate Check: Name + Address
                    // Strict Duplicate Check: PlaceId or Name + Address
                    if (await _restaurantService.IsDuplicateAsync(dto.Name, dto.Address, dto.PlaceId)) continue;

                    // Register restaurant locally
                    var result = await _restaurantService.RegisterRestaurantAsync(ownerId, dto);
                    await _restaurantService.ApproveRestaurantAsync(result.Id);
                    await _restaurantService.ToggleRestaurantStatusAsync(result.Id, ownerId);
                    
                    seededIds.Add(result.Id);

                    // Seed Menu items
                    try
                    {
                        var menuSeedUrl = $"http://menu-service:8080/api/v1/menu/seed?restaurantId={result.Id}&cuisine={Uri.EscapeDataString(dto.Cuisine)}&dishCount=50";
                        await _httpClient.PostAsync(menuSeedUrl, null);
                    }
                    catch { }
                }
                
                // Force invalidate all potential nearby caches for this general area
                try {
                    var rLat = Math.Round(lat, 3);
                    var rLon = Math.Round(lon, 3);
                    await _cache.RemoveAsync($"restaurants_nearby_{rLat}_{rLon}_15.0");
                    await _cache.RemoveAsync($"restaurants_nearby_{rLat}_{rLon}_30.0");
                    await _cache.RemoveAsync($"restaurants_nearby_{rLat}_{rLon}_50.0");
                } catch { }

                return Ok(new { 
                    Message = $"{seededIds.Count} new real restaurants discovered!",
                    Count = seededIds.Count,
                    TotalInDb = (await _restaurantService.GetAllRestaurantsAsync()).Count()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Seeding failed", Error = ex.Message });
            }
        }

        [HttpPost("bulk-seed")]
        public async Task<IActionResult> BulkSeed([FromQuery] double lat, [FromQuery] double lon)
        {
            try
            {
                var ownerId = Guid.NewGuid();
                // Pass lat/lon to generator to seed around user
                var restaurants = Helpers.DataGenerator.GenerateRestaurantsAroundLocation(ownerId, lat, lon);
                var seedTasks = new List<Task>();
                var registeredDtos = new List<RegisterRestaurantDto>();
                
                foreach(var r in restaurants)
                {
                    // Check if already exists to prevent duplicates
                    if (await _restaurantService.IsDuplicateAsync(r.Name, r.Address, null))
                        continue;

                    var dto = new DTOs.RegisterRestaurantDto {
                        Name = r.Name,
                        Cuisine = r.Cuisine,
                        Address = r.Address,
                        City = r.City,
                        Latitude = r.Latitude,
                        Longitude = r.Longitude,
                        Phone = r.Phone,
                        MinOrderAmount = r.MinOrderAmount,
                        EstimatedDeliveryMin = r.EstimatedDeliveryMin
                    };
                    
                    var result = await _restaurantService.RegisterRestaurantAsync(ownerId, dto);
                    await _restaurantService.ApproveRestaurantAsync(result.Id);
                    await _restaurantService.ToggleRestaurantStatusAsync(result.Id, ownerId);
                    
                    // Add to a list for travel time enrichment
                    var registeredDto = new RegisterRestaurantDto {
                        Id = result.Id,
                        Name = dto.Name,
                        Latitude = dto.Latitude,
                        Longitude = dto.Longitude
                    };
                    registeredDtos.Add(registeredDto);

                    var menuSeedUrl = $"http://menu-service:8080/api/v1/menu/seed?restaurantId={result.Id}&cuisine={Uri.EscapeDataString(dto.Cuisine)}&dishCount=50";
                    seedTasks.Add(_httpClient.PostAsync(menuSeedUrl, null));
                }

                // NEW: Enrich these 50 mock restaurants with REAL travel times from Google
                if (registeredDtos.Any())
                {
                    await _placesService.EnrichWithTravelTimesAsync(lat, lon, registeredDtos);
                    // Update the DB with new travel times
                    foreach(var r in registeredDtos)
                    {
                        await _restaurantService.UpdateTravelTimeAsync(r.Id, r.EstimatedDeliveryMin);
                    }
                }

                await Task.WhenAll(seedTasks);

                // Invalidate cache for this location
                try
                {
                    var roundedLat = Math.Round(lat, 3);
                    var roundedLon = Math.Round(lon, 3);
                    var radius = 15.0; // Frontend default radius is 15.0
                    var cacheKey = $"restaurants_nearby_{roundedLat}_{roundedLon}_{radius}";
                    await _cache.RemoveAsync(cacheKey);
                }
                catch { }

                return Ok(new { Message = "50 restaurants successfully seeded around your location!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Bulk seeding failed", Error = ex.Message });
            }
        }

        [Authorize(Roles = "OWNER")]
        [HttpPost("seed-my-outlets")]
        public async Task<IActionResult> SeedForMe([FromQuery] double lat = 12.9716, [FromQuery] double lon = 77.5946)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var restaurants = Helpers.DataGenerator.GenerateRestaurantsAroundLocation(ownerId, lat, lon).Take(5);
            var seededIds = new List<Guid>();

            foreach (var r in restaurants)
            {
                var dto = new RegisterRestaurantDto {
                    Name = r.Name + " (My Outlet)",
                    Cuisine = r.Cuisine,
                    Address = r.Address,
                    City = r.City,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    Phone = r.Phone,
                    MinOrderAmount = r.MinOrderAmount,
                    EstimatedDeliveryMin = r.EstimatedDeliveryMin
                };
                
                var result = await _restaurantService.RegisterRestaurantAsync(ownerId, dto);
                await _restaurantService.ApproveRestaurantAsync(result.Id);
                await _restaurantService.ToggleRestaurantStatusAsync(result.Id, ownerId);
                
                seededIds.Add(result.Id);

                // Seed some menu items and ORDERS for this restaurant
                try
                {
                    var menuSeedUrl = $"http://menu-service:8080/api/v1/menu/seed?restaurantId={result.Id}&cuisine={Uri.EscapeDataString(dto.Cuisine)}&dishCount=20";
                    await _httpClient.PostAsync(menuSeedUrl, null);

                    // Seed some dummy orders via the Order service
                    var orderSeedUrl = $"http://order-service:8080/api/v1/orders/seed?restaurantId={result.Id}&count=5";
                    await _httpClient.PostAsync(orderSeedUrl, null);
                }
                catch { }
            }

            return Ok(new { Message = "5 outlets seeded for you!", Count = seededIds.Count });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _restaurantService.GetRestaurantByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpGet("nearby")]
        public async Task<IActionResult> GetNearby([FromQuery] double lat, [FromQuery] double lon, [FromQuery] double radius = 5.0)
        {
            var result = await _restaurantService.GetNearbyRestaurantsAsync(lat, lon, radius);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string name)
        {
            var result = await _restaurantService.SearchRestaurantsAsync(name);
            return Ok(result);
        }

        [HttpGet("cuisine/{cuisine}")]
        public async Task<IActionResult> FilterByCuisine(string cuisine)
        {
            var result = await _restaurantService.GetRestaurantsByCuisineAsync(cuisine);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpGet("my-restaurants")]
        public async Task<IActionResult> GetMyRestaurants()
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _restaurantService.GetRestaurantsByOwnerAsync(ownerId);
            return Ok(result);
        }

        [Authorize(Roles = "OWNER")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRestaurantDto dto)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var result = await _restaurantService.UpdateRestaurantAsync(id, ownerId, dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("admin-fetch-all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _restaurantService.GetAllRestaurantsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var result = await _restaurantService.GetPendingApprovalsAsync();
            return Ok(result);
        }

        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id)
        {
            await _restaurantService.ApproveRestaurantAsync(id);
            return Ok(new { Message = "Restaurant approved successfully" });
        }

        [Authorize(Roles = "OWNER")]
        [HttpPut("{id}/toggle")]
        public async Task<IActionResult> Toggle(Guid id)
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                await _restaurantService.ToggleRestaurantStatusAsync(id, ownerId);
                return Ok(new { Message = "Restaurant status toggled" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _restaurantService.DeleteRestaurantAsync(id);
            return Ok(new { Message = "Restaurant deleted" });
        }

        // --- Review Endpoints ---

        [HttpPost("{id}/reviews")]
        [Authorize]
        public async Task<IActionResult> SubmitReview(Guid id, [FromBody] AddReviewDto dto)
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _restaurantService.SubmitReviewAsync(id, customerId, dto);
                return CreatedAtAction(nameof(GetReviews), new { id = id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpGet("{id}/reviews")]
        public async Task<IActionResult> GetReviews(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _restaurantService.GetReviewsAsync(id, page, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}/reviews/avg")]
        public async Task<IActionResult> GetAverageRating(Guid id)
        {
            var result = await _restaurantService.GetAvgRatingAsync(id);
            return Ok(new { AverageRating = result });
        }

        [HttpDelete("reviews/{reviewId}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteReview(Guid reviewId)
        {
            await _restaurantService.DeleteReviewAsync(reviewId);
            return Ok(new { Message = "Review moderated/deleted" });
        }
    }
}
