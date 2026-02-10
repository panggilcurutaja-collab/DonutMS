using System.Windows.Controls;
using DonutMS.ViewModels;

namespace DonutMS.Views.Ingredients;

public partial class IngredientListView : UserControl
{
    public IngredientListView()
    {
        InitializeComponent();
        DataContext = new IngredientsViewModel(null!, null!, null!);
    }
}
