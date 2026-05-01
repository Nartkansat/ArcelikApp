using ArcelikApp.Data;
using ArcelikApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Uwp.Notifications;

namespace ArcelikApp.Services
{
    public class NotificationService
    {
        public static event EventHandler? NotificationsChanged;

        public static void SendNotification(int? userId, string title, string message, string type = "Info")
        {
            using var db = new AppDbContext();
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            try
            {
                db.Notifications.Add(notification);
                db.SaveChanges();
                
                NotificationsChanged?.Invoke(null, EventArgs.Empty);

                // If the notification is for the current user, show a Windows toast
                if (AuthService.CurrentUser != null && userId == AuthService.CurrentUser.Id)
                {
                    ShowToast(title, message, type);
                }
            }
            catch (Exception)
            {
                // Veritabanına kaydedilemese bile en azından toast göstermeyi deneyebiliriz
                if (AuthService.CurrentUser != null && userId == AuthService.CurrentUser.Id)
                {
                    ShowToast(title, message, type);
                }
            }
        }

        public static void ShowToast(string title, string message, string type = "Info")
        {
            try
            {
                var builder = new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message);

                // Add attribution or type-specific styling if needed
                if (type == "Error") builder.AddAttributionText("⚠️ Kritik Uyarı");
                else if (type == "Warning") builder.AddAttributionText("⚡ Dikkat");
                else builder.AddAttributionText("🔔 Yeni Bildirim");

                builder.Show();
            }
            catch (Exception)
            {
                // Toasts might fail if not supported or missing permissions
            }
        }

        public static void SendToAll(string title, string message, string type = "Info")
        {
            using var db = new AppDbContext();
            var users = db.Users.Where(u => u.IsActive).ToList();
            
            foreach (var user in users)
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                db.Notifications.Add(notification);
            }
            
            db.SaveChanges();
            NotificationsChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void SendToRole(string role, string title, string message, string type = "Info")
        {
            using var db = new AppDbContext();
            var users = db.Users.Where(u => u.IsActive && u.Role == role).ToList();
            
            foreach (var user in users)
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };
                db.Notifications.Add(notification);
            }
            
            db.SaveChanges();
            NotificationsChanged?.Invoke(null, EventArgs.Empty);
        }

        public static List<Notification> GetUserNotifications(int userId)
        {
            using var db = new AppDbContext();
            // Get specific notifications for user OR general notifications (UserId == null)
            return db.Notifications
                .Where(n => n.UserId == userId || n.UserId == null)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public static int GetUnreadCount(int userId)
        {
            using var db = new AppDbContext();
            // Count notifications that are either for this user and unread, 
            // OR for everyone and haven't been "read" by this user.
            // Note: For simplicity in this local app, we'll treat UserId == null notifications 
            // as read if they are marked as IsRead globally, OR we could implement a per-user read status for global ones.
            // For now, let's just count based on IsRead flag.
            return db.Notifications
                .Count(n => (n.UserId == userId || n.UserId == null) && !n.IsRead);
        }

        public static void MarkAsRead(int notificationId)
        {
            using var db = new AppDbContext();
            var notification = db.Notifications.Find(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                db.SaveChanges();
                NotificationsChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static void MarkAllAsRead(int userId)
        {
            using var db = new AppDbContext();
            var unread = db.Notifications
                .Where(n => (n.UserId == userId || n.UserId == null) && !n.IsRead)
                .ToList();

            foreach (var n in unread)
                n.IsRead = true;

            db.SaveChanges();
            NotificationsChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void DeleteNotification(int notificationId)
        {
            using var db = new AppDbContext();
            var notification = db.Notifications.Find(notificationId);
            if (notification != null)
            {
                db.Notifications.Remove(notification);
                db.SaveChanges();
                NotificationsChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static void DeleteAllNotifications(int userId)
        {
            using var db = new AppDbContext();
            var notifications = db.Notifications
                .Where(n => n.UserId == userId || n.UserId == null)
                .ToList();

            db.Notifications.RemoveRange(notifications);
            db.SaveChanges();
            NotificationsChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
