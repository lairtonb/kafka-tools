using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KafkaTools.Common
{
    public class DelegateCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler? CanExecuteChanged;

        public DelegateCommand(Action execute)
            : this(execute, null)
        {
        }

        public DelegateCommand(Action execute, Func<bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (() => true);
        }

        public void Execute(object? parameter)
        {
            _execute.Invoke();
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class AsyncDelegateCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private bool _isExecuting;



        public AsyncDelegateCommand(Func<Task> execute)
            : this(execute, null)
        {
        }

        public AsyncDelegateCommand(Func<Task> execute, Func<bool>? canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (() => true);
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (_isExecuting)
                return;

            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();

                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
