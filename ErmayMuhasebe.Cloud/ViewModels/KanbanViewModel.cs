using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErmayMuhasebe.Models;
using ErmayMuhasebe.Cloud.Services;

namespace ErmayMuhasebe.Cloud.ViewModels
{
    public partial class KanbanViewModel : ObservableObject
    {
        private readonly ClientDatabaseService _db;

        [ObservableProperty] private ObservableCollection<BoardItem> _todo = new();
        [ObservableProperty] private ObservableCollection<BoardItem> _inProgress = new();
        [ObservableProperty] private ObservableCollection<BoardItem> _done = new();

        [ObservableProperty] private string _newTaskTitle = "";
        [ObservableProperty] private string _newTaskUser = "Admin";
        [ObservableProperty] private string _newTaskPriority = "Kritik";
        [ObservableProperty] private bool _loading;

        public KanbanViewModel(ClientDatabaseService db)
        {
            _db = db;
        }

        public async Task LoadDataAsync()
        {
            Loading = true;
            try
            {
                var data = await _db.GetKanbanDataAsync();
                if (data != null)
                {
                    Todo = new ObservableCollection<BoardItem>(data.Todo ?? new List<BoardItem>());
                    InProgress = new ObservableCollection<BoardItem>(data.InProgress ?? new List<BoardItem>());
                    Done = new ObservableCollection<BoardItem>(data.Done ?? new List<BoardItem>());
                }
            }
            finally
            {
                Loading = false;
            }
        }

        public async Task SaveDataAsync()
        {
            var data = new KanbanData(Todo.ToList(), InProgress.ToList(), Done.ToList());
            await _db.SaveKanbanDataAsync(data);
        }

        [RelayCommand]
        public async Task AddNewTask()
        {
            if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;
            
            string color = NewTaskPriority switch { "Kritik" => "#EF4444", "Yüksek" => "#F59E0B", "Normal" => "#3B82F6", _ => "#10B981" };
            Todo.Add(new BoardItem(NewTaskTitle, NewTaskUser, NewTaskPriority, color));
            NewTaskTitle = "";
            await SaveDataAsync();
        }

        [RelayCommand]
        public async Task MoveToNext(BoardItem item)
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
            await SaveDataAsync();
        }

        [RelayCommand]
        public async Task MoveToPrev(BoardItem item)
        {
            if (InProgress.Contains(item))
            {
                InProgress.Remove(item);
                Todo.Add(item);
            }
            else if (Done.Contains(item))
            {
                Done.Remove(item);
                InProgress.Add(item);
            }
            await SaveDataAsync();
        }

        [RelayCommand]
        public async Task Delete(BoardItem item)
        {
            if (Todo.Contains(item)) Todo.Remove(item);
            else if (InProgress.Contains(item)) InProgress.Remove(item);
            else if (Done.Contains(item)) Done.Remove(item);
            await SaveDataAsync();
        }

        public async Task UpdateTask(BoardItem oldItem, BoardItem newItem)
        {
            if (Todo.Contains(oldItem)) { int idx = Todo.IndexOf(oldItem); Todo[idx] = newItem; }
            else if (InProgress.Contains(oldItem)) { int idx = InProgress.IndexOf(oldItem); InProgress[idx] = newItem; }
            else if (Done.Contains(oldItem)) { int idx = Done.IndexOf(oldItem); Done[idx] = newItem; }
            await SaveDataAsync();
        }
    }
}
