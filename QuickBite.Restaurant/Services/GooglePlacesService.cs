using QuickBite.Restaurant.DTOs;
using QuickBite.Restaurant.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace QuickBite.Restaurant.Services
{
    public class GooglePlacesService : IGooglePlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly IDistributedCache _cache;
        private readonly string? _apiKey;

        public GooglePlacesService(HttpClient httpClient, IConfiguration configuration, IDistributedCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
            _apiKey = configuration["GoogleMaps:ApiKey"];
        }

        public async Task<GooglePlacesResult> GetNearbyRestaurantsAsync(double lat, double lon, double radius)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.StartsWith("YOUR_"))
                return new GooglePlacesResult(new List<RegisterRestaurantDto>(), "CONFIG_ERROR", "Invalid API Key");

            try
            {
                var tasks = new List<Task<List<RegisterRestaurantDto>>>();
                
                double localRadius = 5000; // 5km search bubble per task
                
                // Specific queries for delivery partners
                tasks.Add(FetchFromGoogleAsync(lat, lon, localRadius, "top rated restaurants on zomato"));
                tasks.Add(FetchFromGoogleAsync(lat, lon, localRadius, "popular restaurants on swiggy"));
                tasks.Add(FetchFromGoogleAsync(lat, lon, localRadius, "best delivery restaurants"));
                tasks.Add(FetchFromGoogleAsync(lat, lon, localRadius, "highly rated food delivery"));
                
                // Add some category-specific delivery searches
                tasks.Add(FetchFromGoogleAsync(lat, lon, localRadius, "pizza delivery zomato"));
                tasks.Add(FetchFromGoogleAsync(lat, lon, localRadius, "biryani delivery swiggy"));
                tasks.Add(FetchFromGoogleAsync(lat, lon, localRadius, "north indian delivery zomato"));
                tasks.Add(FetchFromGoogleAsync(lat, lon, localRadius, "chinese delivery swiggy"));

                var results = await Task.WhenAll(tasks);

                var allFound = results.SelectMany(x => x)
                    .Where(x => x.Rating >= 4.0) // Filter for high ratings
                    .GroupBy(x => x.Name + x.Address) // Simple deduplication
                    .Select(g => g.First())
                    .ToList();

                return new GooglePlacesResult(allFound, "OK", $"Successfully found {allFound.Count} unique high-rated delivery restaurants.");
            }
            catch (Exception ex)
            {
                return new GooglePlacesResult(new List<RegisterRestaurantDto>(), "API_ERROR", ex.Message);
            }
        }

        public async Task EnrichWithTravelTimesAsync(double originLat, double originLon, List<RegisterRestaurantDto> restaurants)
        {
            try
            {
                // Distance Matrix limit is 25 destinations per call
                var batches = restaurants.Chunk(25);
                foreach (var batch in batches)
                {
                    var destinations = string.Join("|", batch.Select(r => $"{r.Latitude},{r.Longitude}"));
                    var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={originLat},{originLon}&destinations={destinations}&key={_apiKey}";
                    
                    var response = await _httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode) continue;

                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    var rows = doc.RootElement.GetProperty("rows")[0].GetProperty("elements");

                    for (int i = 0; i < batch.Length; i++)
                    {
                        var element = rows[i];
                        if (element.GetProperty("status").GetString() == "OK")
                        {
                            var durationMinutes = element.GetProperty("duration").GetProperty("value").GetInt32() / 60;
                            // Update travel time (add 10-15 mins for preparation/buffer)
                            batch[i].EstimatedDeliveryMin = durationMinutes + 15;
                        }
                    }
                }
            }
            catch { /* Fallback to defaults on error */ }
        }

        private async Task<List<RegisterRestaurantDto>> FetchFromGoogleAsync(double lat, double lon, double radius, string query)
        {
            var url = "https://places.googleapis.com/v1/places:searchText";
            var requestBody = new
            {
                textQuery = query,
                maxResultCount = 20,
                locationBias = new { circle = new { center = new { latitude = lat, longitude = lon }, radius = radius } }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-Goog-Api-Key", _apiKey);
            request.Headers.Add("X-Goog-FieldMask", "places.id,places.name,places.displayName,places.formattedAddress,places.location,places.types,places.photos,places.rating");
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) 
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Google API Error: {response.StatusCode} - {error}");
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var list = new List<RegisterRestaurantDto>();
            if (root.TryGetProperty("places", out var places))
            {
                foreach (var item in places.EnumerateArray())
                {
                    var location = item.GetProperty("location");
                    var types = item.TryGetProperty("types", out var t) ? t.EnumerateArray().Select(x => x.GetString()).ToList() : new List<string?>();
                    
                    var cuisineList = new List<string>();
                    if (types.Contains("pizza_restaurant")) cuisineList.Add("Pizza");
                    if (types.Contains("hamburger_restaurant")) cuisineList.Add("Burgers");
                    if (types.Contains("bakery") || types.Contains("dessert_shop")) cuisineList.Add("Desserts");
                    if (types.Contains("cafe") || types.Contains("coffee_shop")) cuisineList.Add("Beverages");
                    if (types.Contains("chinese_restaurant")) cuisineList.Add("Chinese");
                    if (types.Contains("indian_restaurant")) cuisineList.Add("Indian");
                    if (cuisineList.Count == 0) cuisineList.Add("Multi-Cuisine");

                    var photoReference = item.TryGetProperty("photos", out var photos) && photos.GetArrayLength() > 0 ? photos[0].GetProperty("name").GetString() : null;
                    var imageUrl = !string.IsNullOrEmpty(photoReference) 
                        ? $"https://places.googleapis.com/v1/{photoReference}/media?key={_apiKey}&maxHeightPx=400"
                        : "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&q=80&w=800";

                    list.Add(new RegisterRestaurantDto
                    {
                        Name = item.GetProperty("displayName").GetProperty("text").GetString() ?? "Unknown",
                        Description = "Real-time travel updates enabled",
                        Cuisine = string.Join(", ", cuisineList),
                        Address = item.TryGetProperty("formattedAddress", out var addr) ? addr.GetString() ?? "" : "",
                        Latitude = location.GetProperty("latitude").GetDouble(),
                        Longitude = location.GetProperty("longitude").GetDouble(),
                        City = "Auto Detected",
                        Phone = "Not Provided",
                        MinOrderAmount = 150,
                        EstimatedDeliveryMin = 30, // Initial default
                        ImageUrl = imageUrl,
                        PlaceId = item.GetProperty("name").GetString(),
                        Rating = item.TryGetProperty("rating", out var r) ? (float)r.GetDouble() : 4.2f
                    });
                }
            }
            return list;
        }
    }
}
