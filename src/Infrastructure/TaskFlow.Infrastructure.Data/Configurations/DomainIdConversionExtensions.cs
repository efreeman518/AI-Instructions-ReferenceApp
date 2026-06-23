using EF.Data.Converters;
using EF.Domain.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TaskFlow.Infrastructure.Data.Configurations;

internal static class DomainIdConversionExtensions
{
    public static void ConfigureDomainIdConversions(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var propertyType = property.ClrType;
                var idType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                if (!IsDomainId(idType)) continue;

                var converterType = Nullable.GetUnderlyingType(propertyType) is null
                    ? typeof(DomainIdValueConverter<>).MakeGenericType(idType)
                    : typeof(NullableDomainIdValueConverter<>).MakeGenericType(idType);
                var converter = (ValueConverter)Activator.CreateInstance(converterType)!;

                property.SetValueConverter(converter);
            }
        }
    }

    private static bool IsDomainId(Type type) =>
        type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IDomainId<>));
}
