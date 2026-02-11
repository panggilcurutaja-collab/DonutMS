using System.ComponentModel;
using System.Windows;
using DonutMS.ViewModels;

namespace DonutMS.Views.Auth;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
        Loaded += (_, _) => viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.CloseRequested += (_, _) =>
        {
            DialogResult = viewModel.IsLoginSuccessful;
            Close();
        };
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && sender is System.Windows.Controls.PasswordBox pb)
        {
            if (vm.PasswordInput != pb.Password)
                vm.PasswordInput = pb.Password;
        }
    }

    private void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm)
            return;

        if (e.PropertyName == nameof(LoginViewModel.PasswordInput) &&
            PasswordBox.Password != vm.PasswordInput)
        {
            PasswordBox.Password = vm.PasswordInput;
        }
    }
}
