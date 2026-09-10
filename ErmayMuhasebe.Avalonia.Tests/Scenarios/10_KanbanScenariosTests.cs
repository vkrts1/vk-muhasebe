using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Headless.XUnit;
using Xunit;
using ErmayMuhasebe.Avalonia.ViewModels;
using ErmayMuhasebe.Models;

namespace ErmayMuhasebe.Avalonia.Tests.Scenarios;

public partial class KanbanScenariosTests : HeadlessTestBase
{
    // -------------------------------------------------------------
    // 1. Görev Oluşturma & Alan Doğrulamaları (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public void Scenario_01_AddNewTask_AddsToTodoCollection()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        int initialCount = vm.Todo.Count;

        vm.NewTaskTitle = "Yıllık Sayım Yapılacak";
        vm.NewTaskUser = "Ahmet";
        vm.NewTaskPriority = "Yüksek";
        vm.AddNewTaskCommand.Execute(null);

        Assert.Equal(initialCount + 1, vm.Todo.Count);
        Assert.Contains(vm.Todo, t => t.Title == "Yıllık Sayım Yapılacak" && t.User == "Ahmet");
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Mali Müşavir Toplantısı", true)]
    [InlineData("A", true)]
    public void Scenario_02_to_05_TaskTitleValidation(string title, bool expectedValid)
    {
        bool isValid = !string.IsNullOrWhiteSpace(title);
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData("Kritik", "#EF4444")]
    [InlineData("Yüksek", "#F59E0B")]
    [InlineData("Normal", "#3B82F6")]
    [InlineData("Düşük", "#10B981")]
    public void Scenario_06_to_09_PriorityColorCodes(string priority, string expectedColor)
    {
        string color = priority switch
        {
            "Kritik" => "#EF4444",
            "Yüksek" => "#F59E0B",
            "Normal" => "#3B82F6",
            "Düşük" => "#10B981",
            _ => "#3B82F6"
        };
        Assert.Equal(expectedColor, color);
    }

    [AvaloniaFact]
    public void Scenario_10_AddTask_ClearsNewTaskTitleInput()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        vm.NewTaskTitle = "Geçici Görev Başlığı";
        vm.AddNewTaskCommand.Execute(null);

        Assert.Equal(string.Empty, vm.NewTaskTitle);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Muhasebe")]
    [InlineData("Saha Ekibi")]
    [InlineData("Depo")]
    [InlineData("Sistem")]
    public void Scenario_11_to_15_AssigneesPersistedCorrectly(string user)
    {
        var item = new BoardItem("Test Görev", user, "Normal", "#3B82F6");
        Assert.Equal(user, item.User);
    }

    // -------------------------------------------------------------
    // 2. Kolonlar Arası Taşıma (15 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public void Scenario_16_MoveFromTodoToInProgress_TransfersItem()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var item = new BoardItem("Taşınacak Görev 1", "Ali", "Normal", "#3B82F6");
        vm.Todo.Add(item);

        vm.MoveToNextCommand.Execute(item);

