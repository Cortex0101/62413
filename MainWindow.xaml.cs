using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// Set project to use checked type addition conversion by default
// Thne maybe use unchecked function if really neccessary

// Maybe opt-in to use nullable types
// The only reasons its not on by default is to preserve backwards compatability.
namespace _62413
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {
            // This is a test for the checked type addition conversion
            int a = int.MaxValue;
            int b = 1;
            try
            {
                int c = checked(a + b);
                MessageBox.Show($"Result: {c}");
            }
            catch (OverflowException ex)
            {
                MessageBox.Show($"Overflow occurred: {ex.Message}");
            }
        }
    }
}