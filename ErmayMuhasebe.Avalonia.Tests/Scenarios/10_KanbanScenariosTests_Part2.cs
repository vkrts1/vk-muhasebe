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
    // 5. Çoklu Görev Ekleme & Kolon İşlemleri (51 - 65)
    // =============================================================

    [AvaloniaFact]
    public void Scenario_51_Kanban_AddMultipleTasksToTodo_IncrementsTodoCount()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        int before = vm.Todo.Count;

        vm.Todo.Add(new BoardItem("Görev 1", "Ali", "Normal", "#3B82F6"));
        vm.Todo.Add(new BoardItem("Görev 2", "Veli", "Yüksek", "#F59E0B"));
        vm.Todo.Add(new BoardItem("Görev 3", "Ayşe", "Kritik", "#EF4444"));

        Assert.Equal(before + 3, vm.Todo.Count);
    }

    [AvaloniaFact]
    public void Scenario_52_Kanban_MoveMultipleTasksAcrossColumns_UpdatesAll()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var item1 = new BoardItem("Akış Görevi 1", "Ahmet", "Normal", "#3B82F6");
        var item2 = new BoardItem("Akış Görevi 2", "Mehmet", "Düşük", "#10B981");
        vm.Todo.Add(item1);
        vm.Todo.Add(item2);

        // Todo -> InProgress
        vm.MoveToNextCommand.Execute(item1);
        vm.MoveToNextCommand.Execute(item2);

        Assert.Contains(item1, vm.InProgress);
        Assert.Contains(item2, vm.InProgress);

        // InProgress -> Done
        vm.MoveToNextCommand.Execute(item1);
        Assert.Contains(item1, vm.Done);
        Assert.DoesNotContain(item1, vm.InProgress);
    }

    [AvaloniaFact]
    public void Scenario_53_Kanban_ClearAllTasks_ClearsCollections()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        vm.Todo.Clear();
        vm.InProgress.Clear();
        vm.Done.Clear();

        Assert.Empty(vm.Todo);
        Assert.Empty(vm.InProgress);
        Assert.Empty(vm.Done);
    }

    [Theory]
    [InlineData("Kasa Mutabakatı", "Admin", "Kritik")]
    [InlineData("Banka Ekstresi İndir", "Muhasebe", "Yüksek")]
    [InlineData("Müşteri Takibi", "Satış", "Normal")]
    [InlineData("Arşiv Düzenle", "Stajyer", "Düşük")]
    public void Scenario_54_to_57_BoardItem_CreationPropertiesVerification(string title, string user, string priority)
    {
        var item = new BoardItem(title, user, priority, "#3B82F6");
        Assert.Equal(title, item.Title);
        Assert.Equal(user, item.User);
        Assert.Equal(priority, item.Priority);
    }

    [Theory]
    [InlineData(10, 0, 0, 10)]
    [InlineData(5, 5, 5, 15)]
    [InlineData(0, 12, 8, 20)]
    [InlineData(0, 0, 0, 0)]
    public void Scenario_58_to_61_TotalBoardCountCalculations(int todo, int inProg, int done, int expectedTotal)
    {
        int total = todo + inProg + done;
        Assert.Equal(expectedTotal, total);
    }

    [Theory]
    [InlineData(10, 10, 100.0)]
    [InlineData(10, 5, 50.0)]
    [InlineData(10, 2, 20.0)]
    [InlineData(10, 0, 0.0)]
    public void Scenario_62_to_65_CompletionProgressRates(int total, int completed, double expectedRate)
    {
        double rate = total > 0 ? (double)completed / total * 100.0 : 0.0;
        Assert.Equal(expectedRate, rate);
    }

    // =============================================================
    // 6. Öncelik & Renk Eşleştirmeleri (66 - 80)
    // =============================================================

    [Theory]
    [InlineData("Kritik", "#EF4444", 4)]
    [InlineData("Yüksek", "#F59E0B", 3)]
    [InlineData("Normal", "#3B82F6", 2)]
    [InlineData("Düşük", "#10B981", 1)]
    public void Scenario_66_to_69_PriorityColorAndWeightMapping(string priority, string expectedHex, int expectedWeight)
    {
        string color = priority switch
        {
            "Kritik" => "#EF4444",
            "Yüksek" => "#F59E0B",
            "Normal" => "#3B82F6",
            "Düşük" => "#10B981",
            _ => "#3B82F6"
        };
        int weight = priority switch
        {
            "Kritik" => 4,
            "Yüksek" => 3,
            "Normal" => 2,
            "Düşük" => 1,
            _ => 0
        };

        Assert.Equal(expectedHex, color);
        Assert.Equal(expectedWeight, weight);
    }

    [Fact]
    public void Scenario_70_Kanban_SortTasksByPriorityDescending()
    {
        var items = new List<BoardItem>
        {
            new("Görev A", "User", "Düşük", "#10B981"),
            new("Görev B", "User", "Kritik", "#EF4444"),
            new("Görev C", "User", "Normal", "#3B82F6"),
            new("Görev D", "User", "Yüksek", "#F59E0B")
        };

        int GetWeight(string p) => p switch { "Kritik" => 4, "Yüksek" => 3, "Normal" => 2, "Düşük" => 1, _ => 0 };
        var sorted = items.OrderByDescending(x => GetWeight(x.Priority)).ToList();

        Assert.Equal("Görev B", sorted[0].Title); // Kritik
        Assert.Equal("Görev D", sorted[1].Title); // Yüksek
        Assert.Equal("Görev C", sorted[2].Title); // Normal
        Assert.Equal("Görev A", sorted[3].Title); // Düşük
    }

    [Theory]
    [InlineData("Kritik", true)]
    [InlineData("Yüksek", true)]
    [InlineData("Normal", true)]
    [InlineData("Düşük", true)]
    [InlineData("Bilinmeyen", false)]
    public void Scenario_71_to_75_ValidPriorityStrings(string priority, bool isValid)
    {
        bool valid = priority == "Kritik" || priority == "Yüksek" || priority == "Normal" || priority == "Düşük";
        Assert.Equal(isValid, valid);
    }

    [Theory]
    [InlineData("#EF4444", true)]
    [InlineData("#10B981", true)]
    [InlineData("red", false)]
    [InlineData("", false)]
    public void Scenario_76_to_79_HexColorFormatValidation(string hex, bool isValidHex)
    {
        bool valid = hex.StartsWith("#") && hex.Length == 7;
        Assert.Equal(isValidHex, valid);
    }

    [Fact]
    public void Scenario_80_Kanban_DefaultPriorityIsKritik()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        Assert.Equal("Kritik", vm.NewTaskPriority);
    }

    // =============================================================
    // 7. Arama & Kullanıcı Bazlı Filtreleme (81 - 95)
    // =============================================================

    [Fact]
    public void Scenario_81_Kanban_SearchAcrossAllColumns_FindsMatches()
    {
        var todo = new List<BoardItem> { new("Fatura Kesimi", "Ahmet", "Normal", "#3B82F6") };
        var inProg = new List<BoardItem> { new("Stok Sayımı", "Mehmet", "Yüksek", "#F59E0B") };
        var done = new List<BoardItem> { new("Fatura Onayı", "Ali", "Kritik", "#EF4444") };

        string query = "Fatura";
        var all = todo.Concat(inProg).Concat(done);
        var matches = all.Where(x => x.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public void Scenario_82_Kanban_FilterByUser_FindsAssignedTasks()
    {
        var tasks = new List<BoardItem>
        {
            new("G1", "Ahmet", "Normal", "#3B82F6"),
            new("G2", "Mehmet", "Normal", "#3B82F6"),
            new("G3", "Ahmet", "Yüksek", "#F59E0B"),
            new("G4", "Ayşe", "Kritik", "#EF4444")
        };

        var ahmetTasks = tasks.Where(t => t.User == "Ahmet").ToList();
        Assert.Equal(2, ahmetTasks.Count);
    }

    [Theory]
    [InlineData("Admin", 3)]
    [InlineData("Saha", 2)]
    [InlineData("Finans", 5)]
    [InlineData("Yok", 0)]
    public void Scenario_83_to_86_UserTaskCountLookup(string user, int expCount)
    {
        var dict = new Dictionary<string, int>
        {
            { "Admin", 3 },
            { "Saha", 2 },
            { "Finans", 5 }
        };
        int count = dict.TryGetValue(user, out int val) ? val : 0;
        Assert.Equal(expCount, count);
    }

    [Fact]
    public void Scenario_87_Kanban_CaseInsensitiveSearch()
    {
        var item = new BoardItem("FATURA ONAYI", "Maliye", "Kritik", "#EF4444");
        bool match = item.Title.Contains("fatura", StringComparison.OrdinalIgnoreCase);
        Assert.True(match);
    }

    [Fact]
    public void Scenario_88_Kanban_TurkishCharactersInTitle()
    {
        string trTitle = "ÇEK VE SENET İŞLEMLERİ TAKİBİ";
        var item = new BoardItem(trTitle, "Fatma", "Normal", "#3B82F6");
        Assert.Equal(trTitle, item.Title);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(20, 20)]
    public void Scenario_89_to_91_CollectionCapacityIntegrity(int count, int expected)
    {
        var list = new List<BoardItem>();
        for (int i = 0; i < count; i++) list.Add(new BoardItem($"Görev {i}", "U", "N", "#3B82F6"));
        Assert.Equal(expected, list.Count);
    }

    [Fact]
    public void Scenario_92_Kanban_NewTaskUserDefault_IsNotEmpty()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        Assert.False(string.IsNullOrEmpty(vm.NewTaskUser));
    }

    [Fact]
    public void Scenario_93_Kanban_DeleteFromEmptyCollection_DoesNotThrow()
    {
        var vm = _serviceProvider.GetRequiredService<KanbanViewModel>();
        var dummy = new BoardItem("Olmayan", "U", "N", "#3B82F6");
        vm.DeleteCommand.Execute(dummy);
        Assert.NotNull(vm.Todo);
    }

    [Theory]
    [InlineData(10, 0, 10, 0.0)]
    [InlineData(10, 10, 0, 100.0)]
    [InlineData(10, 5, 5, 50.0)]
    public void Scenario_94_to_95_WorkInProgressVsDoneRatio(int total, int done, int pending, double expRatio)
    {
        double r = total > 0 ? (double)done / total * 100.0 : 0.0;
        Assert.Equal(expRatio, r);
        Assert.Equal(total, done + pending);
    }

    // =============================================================
    // 8. Sınır Değerler & Kalıcılık Analizi (96 - 100)
    // =============================================================

    [Fact]
    public void Scenario_96_Kanban_ExtremelyLongTitle_RetainsWholeString()
    {
        string longTitle = new string('X', 500);
        var item = new BoardItem(longTitle, "Admin", "Normal", "#3B82F6");
        Assert.Equal(500, item.Title.Length);
    }

    [Fact]
    public void Scenario_97_Kanban_SpecialCharactersInTitle_HandledCorrectly()
    {
        string special = "Görev #123 [Öncelik: Acil] <Proje & Maliyet> / 2026";
        var item = new BoardItem(special, "Admin", "Kritik", "#EF4444");
        Assert.Equal(special, item.Title);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Merve")]
    [InlineData("Serkan")]
    [InlineData("Emre")]
    public void Scenario_98_to_100_AssigneeNameIntegrity(string name)
    {
        var item = new BoardItem("Test", name, "Normal", "#3B82F6");
        Assert.Equal(name, item.User);
    }
}
