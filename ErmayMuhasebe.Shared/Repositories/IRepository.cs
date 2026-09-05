using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ErmayMuhasebe.Repositories;

/// <summary>
/// Generic Repository Interface
/// Tüm repository'ler için temel CRUD işlemlerini tanımlar
/// </summary>
/// <typeparam name="T">Entity tipi</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Tüm kayıtları getirir
    /// </summary>
    Task<List<T>> GetAllAsync();

    /// <summary>
    /// Sayfalanmış ve filtrelenmiş kayıtları getirir
    /// </summary>
    Task<List<T>> GetPagedAsync(int skip, int take, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>? orderBy = null, bool descending = false);

    /// <summary>
    /// Filtreye uygun toplam kayıt sayısını getirir
    /// </summary>
    Task<int> GetCountAsync(Expression<Func<T, bool>>? predicate = null);

    /// <summary>
    /// ID'ye göre tek kayıt getirir
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Yeni kayıt ekler veya mevcut kaydı günceller
    /// </summary>
    /// <returns>Kayıt ID'si</returns>
    Task<int> SaveAsync(T entity);

    /// <summary>
    /// Kaydı siler (soft delete)
    /// </summary>
    Task<int> DeleteAsync(T entity);

    /// <summary>
    /// ID'ye göre kaydı siler (hard delete)
    /// </summary>
    Task<int> DeleteAsync(int id);

    /// <summary>
    /// Silinen kayıtları getirir
    /// </summary>
    Task<List<T>> GetDeletedAsync();

    /// <summary>
    /// Silinen kaydı geri yükler
    /// </summary>
    Task RestoreAsync(T entity);
}

