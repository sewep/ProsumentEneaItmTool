﻿using System.Windows.Input;

class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object, bool>? _canExecute;
    public RelayCommand(Action<object?> execute, Func<object, bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);

        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add
        {
            CommandManager.RequerySuggested += value;
        }
        remove
        {
            CommandManager.RequerySuggested -= value;
        }
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute is null || _canExecute(parameter!);
    }

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }
}