using BankingAPI.Contexts;
using BankingAPI.Models;
using BankingAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Tests
{
    public class CustomerRepositoryTest
    {
        private BankingContext _context;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<BankingContext>().UseInMemoryDatabase(databaseName: "BankingTestDb").Options;

            _context = new BankingContext(options);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context?.Dispose();
        }

        [Test]
        public async Task Create_Customer_Returns_Customer_With_Id()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "John Doe",
                Email = "john.doe@example.com",
                Phone = "123-456-7890",
                Status = "Active",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            // Act
            var result = await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            // Assert
            Assert.That(result.Entity, Is.Not.Null);
            Assert.That(result.Entity.Name, Is.EqualTo("John Doe"));
        }

        [Test]
        public async Task Get_Existing_Customer_Returns_Customer()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "Jane Doe",
                Email = "janedoe@gmail.com",
                Phone = "987-654-3210",
                Status = "Active",
                DateOfBirth = new DateTime(1992, 2, 2)
            };
            var created = await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();

            var result = await _context.Customers.FindAsync(created.Entity.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Email, Is.EqualTo("janedoe@gmail.com"));
        }

        [Test]
        public async Task Return_All_Customers()
        {
            var customer1 = new Customer
            {
                Name = "Alice Smith",
                Email = "alice.smith@example.com",
                Phone = "555-123-4567",
                Status = "Active",
                DateOfBirth = new DateTime(1985, 5, 15)
            };

            var customer2 = new Customer
            {
                Name = "Bob Johnson",
                Email = "bob.johnson@example.com",
                Phone = "555-987-6543",
                Status = "Active",
                DateOfBirth = new DateTime(1988, 8, 20)
            };

            await _context.Customers.AddAsync(customer1);
            await _context.Customers.AddAsync(customer2);
            await _context.SaveChangesAsync();

            var customers = await _context.Customers.ToListAsync();

            Assert.That(customers, Is.Not.Null);
            Assert.That(customers.Count, Is.EqualTo(2));

        }
    }
}