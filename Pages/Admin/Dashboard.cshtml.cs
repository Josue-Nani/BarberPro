using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BarberPro.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace BarberPro.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly BarberContext _context;

        public DashboardModel(BarberContext context)
        {
            _context = context;
        }

        public int TotalReservas { get; set; }
        public int TotalClientes { get; set; }
        public int MonthsToShow { get; set; } = 6; // default last 6 months

        public class StatRow
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public string MonthName => CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetMonthName(Month);
            public int ReservationsCount { get; set; }
            public int ClientsCount { get; set; }
        }

        public List<StatRow> Stats { get; set; } = new List<StatRow>();

        // Chart data serialized as JSON for client-side Chart.js
        public string MonthLabelsJson { get; set; } = "[]";
        public string ReservationsJson { get; set; } = "[]";
        public string ClientsJson { get; set; } = "[]";
        public string RevenueJson { get; set; } = "[]";

        // Top services
        public class TopServiceRow { public string Name { get; set; } = string.Empty; public int Count { get; set; } }
        public List<TopServiceRow> TopServices { get; set; } = new List<TopServiceRow>();

        public async Task OnGetAsync(int months = 6)
        {
            MonthsToShow = Math.Clamp(months, 1, 24);

            TotalReservas = await _context.Reservas.CountAsync();

            // Count total clientes based on Usuarios who have the role "Cliente" if roles exist
            TotalClientes = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol != null && u.Rol.NombreRol == "Cliente")
                .CountAsync();

            var start = DateTime.UtcNow.AddMonths(-MonthsToShow + 1);
            var stats = new List<StatRow>();

            var labels = new List<string>();
            var reservationsList = new List<int>();
            var clientsList = new List<int>();
            var revenueList = new List<decimal>();

            for (int i = 0; i < MonthsToShow; i++)
            {
                var date = start.AddMonths(i);
                var year = date.Year;
                var month = date.Month;

                var reservationsCount = await _context.Reservas
                    .Where(r => r.FechaReserva.Year == year && r.FechaReserva.Month == month)
                    .CountAsync();

                // Count new client registrations in Usuarios by FechaRegistro for role Cliente
                var clientsCount = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Where(u => u.FechaRegistro != null && u.FechaRegistro.Value.Year == year && u.FechaRegistro.Value.Month == month && u.Rol != null && u.Rol.NombreRol == "Cliente")
                    .CountAsync();

                // Compute revenue for the month by joining reservas with servicios
                var revenue = await (from r in _context.Reservas
                                     join s in _context.Servicios on r.ServicioID equals s.ServicioID
                                     where r.FechaReserva.Year == year && r.FechaReserva.Month == month
                                     select s.Precio).SumAsync();

                stats.Add(new StatRow
                {
                    Year = year,
                    Month = month,
                    ReservationsCount = reservationsCount,
                    ClientsCount = clientsCount
                });

                labels.Add($"{CultureInfo.GetCultureInfo("es-ES").DateTimeFormat.GetAbbreviatedMonthName(month)} {year}");
                reservationsList.Add(reservationsCount);
                clientsList.Add(clientsCount);
                revenueList.Add(revenue);
            }

            Stats = stats;

            // Top services overall (last 12 months)
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var topServices = await _context.Reservas
                .Where(r => r.FechaReserva >= twelveMonthsAgo)
                .GroupBy(r => r.ServicioID)
                .Select(g => new { ServicioID = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .ToListAsync();

            var svcDict = await _context.Servicios
                .Where(s => topServices.Select(t => t.ServicioID).Contains(s.ServicioID))
                .ToDictionaryAsync(s => s.ServicioID, s => s.Nombre);

            TopServices = topServices.Select(t => new TopServiceRow { Name = svcDict.ContainsKey(t.ServicioID) ? svcDict[t.ServicioID] : ("#" + t.ServicioID), Count = t.Count }).ToList();

            // Serialize for Chart.js
            MonthLabelsJson = JsonSerializer.Serialize(labels);
            ReservationsJson = JsonSerializer.Serialize(reservationsList);
            ClientsJson = JsonSerializer.Serialize(clientsList);
            RevenueJson = JsonSerializer.Serialize(revenueList);
        }

        public async Task<IActionResult> OnPostDownloadReportAsync(int months = 6)
        {
            MonthsToShow = months;

            var start = DateTime.UtcNow.AddMonths(-MonthsToShow + 1);
            var rows = new List<string>();
            rows.Add("Year,Month,Reservations,Clients,Revenue");

            for (int i = 0; i < MonthsToShow; i++)
            {
                var date = start.AddMonths(i);
                var year = date.Year;
                var month = date.Month;

                var reservationsCount = await _context.Reservas
                    .Where(r => r.FechaReserva.Year == year && r.FechaReserva.Month == month)
                    .CountAsync();

                var clientsCount = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Where(u => u.FechaRegistro != null && u.FechaRegistro.Value.Year == year && u.FechaRegistro.Value.Month == month && u.Rol != null && u.Rol.NombreRol == "Cliente")
                    .CountAsync();

                var revenue = await (from r in _context.Reservas
                                     join s in _context.Servicios on r.ServicioID equals s.ServicioID
                                     where r.FechaReserva.Year == year && r.FechaReserva.Month == month
                                     select s.Precio).SumAsync();

                rows.Add($"{year},{month},{reservationsCount},{clientsCount},{revenue}");
            }

            var csv = string.Join("\n", rows);
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "dashboard-report.csv");
        }
    }
}
