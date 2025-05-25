using System.ComponentModel.DataAnnotations.Schema;

namespace ReminderWorker.Data;

public class Remind
{
    public long Id { get; set; } 
    
    public DateTime Time { get; set; }
    public string Message { get; set; }
    [NotMapped]
    public int SelectedIndex { get; set; }
    public bool IsRemind { get; set; }
    
    public long UserId { get; set; } 
   
}