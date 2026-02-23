using System.ComponentModel;
using BlazorChatDemo.Data;
using BlazorChatDemo.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace BlazorChatDemo.Services;

public sealed class NorthwindToolsProvider
{
    private readonly IDbContextFactory<NorthwindDbContext> _dbFactory;
    private readonly ILogger<NorthwindToolsProvider> _logger;
    private List<AIFunction>? _tools;

    public NorthwindToolsProvider(IDbContextFactory<NorthwindDbContext> dbFactory, ILogger<NorthwindToolsProvider> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public IReadOnlyList<AIFunction> Tools => _tools ??= CreateTools();

    private List<AIFunction> CreateTools() =>
    [
        AIFunctionFactory.Create(QueryOrders, "query_orders"),
        AIFunctionFactory.Create(InvoiceAging, "invoice_aging"),
        AIFunctionFactory.Create(LowStockProducts, "low_stock_products"),
        AIFunctionFactory.Create(EmployeeOrderStats, "employee_order_stats"),
        AIFunctionFactory.Create(EmployeeTerritories, "employee_territories"),
        AIFunctionFactory.Create(CreateOrder, "create_order"),
    ];

    [Description("Search orders by customer name and/or status. Returns up to 25 orders with line items.")]
    private string QueryOrders(
        [Description("Customer company name to search for (partial match). Omit for all customers.")] string customerName = "",
        [Description("Order status filter: New, Processing, Shipped, Delivered, Cancelled. Omit for all.")] string status = "")
    {
        using var db = _dbFactory.CreateDbContext();

        var query = db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Employee)
            .Include(o => o.Shipper)
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(customerName))
            query = query.Where(o => o.Customer != null && o.Customer.CompanyName.Contains(customerName));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var st))
            query = query.Where(o => o.Status == st);

        var results = query.OrderByDescending(o => o.OrderDate).Take(25).ToList();
        if (results.Count == 0) return "No orders found.";

        var lines = results.Select(o =>
        {
            var items = string.Join("; ", o.OrderItems.Select(i =>
                $"{i.Product?.Name ?? "?"} x{i.Quantity} @{i.UnitPrice:F2}"));
            return $"[{o.OrderDate:yyyy-MM-dd}] {o.Status} | {o.Customer?.CompanyName ?? "?"} | " +
                   $"Employee: {o.Employee?.FirstName} {o.Employee?.LastName} | " +
                   $"Shipper: {o.Shipper?.CompanyName ?? "?"} | Freight: {o.Freight:F2} | " +
                   $"Ship: {o.ShipCity}, {o.ShipCountry} | Items: {items}";
        });
        return $"Found {results.Count} order(s):\n" + string.Join("\n", lines);
    }

    [Description("Get invoice aging summary grouped by customer. Shows overdue invoices by default.")]
    private string InvoiceAging(
        [Description("Invoice status: Draft, Sent, Paid, Overdue, Cancelled. Defaults to Overdue.")] string status = "Overdue")
    {
        _logger.LogInformation("[Tool:invoice_aging] Called with status={Status}", status);
        try
        {
            using var db = _dbFactory.CreateDbContext();

            if (!Enum.TryParse<InvoiceStatus>(status, true, out var invoiceStatus))
                invoiceStatus = InvoiceStatus.Overdue;

            var invoices = db.Invoices
                .Include(inv => inv.Orders).ThenInclude(o => o.Customer)
                .Include(inv => inv.Orders).ThenInclude(o => o.OrderItems)
                .AsSplitQuery()
                .Where(inv => inv.Status == invoiceStatus)
                .ToList();

            var grouped = invoices
                .SelectMany(inv => inv.Orders.Select(o => new { inv, o }))
                .Where(x => x.o.Customer != null)
                .GroupBy(x => x.o.Customer!.CompanyName)
                .Select(g => new
                {
                    Customer = g.Key,
                    Count = g.Select(x => x.inv.InvoiceNumber).Distinct().Count(),
                    Total = g.Sum(x => x.o.OrderItems.Sum(i => i.UnitPrice * i.Quantity * (1m - i.Discount / 100m))),
                    OldestDue = g.Min(x => x.inv.DueDate),
                    Invoices = g.Select(x => x.inv.InvoiceNumber).Distinct().ToList()
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            if (grouped.Count == 0) return $"No {invoiceStatus} invoices found.";

            var lines = grouped.Select(g =>
                $"{g.Customer}: {g.Count} invoice(s), Total: {g.Total:F2}, " +
                $"Oldest Due: {g.OldestDue?.ToString("yyyy-MM-dd") ?? "N/A"}, " +
                $"Invoices: {string.Join(", ", g.Invoices)}");
            var grandTotal = grouped.Sum(g => g.Total);
            return $"Invoice Aging ({invoiceStatus}) — {grouped.Count} customer(s), Grand Total: {grandTotal:F2}\n" +
                   string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Tool:invoice_aging] Error");
            return $"Error: {ex.Message}";
        }
    }

    [Description("Find non-discontinued products with stock below a given threshold. Includes supplier info.")]
    private string LowStockProducts(
        [Description("Stock threshold. Products below this level are returned. Default is 20.")] int threshold = 20)
    {
        using var db = _dbFactory.CreateDbContext();
        if (threshold <= 0) threshold = 20;

        var products = db.Products
            .Include(p => p.Supplier)
            .Include(p => p.Category)
            .Where(p => !p.Discontinued && p.UnitsInStock < threshold)
            .OrderBy(p => p.UnitsInStock)
            .ToList();

        if (products.Count == 0) return $"No products with stock below {threshold}.";

        var lines = products.Select(p =>
            $"{p.Name} | Stock: {p.UnitsInStock} | Price: {p.UnitPrice:F2} | " +
            $"Category: {p.Category?.Name ?? "N/A"} | " +
            $"Supplier: {p.Supplier?.CompanyName ?? "N/A"} ({p.Supplier?.ContactName}, {p.Supplier?.Phone})");
        return $"Low stock (below {threshold}): {products.Count} product(s)\n" + string.Join("\n", lines);
    }

    [Description("Get order count and total freight per employee, optionally filtered by date range.")]
    private string EmployeeOrderStats(
        [Description("Start date in yyyy-MM-dd format. Omit for all time.")] string fromDate = "",
        [Description("End date in yyyy-MM-dd format. Omit for all time.")] string toDate = "")
    {
        using var db = _dbFactory.CreateDbContext();
        var employees = db.Employees.Include(e => e.Orders).ToList();

        var stats = employees.Select(e =>
        {
            IEnumerable<Order> orders = e.Orders;
            if (DateTime.TryParse(fromDate, out var from)) orders = orders.Where(o => o.OrderDate >= from);
            if (DateTime.TryParse(toDate, out var to)) orders = orders.Where(o => o.OrderDate <= to);
            var list = orders.ToList();
            return new
            {
                Name = $"{e.FirstName} {e.LastName}",
                e.Title,
                Count = list.Count,
                TotalFreight = list.Sum(o => o.Freight),
                AvgFreight = list.Count > 0 ? list.Average(o => o.Freight) : 0m,
                Last = list.Count > 0 ? list.Max(o => o.OrderDate).ToString("yyyy-MM-dd") : "N/A"
            };
        }).OrderByDescending(x => x.Count).ToList();

        var lines = stats.Select(s =>
            $"{s.Name} ({s.Title}): {s.Count} orders, Freight: {s.TotalFreight:F2} total / {s.AvgFreight:F2} avg, Last: {s.Last}");
        return $"Employee Order Stats (from: {(string.IsNullOrEmpty(fromDate) ? "all" : fromDate)}, to: {(string.IsNullOrEmpty(toDate) ? "all" : toDate)})\n" +
               string.Join("\n", lines);
    }

    [Description("List all employees with their territory counts and territory/region details.")]
    private string EmployeeTerritories()
    {
        using var db = _dbFactory.CreateDbContext();
        var employees = db.Employees
            .Include(e => e.Territories).ThenInclude(et => et.Territory).ThenInclude(t => t.Region)
            .AsSplitQuery()
            .ToList();

        var lines = employees.OrderByDescending(e => e.Territories.Count).Select(e =>
        {
            var terrs = string.Join(", ", e.Territories.Select(et =>
                $"{et.Territory?.Name ?? "?"} ({et.Territory?.Region?.Name ?? "?"})"));
            return $"{e.FirstName} {e.LastName}: {e.Territories.Count} — {terrs}";
        });
        return "Employee Territories:\n" + string.Join("\n", lines);
    }

    [Description("Create a new order for a customer with a product and shipper.")]
    private string CreateOrder(
        [Description("Customer company name (must match an existing customer).")] string customerName,
        [Description("Product name (must match an existing product).")] string productName,
        [Description("Quantity to order. Must be greater than 0.")] int quantity,
        [Description("Shipper company name (must match an existing shipper).")] string shipperName)
    {
        using var db = _dbFactory.CreateDbContext();
        try
        {
            var customer = db.Customers.FirstOrDefault(c => c.CompanyName.Contains(customerName));
            if (customer == null)
                return "Customer not found. Available: " + string.Join(", ",
                    db.Customers.Select(c => c.CompanyName).Take(10).ToList());

            var product = db.Products.FirstOrDefault(p => p.Name.Contains(productName));
            if (product == null)
                return "Product not found. Available: " + string.Join(", ",
                    db.Products.Where(p => !p.Discontinued).Select(p => p.Name).Take(10).ToList());

            var shipper = db.Shippers.FirstOrDefault(s => s.CompanyName.Contains(shipperName));
            if (shipper == null)
                return "Shipper not found. Available: " + string.Join(", ",
                    db.Shippers.Select(s => s.CompanyName).ToList());

            if (quantity <= 0) return "Quantity must be greater than 0.";

            var order = new Order
            {
                Id = Guid.NewGuid(),
                Customer = customer,
                Shipper = shipper,
                OrderDate = DateTime.Now,
                RequiredDate = DateTime.Now.AddDays(14),
                Status = OrderStatus.New,
                ShipAddress = customer.Address,
                ShipCity = customer.City,
                ShipCountry = customer.Country,
                Freight = 0m
            };

            order.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                Product = product,
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
                Discount = 0m
            });

            db.Orders.Add(order);
            db.SaveChanges();

            return $"Order created! Date: {order.OrderDate:yyyy-MM-dd}, Customer: {customer.CompanyName}, " +
                   $"Product: {product.Name} x{quantity} @{product.UnitPrice:F2} = {product.UnitPrice * quantity:F2}, " +
                   $"Shipper: {shipper.CompanyName}, Status: New, Ship: {order.ShipCity}, {order.ShipCountry}";
        }
        catch (Exception ex)
        {
            return $"Error creating order: {ex.Message}";
        }
    }
}
