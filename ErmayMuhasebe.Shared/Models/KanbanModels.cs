using System;
using System.Collections.Generic;

namespace ErmayMuhasebe.Models
{
    public record BoardItem
    {
        public string Title { get; set; } = "";
        public string User { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Color { get; set; } = "";
        public string Description { get; set; } = "";
        
        // Parameterless constructor for Firebase/Serialization
        public BoardItem() { }
        
        // Manual constructor for cloning if needed, though 'with' works
        public BoardItem(string title, string user, string priority, string color, string description = "")
        {
            Title = title;
            User = user;
            Priority = priority;
            Color = color;
            Description = description;
        }
    }
    
    public record KanbanData(List<BoardItem> Todo, List<BoardItem> InProgress, List<BoardItem> Done);
}
