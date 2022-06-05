using System;
using System.Windows.Input;

namespace HostingSimple.Internal
{
    /// <summary>
    /// Some default ICommand implementation since WPF doesn't have any builtin
    /// </summary>
    public class Command : ICommand
    {
        public delegate void CommandOnExecute();
        public delegate bool CommandOnCanExecute();

        private readonly CommandOnExecute? _execute;
        private readonly CommandOnCanExecute? _canExecute;

        public Command(CommandOnExecute onExecuteMethod, CommandOnCanExecute? onCanExecuteMethod = null)
        {
            _execute = onExecuteMethod;
            _canExecute = onCanExecuteMethod;
        }


        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute?.Invoke();
        }
    }
}
