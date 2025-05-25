using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DiaryTelegramBot.Data;

public class Record
{
    [Key]
    public long id { get; set; }
    public long UserId { get; set; }
    public string Text { get; set; }
    
    [NotMapped]
    public int SelectedIndex { get; set; }
    [NotMapped]
    public bool IsTemp { get; set; } 
    public DateTime SentTime { get; set; }
}