using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Services;

namespace ErmayMuhasebe.Repositories.Firebase;

/// <summary>
/// Firebase için base repository implementasyonu
/// Tüm Firebase repository'ler için ortak işlevler sağlar
/// </summary>
/// <typeparam name="T">Entity tipi</typeparam>
public abstract class BaseFirebaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IFirebaseService _firebaseService;
    protected abstract string ResourceName { get; }

    public BaseFirebaseRepository(IFirebaseService firebaseService)
    {
        _firebaseService = firebaseService;
    }

    public virtual async Task<List<T>> GetAllAsync()
    {
        try
        {
            return await _firebaseService.GetAllAsync<T>(ResourceName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase GetAllAsync Error ({ResourceName}): {ex.Message}");
            return new List<T>();
        }
    }

    public virtual async Task<List<T>> GetPagedAsync(int skip, int take, Expression<Func<T, bool>>? predicate = null, Expression<Func<T, object>>? orderBy = null, bool descending = false)
    {
        var all = await GetAllAsync();
        var query = all.AsQueryable();
        
        if (predicate != null) query = query.Where(predicate);
        if (orderBy != null)
        {
            if (descending) query = query.OrderByDescending(orderBy);
            else query = query.OrderBy(orderBy);
        }
        
        return query.Skip(skip).Take(take).ToList();
    }

    public virtual async Task<int> GetCountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var all = await GetAllAsync();
        var query = all.AsQueryable();
        if (predicate != null) query = query.Where(predicate);
        return query.Count();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        try
        {
            var all = await GetAllAsync();
            // Firebase'de ID property'si olan entity'ler için
            var entity = all.FirstOrDefault(e => GetId(e) == id);
            return entity;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase GetByIdAsync Error ({ResourceName}): {ex.Message}");
            return null;
        }
    }

    public virtual async Task<int> SaveAsync(T entity)
    {
        try
        {
            int id = GetId(entity);
            if (id == 0)
            {
                // Yeni kayıt - ID oluştur
                id = await GenerateNewIdAsync();
                SetId(entity, id);
            }

            await _firebaseService.SaveAsync(ResourceName, entity, id);
            return id;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase SaveAsync Error ({ResourceName}): {ex.Message}");
            return 0;
        }
    }

    public virtual async Task<int> DeleteAsync(T entity)
    {
        try
        {
            int id = GetId(entity);
            await _firebaseService.DeleteAsync(ResourceName, id);
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase DeleteAsync Error ({ResourceName}): {ex.Message}");
            return 0;
        }
    }

    public virtual async Task<int> DeleteAsync(int id)
    {
        try
        {
            await _firebaseService.DeleteAsync(ResourceName, id);
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase DeleteAsync Error ({ResourceName}): {ex.Message}");
            return 0;
        }
    }

    public virtual async Task<List<T>> GetDeletedAsync()
    {
        // Firebase'de soft delete için IsDeleted property'si kontrol et
        var all = await GetAllAsync();
        return all.Where(e => IsDeleted(e)).ToList();
    }

    public virtual async Task RestoreAsync(T entity)
    {
        SetDeleted(entity, false);
        await SaveAsync(entity);
    }

    // Helper methods - Reflection kullanarak ID ve IsDeleted property'lerine eriş
    protected virtual int GetId(T entity)
    {
        var prop = typeof(T).GetProperty("Id");
        if (prop != null && prop.PropertyType == typeof(int))
        {
            return (int)(prop.GetValue(entity) ?? 0);
        }
        return 0;
    }

    protected virtual void SetId(T entity, int id)
    {
        var prop = typeof(T).GetProperty("Id");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(entity, id);
        }
    }

    protected virtual bool IsDeleted(T entity)
    {
        var prop = typeof(T).GetProperty("IsDeleted");
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            return (bool)(prop.GetValue(entity) ?? false);
        }
        return false;
    }

    protected virtual void SetDeleted(T entity, bool value)
    {
        var prop = typeof(T).GetProperty("IsDeleted");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(entity, value);
        }
    }

    protected virtual async Task<int> GenerateNewIdAsync()
    {
        var all = await GetAllAsync();
        if (all.Count == 0)
            return 1;

        int maxId = all.Max(e => GetId(e));
        return maxId + 1;
    }
}

