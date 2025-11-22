using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using BarberPro.Data;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Authorization
{
    public class AdminHandler : AuthorizationHandler<AdminRequirement>
    {
        private readonly BarberContext _context;

        public AdminHandler(BarberContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
        {
            // If user has role claim set to Administrador, succeed immediately
            if (context.User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Administrador"))
            {
                context.Succeed(requirement);
                return;
            }

            // Otherwise, try to find user's id and check DB role
            var idClaim = context.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var userId))
            {
                try
                {
                    var user = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.UsuarioID == userId);
                    if (user != null)
                    {
                        var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RolID == user.RolID);
                        if (role != null && role.NombreRol == "Administrador")
                        {
                            context.Succeed(requirement);
                            return;
                        }
                    }
                }
                catch
                {
                    // ignore DB errors and do not grant access
                }
            }

            // otherwise do nothing (not authorized)
        }
    }
}
