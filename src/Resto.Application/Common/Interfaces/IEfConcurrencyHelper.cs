namespace Resto.Application.Common.Interfaces;

public interface IEfConcurrencyHelper
{
    void StampRowVersion<TEntity>(TEntity entity, byte[] rowVersion) where TEntity : class;
}
