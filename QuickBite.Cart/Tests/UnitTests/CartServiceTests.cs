using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using NUnit.Framework;
using QuickBite.Cart.DTOs;
using QuickBite.Cart.Entities;
using QuickBite.Cart.Interfaces;
using QuickBite.Cart.Services;

namespace QuickBite.Cart.Tests.UnitTests
{
    [TestFixture]
    public class CartServiceTests
    {
        private Mock<ICartRepository> _repositoryMock;
        private Mock<IDistributedCache> _cacheMock;
        private CartService _cartService;

        [SetUp]
        public void Setup()
        {
            _repositoryMock = new Mock<ICartRepository>();
            _cacheMock = new Mock<IDistributedCache>();
            _cartService = new CartService(_repositoryMock.Object, _cacheMock.Object);
        }

        [Test]
        public async Task AddToCartAsync_ShouldThrow_WhenRestaurantIdMismatch()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var existingRestaurantId = Guid.NewGuid();
            var newRestaurantId = Guid.NewGuid();
            
            var existingCart = new Entities.Cart
            {
                CartId = Guid.NewGuid(),
                CustomerId = customerId,
                RestaurantId = existingRestaurantId,
                Items = new List<CartItem> { new CartItem { ItemId = Guid.NewGuid(), Price = 100, Quantity = 1 } }
            };

            _repositoryMock.Setup(r => r.GetCartByCustomerIdAsync(customerId)).ReturnsAsync(existingCart);
            
            var dto = new AddToCartDto(newRestaurantId, Guid.NewGuid(), "Burger", 150, 1, null);

            // Act
            Func<Task> act = async () => await _cartService.AddToCartAsync(customerId, dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("You can only add items from one restaurant at a time. Clear your cart or switch restaurant.");
        }

        [Test]
        public async Task AddToCartAsync_ShouldAddItem_WhenCartIsEmpty()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var restaurantId = Guid.NewGuid();
            var menuItemId = Guid.NewGuid();

            _repositoryMock.Setup(r => r.GetCartByCustomerIdAsync(customerId)).ReturnsAsync((Entities.Cart)null);
            
            var dto = new AddToCartDto(restaurantId, menuItemId, "Pizza", 500, 2, "Extra Cheese");

            // Act
            var result = await _cartService.AddToCartAsync(customerId, dto);

            // Assert
            result.RestaurantId.Should().Be(restaurantId);
            result.Items.Should().HaveCount(1);
            result.Items[0].Name.Should().Be("Pizza");
            result.GrandTotal.Should().Be(1000);
            
            _repositoryMock.Verify(r => r.AddCartAsync(It.IsAny<Entities.Cart>()), Times.Once);
            _repositoryMock.Verify(r => r.AddCartItemAsync(It.IsAny<CartItem>()), Times.Once);
        }

        [Test]
        public async Task RemoveItemAsync_ShouldDecreaseGrandTotal()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var cart = new Entities.Cart
            {
                CartId = Guid.NewGuid(),
                CustomerId = customerId,
                Items = new List<CartItem> { 
                    new CartItem { ItemId = itemId, Price = 100, Quantity = 2 },
                    new CartItem { ItemId = Guid.NewGuid(), Price = 50, Quantity = 1 }
                }
            };

            _repositoryMock.Setup(r => r.GetCartByCustomerIdAsync(customerId)).ReturnsAsync(cart);

            // Act
            var result = await _cartService.RemoveItemAsync(customerId, itemId);

            // Assert
            result.Items.Should().HaveCount(1);
            result.GrandTotal.Should().Be(50);
            _repositoryMock.Verify(r => r.DeleteCartItemAsync(It.IsAny<CartItem>()), Times.Once);
        }

        [Test]
        public async Task ClearCartAsync_ShouldEmptyTheCart()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var cart = new Entities.Cart
            {
                CartId = Guid.NewGuid(),
                CustomerId = customerId,
                Items = new List<CartItem> { new CartItem { Price = 100, Quantity = 1 } }
            };

            _repositoryMock.Setup(r => r.GetCartByCustomerIdAsync(customerId)).ReturnsAsync(cart);

            // Act
            await _cartService.ClearCartAsync(customerId);

            // Assert
            cart.Items.Should().BeEmpty();
            cart.GrandTotal.Should().Be(0);
            _repositoryMock.Verify(r => r.ClearCartItemsAsync(cart.CartId), Times.Once);
        }
    }
}
