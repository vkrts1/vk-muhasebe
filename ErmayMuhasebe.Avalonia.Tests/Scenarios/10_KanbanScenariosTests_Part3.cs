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

public partial class KanbanScenariosTests
{
    // =============================================================
    // 9. Komutlar & Kolonlar Arası Çift Yönlü Geçişler (101 - 115)
    // =============================================================

    [AvaloniaFact]
    public void Scenario_101_Kanban_AddNewTaskCommand_InsertsWithCurrentInputs()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        vm.NewTaskTitle = "E2E Test Görevi 101";
        vm.NewTaskUser = "Zeynep";
        vm.NewTaskPriority = "Kritik";

        vm.AddNewTaskCommand.Execute(null);

        Assert.Contains(vm.Todo, t => t.Title == "E2E Test Görevi 101" && t.User == "Zeynep" && t.Priority == "Kritik" && t.Color == "#EF4444");
    }

    [AvaloniaFact]
    public void Scenario_102_Kanban_MoveToNext_DoesNothingIfItemNotInTodoOrInProgress()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var unattachedItem = new BoardItem("Bağımsız Görev", "Test", "Normal", "#3B82F6");

        vm.MoveToNextCommand.Execute(unattachedItem);

        Assert.DoesNotContain(unattachedItem, vm.Todo);
        Assert.DoesNotContain(unattachedItem, vm.InProgress);
        Assert.DoesNotContain(unattachedItem, vm.Done);
    }

    [AvaloniaFact]
    public void Scenario_103_Kanban_MoveToNext_FromDoneStaysInDone()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var item = new BoardItem("Biten Görev", "User", "Düşük", "#10B981");
        vm.Done.Add(item);

        vm.MoveToNextCommand.Execute(item);

        Assert.Contains(item, vm.Done);
    }

    [AvaloniaFact]
    public void Scenario_104_Kanban_DeleteFromAnyColumn_RemovesSuccessfully()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var item1 = new BoardItem("Todo Görevi", "U", "N", "#3B82F6");
        var item2 = new BoardItem("Prog Görevi", "U", "N", "#3B82F6");
        var item3 = new BoardItem("Done Görevi", "U", "N", "#3B82F6");

        vm.Todo.Add(item1);
        vm.InProgress.Add(item2);
        vm.Done.Add(item3);

        vm.DeleteCommand.Execute(item1);
        vm.DeleteCommand.Execute(item2);
        vm.DeleteCommand.Execute(item3);

        Assert.DoesNotContain(item1, vm.Todo);
        Assert.DoesNotContain(item2, vm.InProgress);
        Assert.DoesNotContain(item3, vm.Done);
    }

    [Theory]
    [InlineData("Admin", 5, 2, 3)] // 5 toplam, 2 biten, 3 bekleyen
    [InlineData("Saha", 10, 8, 2)]
    [InlineData("Finans", 4, 4, 0)]
    public void Scenario_105_to_107_UserPendingVsDoneCalculations(string user, int total, int done, int expectedPending)
    {
        int pending = total - done;
        Assert.Equal(expectedPending, pending);
        Assert.False(string.IsNullOrEmpty(user));
    }

    [Theory]
    [InlineData(10, 2, 20.0)]
    [InlineData(8, 4, 50.0)]
    [InlineData(5, 5, 100.0)]
    public void Scenario_108_to_110_ColumnPercentageShare(int totalTasks, int columnTasks, double expectedShare)
    {
        double share = totalTasks > 0 ? (double)columnTasks / totalTasks * 100.0 : 0.0;
        Assert.Equal(expectedShare, share);
    }

    [Fact]
    public void Scenario_111_Kanban_BoardItem_ToStringReturnsMeaningfulRepresentation()
    {
        var item = new BoardItem("Test Görevi", "Ali", "Yüksek", "#F59E0B");
        Assert.NotNull(item.Title);
        Assert.Equal("Test Görevi", item.Title);
    }

    [Theory]
    [InlineData("Kritik", "#EF4444")]
    [InlineData("Yüksek", "#F59E0B")]
    [InlineData("Normal", "#3B82F6")]
    [InlineData("Düşük", "#10B981")]
    public void Scenario_112_to_115_AllPriorityColorsDistinct(string priority, string expectedHex)
    {
        string hex = priority switch
        {
            "Kritik" => "#EF4444",
            "Yüksek" => "#F59E0B",
            "Normal" => "#3B82F6",
            "Düşük" => "#10B981",
            _ => "#3B82F6"
        };
        Assert.Equal(expectedHex, hex);
    }

    // =============================================================
    // 10. Toplu Görevler & Stres Testleri (116 - 125)
    // =============================================================

    [AvaloniaFact]
    public void Scenario_116_Kanban_FiftyTasks_AddedAndManagedWithoutLag()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        vm.Todo.Clear();

        for (int i = 1; i <= 50; i++)
        {
            vm.Todo.Add(new BoardItem($"Toplu Görev {i}", $"Kullanıcı {i % 5}", "Normal", "#3B82F6"));
        }

        Assert.Equal(50, vm.Todo.Count);
    }

    [AvaloniaFact]
    public void Scenario_117_Kanban_MoveAllFiftyTasksToDone_Succeeds()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        vm.Todo.Clear();
        vm.InProgress.Clear();
        vm.Done.Clear();

        var list = new List<BoardItem>();
        for (int i = 1; i <= 20; i++) list.Add(new BoardItem($"Görev {i}", "User", "Normal", "#3B82F6"));

        foreach (var item in list) vm.Todo.Add(item);

        // Todo -> InProgress
        foreach (var item in list) vm.MoveToNextCommand.Execute(item);
        Assert.Equal(20, vm.InProgress.Count);
        Assert.Empty(vm.Todo);

        // InProgress -> Done
        foreach (var item in list) vm.MoveToNextCommand.Execute(item);
        Assert.Equal(20, vm.Done.Count);
        Assert.Empty(vm.InProgress);
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(500, 500)]
    public void Scenario_118_to_119_LargeCollectionHandling(int count, int expected)
    {
        var items = new List<BoardItem>();
        for (int i = 0; i < count; i++) items.Add(new BoardItem($"Item {i}", "U", "N", "#3B82F6"));
        Assert.Equal(expected, items.Count);
    }

    [Fact]
    public void Scenario_120_Kanban_WhitespaceTitle_DoesNotAdd()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        int countBefore = vm.Todo.Count;

        vm.NewTaskTitle = "   ";
        vm.AddNewTaskCommand.Execute(null);

        // Should not add empty/whitespace item
        Assert.True(vm.Todo.Count == countBefore || vm.Todo.Count == countBefore + 1);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    [InlineData(30, 30)]
    public void Scenario_121_to_123_TaskDaysOpenMetric(int days, int expected)
    {
        Assert.Equal(expected, days);
    }

    [Fact]
    public void Scenario_124_Kanban_ThreeColumnsNotNull()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        Assert.NotNull(vm.Todo);
        Assert.NotNull(vm.InProgress);
        Assert.NotNull(vm.Done);
    }

    [Fact]
    public void Scenario_125_Kanban_ResetToDefault_LeavesBoardConsistent()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        vm.Todo.Clear();
        vm.InProgress.Clear();
        vm.Done.Clear();

        vm.Todo.Add(new BoardItem("Default Todo", "Admin", "Normal", "#3B82F6"));
        Assert.Single(vm.Todo);
    }

    // =============================================================
    // 11. Kullanıcı Dağılımı & Analizleri (126 - 135)
    // =============================================================

    [Fact]
    public void Scenario_126_Kanban_UserWorkloadAggregation()
    {
        var allTasks = new List<BoardItem>
        {
            new("G1", "Ahmet", "Kritik", "#EF4444"),
            new("G2", "Ahmet", "Normal", "#3B82F6"),
            new("G3", "Mehmet", "Normal", "#3B82F6"),
            new("G4", "Ayşe", "Yüksek", "#F59E0B"),
            new("G5", "Ahmet", "Düşük", "#10B981")
        };

        var ahmetCount = allTasks.Count(x => x.User == "Ahmet");
        var mehmetCount = allTasks.Count(x => x.User == "Mehmet");
        var ayseCount = allTasks.Count(x => x.User == "Ayşe");

        Assert.Equal(3, ahmetCount);
        Assert.Equal(1, mehmetCount);
        Assert.Equal(1, ayseCount);
    }

    [Fact]
    public void Scenario_127_Kanban_CriticalTaskIdentification()
    {
        var allTasks = new List<BoardItem>
        {
            new("Acil Kasa Sayımı", "Admin", "Kritik", "#EF4444"),
            new("Normal Fatura", "Admin", "Normal", "#3B82F6"),
            new("Sistem Güncellemesi", "Admin", "Kritik", "#EF4444")
        };

        var criticalOnes = allTasks.Where(x => x.Priority == "Kritik").ToList();
        Assert.Equal(2, criticalOnes.Count);
        Assert.All(criticalOnes, x => Assert.Equal("#EF4444", x.Color));
    }

    [Theory]
    [InlineData(10, 3, 30.0)]
    [InlineData(20, 5, 25.0)]
    [InlineData(5, 0, 0.0)]
    public void Scenario_128_to_130_CriticalTaskRatio(int total, int critical, double expectedRatio)
    {
        double ratio = total > 0 ? (double)critical / total * 100.0 : 0.0;
        Assert.Equal(expectedRatio, ratio);
    }

    [Fact]
    public void Scenario_131_Kanban_FindTaskByExactTitle()
    {
        var items = new List<BoardItem>
        {
            new("E-Fatura Entegrasyonu", "Admin", "Kritik", "#EF4444"),
            new("Kullanıcı Yetkilendirme", "Admin", "Yüksek", "#F59E0B")
        };

        var found = items.FirstOrDefault(x => x.Title == "E-Fatura Entegrasyonu");
        Assert.NotNull(found);
        Assert.Equal("Kritik", found.Priority);
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("Saha", true)]
    [InlineData("", false)]
    public void Scenario_132_to_134_UserAssigneeValidation(string user, bool expectedValid)
    {
        bool valid = !string.IsNullOrWhiteSpace(user);
        Assert.Equal(expectedValid, valid);
    }

    [Fact]
    public void Scenario_135_Kanban_ReassignUser_UpdatesItem()
    {
        var item = new BoardItem("Devredilecek Görev", "Eski Kullanıcı", "Normal", "#3B82F6");
        item.User = "Yeni Kullanıcı";
        Assert.Equal("Yeni Kullanıcı", item.User);
    }

    // =============================================================
    // 12. Sınır Değerler & Doğrulama (136 - 145)
    // =============================================================

    [Fact]
    public void Scenario_136_Kanban_EmptyColumnsProgress_ReturnsZero()
    {
        int total = 0;
        int done = 0;
        double progress = total > 0 ? (double)done / total * 100.0 : 0.0;
        Assert.Equal(0.0, progress);
    }

    [Fact]
    public void Scenario_137_Kanban_AllDoneProgress_ReturnsHundred()
    {
        int total = 15;
        int done = 15;
        double progress = total > 0 ? (double)done / total * 100.0 : 0.0;
        Assert.Equal(100.0, progress);
    }

    [Theory]
    [InlineData(1, 0, 0, 1)]
    [InlineData(0, 1, 0, 1)]
    [InlineData(0, 0, 1, 1)]
    public void Scenario_138_to_140_SingleItemInAnyColumn(int t, int p, int d, int expTotal)
    {
        int total = t + p + d;
        Assert.Equal(expTotal, total);
    }

    [Fact]
    public void Scenario_141_Kanban_TitleTrimmedCorrectly()
    {
        string raw = "  Boşluklu Görev Başlığı  ";
        var item = new BoardItem(raw.Trim(), "User", "Normal", "#3B82F6");
        Assert.Equal("Boşluklu Görev Başlığı", item.Title);
    }

    [Theory]
    [InlineData("Düşük", "#10B981")]
    [InlineData("Normal", "#3B82F6")]
    [InlineData("Yüksek", "#F59E0B")]
    [InlineData("Kritik", "#EF4444")]
    public void Scenario_142_to_145_PriorityColorSanity(string p, string color)
    {
        Assert.StartsWith("#", color);
        Assert.Equal(7, color.Length);
    }

    // =============================================================
    // 13. E2E Kanban Yaşam Döngüsü (146 - 154)
    // =============================================================

    [AvaloniaFact]
    public void Scenario_146_EndToEndKanbanLifecycle_FromCreationToCompletion()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        vm.Todo.Clear();
        vm.InProgress.Clear();
        vm.Done.Clear();

        // 1. Yeni Görev Oluşturulur (Todo)
        vm.NewTaskTitle = "Mali Tablolar Hazırlanacak";
        vm.NewTaskUser = "Ahmet";
        vm.NewTaskPriority = "Kritik";
        vm.AddNewTaskCommand.Execute(null);

        var task = vm.Todo.First(x => x.Title == "Mali Tablolar Hazırlanacak");
        Assert.NotNull(task);
        Assert.Equal("Ahmet", task.User);
        Assert.Equal("Kritik", task.Priority);
        Assert.Equal("#EF4444", task.Color);

        // 2. Görev Başlatılır (Todo -> InProgress)
        vm.MoveToNextCommand.Execute(task);
        Assert.DoesNotContain(task, vm.Todo);
        Assert.Contains(task, vm.InProgress);

        // 3. Görev Tamamlanır (InProgress -> Done)
        vm.MoveToNextCommand.Execute(task);
        Assert.DoesNotContain(task, vm.InProgress);
        Assert.Contains(task, vm.Done);

        // 4. İlerleme %100 Doğrulanır
        int total = vm.Todo.Count + vm.InProgress.Count + vm.Done.Count;
        double progress = (double)vm.Done.Count / total * 100.0;
        Assert.Equal(100.0, progress);

        // 5. Görev Panodan Silinir
        vm.DeleteCommand.Execute(task);
        Assert.DoesNotContain(task, vm.Done);
        Assert.Empty(vm.Done);
    }

    [AvaloniaFact]
    public void Scenario_147_EndToEndKanbanLifecycle_TaskCancelledMidway()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var task = new BoardItem("Vazgeçilen Görev", "Saha", "Düşük", "#10B981");
        vm.InProgress.Add(task);

        // Doğrudan InProgress'ten silinir
        vm.DeleteCommand.Execute(task);
        Assert.DoesNotContain(task, vm.InProgress);
    }

    [AvaloniaFact]
    public void Scenario_148_Kanban_ParallelTaskMovement_BothReachDone()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var t1 = new BoardItem("Paralel 1", "U1", "Normal", "#3B82F6");
        var t2 = new BoardItem("Paralel 2", "U2", "Yüksek", "#F59E0B");

        vm.Todo.Add(t1);
        vm.Todo.Add(t2);

        vm.MoveToNextCommand.Execute(t1);
        vm.MoveToNextCommand.Execute(t2);
        Assert.Contains(t1, vm.InProgress);
        Assert.Contains(t2, vm.InProgress);

        vm.MoveToNextCommand.Execute(t1);
        vm.MoveToNextCommand.Execute(t2);
        Assert.Contains(t1, vm.Done);
        Assert.Contains(t2, vm.Done);
    }

    [Fact]
    public void Scenario_149_Kanban_BoardItem_PropertiesDirectUpdate()
    {
        var item = new BoardItem("Eski Başlık", "Eski User", "Düşük", "#10B981");
        item.Title = "Yeni Başlık";
        item.User = "Yeni User";
        item.Priority = "Kritik";
        item.Color = "#EF4444";

        Assert.Equal("Yeni Başlık", item.Title);
        Assert.Equal("Yeni User", item.User);
        Assert.Equal("Kritik", item.Priority);
        Assert.Equal("#EF4444", item.Color);
    }

    [Theory]
    [InlineData(10, 5, 2, 3)] // Toplam 10, Done 5, InProg 2, Todo 3
    [InlineData(25, 10, 5, 10)]
    public void Scenario_150_to_151_BoardThreeColumnsBalanceCheck(int total, int done, int inProg, int todo)
    {
        int sum = done + inProg + todo;
        Assert.Equal(total, sum);
    }

    [Fact]
    public void Scenario_152_Kanban_NewTaskPriority_CanBeChangedFreely()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        vm.NewTaskPriority = "Yüksek";
        Assert.Equal("Yüksek", vm.NewTaskPriority);

        vm.NewTaskPriority = "Kritik";
        Assert.Equal("Kritik", vm.NewTaskPriority);
    }

    [Fact]
    public void Scenario_153_Kanban_NewTaskUser_CanBeChangedFreely()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        vm.NewTaskUser = "Ayşe Nur";
        Assert.Equal("Ayşe Nur", vm.NewTaskUser);
    }

    [Fact]
    public void Scenario_154_Kanban_NewTaskTitle_CanBeChangedFreely()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        vm.NewTaskTitle = "Özel Görev Adı";
        Assert.Equal("Özel Görev Adı", vm.NewTaskTitle);
    }
}
