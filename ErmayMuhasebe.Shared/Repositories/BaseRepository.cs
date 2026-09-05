using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories;

/// <summary>
/// Base Repository Implementation
/// Tüm repository'ler için ortak işlevleri sağlar
/// </summary>
/// <typeparam name="T">Entity tipi</typeparam>
public abstract class BaseRepository<T> : IRepository<T> where T : class, new()
{
    protected readonly DatabaseService _dbService;
    protected readonly CloudSyncService _syncService;

    protected BaseRepository(DatabaseService dbService)
    {
        _dbService = dbService;
        _syncService = dbService.SyncService;
    }

    /// <summary>
    /// Veritabanı bağlantısını döndürür
    /// </summary>
    protected async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        await _dbService.EnsureInitializedAsync();
        return _dbService.GetConnection();
    }

    /// <summary>
    /// Tablo adını döndürür (Entity adından türetilir)
    /// </summary>
    protected virtual string GetTableName() => typeof(T).Name;

    public virtual async Task<List<T>> GetPagedAsync(int skip, int take, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>? orderBy = null, bool descending = false)
    {
        var db = await GetConnectionAsync();
        var query = db.Table<T>();
        if (predicate != null) query = query.Where(predicate);
        
        if (orderBy != null)
        {
            // SQLite-net-PCL OrderBy is a bit strict with types, but this often works for basic types
            if (descending) query = query.OrderByDescending(orderBy);
            else query = query.OrderBy(orderBy);
        }

        return await query.Skip(skip).Take(take).ToListAsync();
    }

    public virtual async Task<int> GetCountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var db = await GetConnectionAsync();
        var query = db.Table<T>();
        if (predicate != null) query = query.Where(predicate);
        return await query.CountAsync();
    }

    public abstract Task<List<T>> GetAllAsync();
    public abstract Task<T?> GetByIdAsync(int id);
    public abstract Task<int> SaveAsync(T entity);
    public abstract Task<int> DeleteAsync(T entity);
    public abstract Task<int> DeleteAsync(int id);
    public abstract Task<List<T>> GetDeletedAsync();
    public abstract Task RestoreAsync(T entity);
}

