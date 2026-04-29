using System;
using System.Threading.Tasks;

namespace ArcelikApp.Services
{
    public enum ModernDialogType
    {
        Info,
        Success,
        Warning,
        Error,
        Question
    }

    public class ModernDialogService
    {
        private static TaskCompletionSource<bool>? _dialogTaskSource;

        public static event EventHandler<ModernDialogEventArgs>? DialogRequested;

        public static Task<bool> ShowAsync(string title, string message, ModernDialogType type = ModernDialogType.Info)
        {
            _dialogTaskSource = new TaskCompletionSource<bool>();
            
            DialogRequested?.Invoke(null, new ModernDialogEventArgs
            {
                Title = title,
                Message = message,
                Type = type
            });

            return _dialogTaskSource.Task;
        }

        public static void SetResult(bool result)
        {
            _dialogTaskSource?.TrySetResult(result);
        }
    }

    public class ModernDialogEventArgs : EventArgs
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public ModernDialogType Type { get; set; } = ModernDialogType.Info;
    }
}
