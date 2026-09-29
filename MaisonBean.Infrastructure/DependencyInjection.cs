using MaisonBean.Application.Common;
using MaisonBean.Application.Interfaces;
using MaisonBean.Domain.Entities;
using MaisonBean.Infrastructure.Configurations;
using MaisonBean.Infrastructure.Persistence;
using MaisonBean.Infrastructure.Persistence.Repositories;
using MaisonBean.Infrastructure.Repositories;
using MaisonBean.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace MaisonBean.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<AppUser>(options =>
            {
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireDigit = false;
        })
        .AddRoles<IdentityRole<int>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICookieService, CookieService>();
        services.Configure<CloudinarySettings>(
        configuration.GetSection("Cloudinary")
);

        services.AddScoped<IImageService,
            CloudinaryService>();

        services.Configure<JwtSettings>(
            configuration.GetSection("JwtSettings"));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IBeanTypeRepository, BeanTypeRepository>();
        services.AddScoped<IMilkOptionRepository, MilkOptionRepository>();
        services.AddScoped<ICustomizationRepository, CustomizationRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}