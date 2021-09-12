using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MathGame
{
	/// <summary>
	/// Interaction logic for ResultDialog.xaml
	/// </summary>
	public partial class ResultDialog
    {
        private TaskCompletionSource<bool> _tc;

		public ResultDialog(ViewModel viewModel, TaskCompletionSource<bool> tc)
		{
			InitializeComponent();
            DataContext = viewModel;
            _tc = tc;
        }

        private void ButtonExit_OnClick(object sender, RoutedEventArgs e)
        {
            _tc.SetResult(false);
        }

        private void ButtonPlayAgain_OnClick(object sender, RoutedEventArgs e)
        {
            _tc.SetResult(true);
        }
    }
}
