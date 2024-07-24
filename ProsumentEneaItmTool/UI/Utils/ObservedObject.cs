using System.ComponentModel;
using System.Runtime.CompilerServices;

public abstract class ObservedObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return;
        }

        OnPropertyChanged([propertyName]);
    }

    protected void OnPropertyChanged(params string[] propertyNames)
    {
        if (PropertyChanged != null)
        {
            foreach (string propertyName in propertyNames)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
