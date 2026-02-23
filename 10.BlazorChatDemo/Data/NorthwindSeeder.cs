using BlazorChatDemo.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlazorChatDemo.Data;

public static class NorthwindSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NorthwindDbContext>();

        await db.Database.EnsureCreatedAsync();

        if (await db.Customers.AnyAsync())
            return; // already seeded

        var random = new Random(12345);

        var regions = CreateRegions(db);
        var territories = CreateTerritories(db, regions);
        var employees = CreateEmployees(db, territories);
        var categories = CreateCategories(db);
        var suppliers = CreateSuppliers(db);
        var products = CreateProducts(db, categories, suppliers, random);
        var customers = CreateCustomers(db, random);
        var shippers = CreateShippers(db, random);
        var orders = CreateOrders(db, customers, employees, shippers, products, random);
        CreateInvoices(db, orders, random);

        await db.SaveChangesAsync();
    }

    private static List<Region> CreateRegions(NorthwindDbContext db)
    {
        var names = new[] { "North", "South", "East", "West" };
        var regions = names.Select(n => new Region { Id = Guid.NewGuid(), Name = n }).ToList();
        db.Regions.AddRange(regions);
        return regions;
    }

    private static List<Territory> CreateTerritories(NorthwindDbContext db, List<Region> regions)
    {
        var data = new (string Name, int RegionIndex)[]
        {
            ("Seattle", 0), ("Portland", 0), ("Spokane", 0),
            ("Los Angeles", 1), ("San Diego", 1), ("Phoenix", 1),
            ("Denver", 2), ("Dallas", 2), ("Houston", 2),
            ("Chicago", 3), ("Detroit", 3), ("Minneapolis", 3)
        };

        var territories = data.Select(d => new Territory
        {
            Id = Guid.NewGuid(),
            Name = d.Name,
            Region = regions[d.RegionIndex]
        }).ToList();

        db.Territories.AddRange(territories);
        return territories;
    }

    private static List<Employee> CreateEmployees(NorthwindDbContext db, List<Territory> territories)
    {
        var data = new[]
        {
            new { First = "Nancy", Last = "Davolio", Title = "Sales Manager", Hire = new DateTime(2020, 1, 5), Email = "nancy@example.com", Phone = "555-0100" },
            new { First = "Andrew", Last = "Fuller", Title = "Senior Sales", Hire = new DateTime(2021, 3, 12), Email = "andrew@example.com", Phone = "555-0101" },
            new { First = "Janet", Last = "Leverling", Title = "Sales Representative", Hire = new DateTime(2022, 5, 20), Email = "janet@example.com", Phone = "555-0102" },
            new { First = "Margaret", Last = "Peacock", Title = "Sales Representative", Hire = new DateTime(2023, 2, 10), Email = "margaret@example.com", Phone = "555-0103" },
            new { First = "Steven", Last = "Buchanan", Title = "Sales Associate", Hire = new DateTime(2024, 7, 1), Email = "steven@example.com", Phone = "555-0104" }
        };

        var employees = data.Select(d => new Employee
        {
            Id = Guid.NewGuid(),
            FirstName = d.First,
            LastName = d.Last,
            Title = d.Title,
            HireDate = d.Hire,
            Email = d.Email,
            Phone = d.Phone
        }).ToList();

        // Reporting hierarchy
        employees[1].ReportsTo = employees[0];
        employees[2].ReportsTo = employees[0];
        employees[3].ReportsTo = employees[1];
        employees[4].ReportsTo = employees[2];

        db.Employees.AddRange(employees);

        // Territory assignments
        var assignments = new Dictionary<int, int[]>
        {
            { 0, new[] { 0, 1, 2 } },
            { 1, new[] { 3, 4 } },
            { 2, new[] { 5, 6 } },
            { 3, new[] { 7, 8 } },
            { 4, new[] { 9, 10, 11 } }
        };

        foreach (var entry in assignments)
        {
            foreach (var ti in entry.Value)
            {
                db.EmployeeTerritories.Add(new EmployeeTerritory
                {
                    Id = Guid.NewGuid(),
                    Employee = employees[entry.Key],
                    Territory = territories[ti]
                });
            }
        }

        return employees;
    }

    private static List<Category> CreateCategories(NorthwindDbContext db)
    {
        var names = new[] { "Beverages", "Condiments", "Confections", "Dairy", "Grains", "Meat", "Produce", "Seafood" };
        var categories = names.Select(n => new Category { Id = Guid.NewGuid(), Name = n }).ToList();
        db.Categories.AddRange(categories);
        return categories;
    }

    private static List<Supplier> CreateSuppliers(NorthwindDbContext db)
    {
        var data = new (string Company, string Contact, string Phone, string City, string Country)[]
        {
            ("Exotic Liquids", "Charlotte Cooper", "(171) 555-2222", "London", "UK"),
            ("New Orleans Cajun Delights", "Shelley Burke", "(100) 555-4822", "New Orleans", "USA"),
            ("Grandma Kelly's Homestead", "Regina Murphy", "(313) 555-5735", "Ann Arbor", "USA"),
            ("Tokyo Traders", "Yoshi Nagase", "(03) 3555-5011", "Tokyo", "Japan"),
            ("Cooperativa de Quesos", "Antonio del Valle", "(98) 598 76 54", "Oviedo", "Spain"),
            ("Mayumi's", "Mayumi Ohno", "(06) 431-7877", "Osaka", "Japan"),
            ("Pavlova", "Ian Devling", "(0261) 155633", "Melbourne", "Australia"),
            ("Nord-Ost", "Sven Petersen", "(047) 555-1212", "Hamburg", "Germany"),
            ("Formaggi Fortini", "Elio Rossi", "(0544) 60323", "Ravenna", "Italy"),
            ("Healthy Kiosk", "Linn Svensson", "(08) 598 42 30", "Stockholm", "Sweden")
        };

        var suppliers = data.Select(d => new Supplier
        {
            Id = Guid.NewGuid(),
            CompanyName = d.Company,
            ContactName = d.Contact,
            Phone = d.Phone,
            City = d.City,
            Country = d.Country
        }).ToList();

        db.Suppliers.AddRange(suppliers);
        return suppliers;
    }

    private static List<Product> CreateProducts(NorthwindDbContext db, List<Category> categories, List<Supplier> suppliers, Random random)
    {
        var names = new[]
        {
            "Chai", "Chang", "Aniseed Syrup", "Chef Anton's Cajun Seasoning", "Chef Anton's Gumbo Mix",
            "Grandma's Boysenberry Spread", "Uncle Bob's Organic Dried Pears", "Northwoods Cranberry Sauce", "Mishi Kobe Niku", "Ikura",
            "Queso Cabrales", "Queso Manchego", "Konbu", "Tofu", "Genen Shouyu",
            "Pavlova", "Alice Mutton", "Carnarvon Tigers", "Teatime Chocolate Biscuits", "Sir Rodney's Scones",
            "Gustaf's Knäckebröd", "Tunnbröd", "Guaraná Fantástica", "Sasquatch Ale", "Steeleye Stout",
            "Inlagd Sill", "Gravad lax", "Côte de Blaye", "Chartreuse verte", "Ipoh Coffee"
        };

        var products = names.Select(n => new Product
        {
            Id = Guid.NewGuid(),
            Name = n,
            UnitPrice = Math.Round((decimal)(random.NextDouble() * 248) + 2m, 2),
            UnitsInStock = random.Next(10, 120),
            Discontinued = random.NextDouble() < 0.05,
            Category = categories[random.Next(categories.Count)],
            Supplier = suppliers[random.Next(suppliers.Count)]
        }).ToList();

        db.Products.AddRange(products);
        return products;
    }

    private static List<Customer> CreateCustomers(NorthwindDbContext db, Random random)
    {
        var data = new (string Company, string Contact, string City, string Country)[]
        {
            ("Alfreds Futterkiste", "Maria Anders", "Berlin", "Germany"),
            ("Ana Trujillo Emparedados", "Ana Trujillo", "México D.F.", "Mexico"),
            ("Antonio Moreno Taquería", "Antonio Moreno", "México D.F.", "Mexico"),
            ("Around the Horn", "Thomas Hardy", "London", "UK"),
            ("Berglunds snabbköp", "Christina Berglund", "Luleå", "Sweden"),
            ("Blauer See Delikatessen", "Hanna Moos", "Mannheim", "Germany"),
            ("Blondel père et fils", "Frédérique Citeaux", "Strasbourg", "France"),
            ("Bólido Comidas", "Martín Sommer", "Madrid", "Spain"),
            ("Bon app'", "Laurence Lebihan", "Marseille", "France"),
            ("Bottom-Dollar Markets", "Elizabeth Lincoln", "Tsawassen", "Canada"),
            ("Cactus Comidas", "Patricio Simpson", "Buenos Aires", "Argentina"),
            ("Centro comercial Moctezuma", "Francisco Chang", "México D.F.", "Mexico"),
            ("Chop-suey Chinese", "Yang Wang", "Bern", "Switzerland"),
            ("Comércio Mineiro", "Pedro Afonso", "São Paulo", "Brazil"),
            ("Consolidated Holdings", "Elizabeth Brown", "London", "UK"),
            ("Drachenblut Delikatessen", "Sven Ottlieb", "Aachen", "Germany"),
            ("Du monde entier", "Janine Labrune", "Paris", "France"),
            ("Eastern Connection", "Ann Devon", "London", "UK"),
            ("Ernst Handel", "Roland Mendel", "Graz", "Austria"),
            ("Familia Arquibaldo", "Aria Cruz", "São Paulo", "Brazil")
        };

        var customers = data.Select(d => new Customer
        {
            Id = Guid.NewGuid(),
            CompanyName = d.Company,
            ContactName = d.Contact,
            City = d.City,
            Country = d.Country,
            Phone = $"+1-555-{random.Next(1000, 9999)}"
        }).ToList();

        db.Customers.AddRange(customers);
        return customers;
    }

    private static List<Shipper> CreateShippers(NorthwindDbContext db, Random random)
    {
        var names = new[] { "Speedy Express", "United Package", "Federal Shipping" };
        var shippers = names.Select(n => new Shipper
        {
            Id = Guid.NewGuid(),
            CompanyName = n,
            Phone = $"+1-800-{random.Next(1000, 9999)}"
        }).ToList();

        db.Shippers.AddRange(shippers);
        return shippers;
    }

    private static List<Order> CreateOrders(NorthwindDbContext db, List<Customer> customers,
        List<Employee> employees, List<Shipper> shippers, List<Product> products, Random random)
    {
        var orders = new List<Order>();
        var statuses = Enum.GetValues<OrderStatus>();
        var baseDate = new DateTime(2024, 1, 1);

        for (int i = 0; i < 50; i++)
        {
            var customer = customers[random.Next(customers.Count)];
            var order = new Order
            {
                Id = Guid.NewGuid(),
                Customer = customer,
                Employee = employees[random.Next(employees.Count)],
                Shipper = shippers[random.Next(shippers.Count)],
                OrderDate = baseDate.AddDays(random.Next(0, 420)),
                Freight = Math.Round((decimal)(random.NextDouble() * 95) + 5m, 2),
                Status = statuses[random.Next(statuses.Length)],
                ShipAddress = "123 Market St",
                ShipCity = customer.City,
                ShipCountry = customer.Country
            };
            order.RequiredDate = order.OrderDate.AddDays(7 + random.Next(0, 14));
            order.ShippedDate = random.NextDouble() > 0.2 ? order.OrderDate.AddDays(random.Next(1, 10)) : null;

            var itemCount = random.Next(2, 5);
            for (int j = 0; j < itemCount; j++)
            {
                var product = products[random.Next(products.Count)];
                order.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    Product = product,
                    UnitPrice = product.UnitPrice,
                    Quantity = random.Next(1, 50),
                    Discount = random.Next(0, 16)
                });
            }

            orders.Add(order);
        }

        db.Orders.AddRange(orders);
        return orders;
    }

    private static void CreateInvoices(NorthwindDbContext db, List<Order> orders, Random random)
    {
        var invoiceStatuses = Enum.GetValues<InvoiceStatus>();
        var orderQueue = new Queue<Order>(orders.OrderBy(_ => random.Next()));
        int invoiceNumber = 1;

        while (orderQueue.Count > 0 && invoiceNumber <= 20)
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-{invoiceNumber:0000}",
                InvoiceDate = new DateTime(2024, 1, 1).AddDays(random.Next(0, 420)),
                Status = invoiceStatuses[random.Next(invoiceStatuses.Length)]
            };
            invoice.DueDate = invoice.InvoiceDate.AddDays(30);

            var ordersInInvoice = random.Next(2, 4);
            for (int i = 0; i < ordersInInvoice && orderQueue.Count > 0; i++)
            {
                var order = orderQueue.Dequeue();
                order.Invoice = invoice;
            }

            db.Invoices.Add(invoice);
            invoiceNumber++;
        }
    }
}
