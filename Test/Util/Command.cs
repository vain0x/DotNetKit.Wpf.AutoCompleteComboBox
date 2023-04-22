using System;
using System.Windows.Input;

namespace Test.Util
{
    sealed class Command : ICommand
    {
        readonly Action<object?> execute;

        public Command(Action<object?> execute)
        {
            this.execute = execute;
        }

        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? _parameter) => true;
        public void Execute(object? parameter) => execute(parameter);
    }
}
