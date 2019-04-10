using System;
using Microsoft.Bot.Schema;

public class ScheduleNotification
{
    // 通知をする時間
    public DateTime NotificationTime { get; set; }
    // 実際の予定開始時間
    public DateTime StartTime { get; set; }
    // 予定のタイトル
    public string Title { get; set; }
    // 予定へのリンク
    public string WebLink { get; set; }
    public ConversationReference ConversationReference { get; set; }
}