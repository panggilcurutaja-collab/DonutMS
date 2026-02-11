using System.ComponentModel;
using System.Windows;
using DonutMS.ViewModels;

namespace DonutMS.Views.Auth;

public partial class RegisterWindow : Window
{
    public RegisterWindow(RegisterViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
        Loaded += (_, _) => viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.CloseRequested += (_, _) => Close();
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm && sender is System.Windows.Controls.PasswordBox pb)
        {
            if (vm.PasswordInput != pb.Password)
                vm.PasswordInput = pb.Password;
        }
    }

    private void ConfirmPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is RegisterViewModel vm && sender is System.Windows.Controls.PasswordBox pb)
        {
            if (vm.ConfirmPasswordInput != pb.Password)
                vm.ConfirmPasswordInput = pb.Password;
        }
    }

    private void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not RegisterViewModel vm)
            return;

        if (e.PropertyName == nameof(RegisterViewModel.PasswordInput) &&
            PasswordBox.Password != vm.PasswordInput)
        {
            PasswordBox.Password = vm.PasswordInput;
        }

        if (e.PropertyName == nameof(RegisterViewModel.ConfirmPasswordInput) &&
            ConfirmPasswordBox.Password != vm.ConfirmPasswordInput)
        {
            ConfirmPasswordBox.Password = vm.ConfirmPasswordInput;
        }
    }
}