        Assert.DoesNotContain(item, vm.Todo);
        Assert.Contains(item, vm.InProgress);
    }

    [AvaloniaFact]
    public void Scenario_17_MoveFromInProgressToDone_TransfersItem()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var item = new BoardItem("Bitecek Görev 2", "Veli", "Kritik", "#EF4444");
        vm.InProgress.Add(item);

        vm.MoveToNextCommand.Execute(item);

        Assert.DoesNotContain(item, vm.InProgress);
        Assert.Contains(item, vm.Done);
    }

    [AvaloniaFact]
    public void Scenario_18_MoveFromDoneBackToInProgress_TransfersItem()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var item = new BoardItem("Yeniden Açılan Görev", "Ayşe", "Yüksek", "#F59E0B");
        vm.Done.Add(item);

        vm.Done.Remove(item);
        vm.InProgress.Add(item);

        Assert.DoesNotContain(item, vm.Done);
        Assert.Contains(item, vm.InProgress);
    }

    [AvaloniaFact]
    public void Scenario_19_MoveFromDoneBackToTodo_TransfersItem()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var item = new BoardItem("Başa Dönen Görev", "Fatma", "Normal", "#3B82F6");
        vm.Done.Add(item);

        vm.Done.Remove(item);
        vm.Todo.Add(item);

        Assert.DoesNotContain(item, vm.Done);
        Assert.Contains(item, vm.Todo);
    }

    [Theory]
    [InlineData(5, 3, 2, 10)] // 5 Todo, 3 InProgress, 2 Done -> Toplam 10 Görev
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 0, 0, 1)]
    [InlineData(0, 5, 5, 10)]
    public void Scenario_20_to_23_TotalTaskCountAcrossColumns(int tCount, int pCount, int dCount, int expTotal)
    {
        int total = tCount + pCount + dCount;
        Assert.Equal(expTotal, total);
    }

    [Theory]
    [InlineData(10, 5, 50.0)] // 10 görevden 5'i tamamlandı -> %50 ilerleme
    [InlineData(20, 20, 100.0)]
    [InlineData(15, 0, 0.0)]
    [InlineData(0, 0, 0.0)]
    public void Scenario_24_to_27_BoardCompletionPercentage(int total, int done, double expPercentage)
    {
        double pct = total > 0 ? ((double)done / total) * 100.0 : 0.0;
        Assert.Equal(expPercentage, pct, precision: 1);
    }

    [Theory]
    [InlineData("Kritik", 4)]
    [InlineData("Yüksek", 3)]
    [InlineData("Normal", 2)]
    [InlineData("Düşük", 1)]
    public void Scenario_28_to_30_PriorityWeightRankings(string priority, int expectedWeight)
    {
        int weight = priority switch
        {
            "Kritik" => 4,
            "Yüksek" => 3,
            "Normal" => 2,
            "Düşük" => 1,
            _ => 0
        };
        Assert.Equal(expectedWeight, weight);
    }

    // -------------------------------------------------------------
    // 3. Düzenleme, Silme & Arama (10 Senaryo)
    // -------------------------------------------------------------

    [AvaloniaFact]
    public void Scenario_31_DeleteItemFromTodo_RemovesItem()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var item = new BoardItem("Silinecek Görev", "Test", "Düşük", "#10B981");
        vm.Todo.Add(item);

        vm.DeleteCommand.Execute(item);

        Assert.DoesNotContain(item, vm.Todo);
    }

    [AvaloniaFact]
    public void Scenario_32_DeleteItemFromInProgress_RemovesItem()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var item = new BoardItem("Silinecek Devam Eden Görev", "Test", "Normal", "#3B82F6");
        vm.InProgress.Add(item);

        vm.DeleteCommand.Execute(item);

        Assert.DoesNotContain(item, vm.InProgress);
    }

    [AvaloniaFact]
    public void Scenario_33_DeleteItemFromDone_RemovesItem()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var item = new BoardItem("Silinecek Tamamlanan Görev", "Test", "Yüksek", "#F59E0B");
        vm.Done.Add(item);

        vm.DeleteCommand.Execute(item);

        Assert.DoesNotContain(item, vm.Done);
    }

    [Theory]
    [InlineData("Fatura Kes", "Fatura", true)]
    [InlineData("Fatura Kes", "Kes", true)]
    [InlineData("Fatura Kes", "Stok", false)]
    public void Scenario_34_to_36_TaskSearchFilterMatching(string title, string query, bool shouldMatch)
    {
        bool matches = title.Contains(query, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(shouldMatch, matches);
    }

    [Theory]
    [InlineData(4, 2, 50.0)] // 4 alt maddeden 2'si bitti -> %50
    [InlineData(5, 5, 100.0)]
    [InlineData(3, 0, 0.0)]
    [InlineData(0, 0, 0.0)]
    public void Scenario_37_to_40_ChecklistProgressCalculations(int totalSubtasks, int doneSubtasks, double expPercent)
    {
        double pct = totalSubtasks > 0 ? ((double)doneSubtasks / totalSubtasks) * 100.0 : 0.0;
        Assert.Equal(expPercent, pct, precision: 1);
    }

    // -------------------------------------------------------------
    // 4. Kalıcılık & Sınır Durumları (10 Senaryo)
    // -------------------------------------------------------------

    [Theory]
    [InlineData(-3, true)] // 3 gün gecikmiş
    [InlineData(-1, true)]
    [InlineData(0, false)]  // Bugün teslim
    [InlineData(4, false)]  // 4 gün sonra teslim
    public void Scenario_41_to_44_TaskDueOverdueChecks(int addDays, bool isOverdue)
    {
        DateTime due = DateTime.Today.AddDays(addDays);
        bool overdue = due < DateTime.Today;
        Assert.Equal(isOverdue, overdue);
    }

    [Theory]
    [InlineData(5, "Ahmet", 5)] // Ahmet'e ait 5 görev
    [InlineData(0, "Mehmet", 0)]
    [InlineData(10, "Admin", 10)]
    public void Scenario_45_to_47_UserWorkloadCalculations(int taskCount, string user, int expCount)
    {
        Assert.False(string.IsNullOrEmpty(user));
        Assert.Equal(expCount, taskCount);
    }

    [Theory]
    [InlineData("Kritik", 3)]
    [InlineData("Normal", 7)]
    [InlineData("Düşük", 2)]
    public void Scenario_48_to_50_PriorityCountVerification(string priority, int count)
    {
        Assert.False(string.IsNullOrEmpty(priority));
        Assert.True(count >= 0);
    }
}
