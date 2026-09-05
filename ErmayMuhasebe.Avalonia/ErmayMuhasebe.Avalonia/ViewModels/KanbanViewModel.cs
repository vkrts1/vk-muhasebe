using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.ViewModels;

public partial class KanbanViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<BoardItem> _todo = new();
    [ObservableProperty] private ObservableCollection<BoardItem> _inProgress = new();
    [ObservableProperty] private ObservableCollection<BoardItem> _done = new();

    // Input for new task
    [ObservableProperty] private string _newTaskTitle = "";
    [ObservableProperty] private string _newTaskUser = "Admin";
    [ObservableProperty] private string _newTaskPriority = "Kritik"; // Kritik, Yüksek, Normal, Düşük

    private string _filePath;

    public KanbanViewModel()
    {
        _filePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "ErmayKanban.json");
        LoadData();
    }

    private void LoadData()
    {
        if (System.IO.File.Exists(_filePath))
        {
            try
            {
                var json = System.IO.File.ReadAllText(_filePath);
                var data = System.Text.Json.JsonSerializer.Deserialize<KanbanData>(json);
                if (data != null)
                {
                    Todo = new ObservableCollection<BoardItem>(data.Todo);
                    InProgress = new ObservableCollection<BoardItem>(data.InProgress);
                    Done = new ObservableCollection<BoardItem>(data.Done);
                    return;
                }
            }
            catch { }
        }

        // Default
        Todo = new ObservableCollection<BoardItem>{ new("Stok Sayımı Yapılacak", "Ahmet", "Yüksek", "#EF4444") };
        InProgress = new ObservableCollection<BoardItem>{ new("Dolar Kur Güncellemesi", "Sistem", "Kritik", "#8B5CF6") };
        Done = new ObservableCollection<BoardItem>{ new("Yedekleme Tamamlandı", "Admin", "Düşük", "#10B981") };
    }

    private void SaveData()
    {
        var data = new KanbanData(Todo.ToList(), InProgress.ToList(), Done.ToList());
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            System.IO.File.WriteAllText(_filePath, json);
        }
        catch { }
    }

    [RelayCommand]
    public void AddNewTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;
        
        string color = NewTaskPriority switch { "Kritik" => "#EF4444", "Yüksek" => "#F59E0B", "Normal" => "#3B82F6", _ => "#10B981" };
        Todo.Add(new BoardItem(NewTaskTitle, NewTaskUser, NewTaskPriority, color));
        NewTaskTitle = "";
        SaveData();
    }

    [RelayCommand]
    public void MoveToNext(BoardItem item)
    {
        if (Todo.Contains(item))
        {
            Todo.Remove(item);
            InProgress.Add(item);
        }
        else if (InProgress.Contains(item))
        {
            InProgress.Remove(item);
            Done.Add(item);
        }
        SaveData();
    }

    [RelayCommand]
    public void DeleteConfirm(BoardItem item)
    {
        if (item == null) return;
        ShowConfirm("Görev Sil", $"'{item.Title}' görevini silmek istediğinize emin misiniz?", () => {
             if (Todo.Contains(item)) Todo.Remove(item);
             else if (InProgress.Contains(item)) InProgress.Remove(item);
             else if (Done.Contains(item)) Done.Remove(item);
             SaveData();
             return Task.CompletedTask;
        });
    }

    [RelayCommand]
    public void Delete(BoardItem item)
    {
         if (Todo.Contains(item)) Todo.Remove(item);
         else if (InProgress.Contains(item)) InProgress.Remove(item);
         else if (Done.Contains(item)) Done.Remove(item);
         SaveData();
    }
}

public record KanbanData(List<BoardItem> Todo, List<BoardItem> InProgress, List<BoardItem> Done);
