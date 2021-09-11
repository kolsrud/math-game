using System;
using System.Windows;

namespace MathGame
{
    public class ViewModel
    {
        public ObservableValue<string> Problem { get; } = new ObservableValue<string>();
        public ObservableValue<string> Solution { get; } = new ObservableValue<string>();

        private int _x;
        private int _y;
        private Random _gen = new Random();

        public ViewModel()
        {
            NewProblem();
        }

        private void NewProblem()
        {
            Solution.Value = "";
            _x = _gen.Next(0, 11);
            _y = _gen.Next(0, 11);
            Problem.Value = _x + " * " + _y;
        }

        public void CheckAnswer()
        {
            if (string.IsNullOrEmpty(Solution.Value))
                return;

            if (int.Parse(Solution.Value) == _x * _y)
                MessageBox.Show("Grattis! Det var rätt!");
            else
                MessageBox.Show("Ajdå! Det blev fel... Rätt svar är " + _x*_y);

            NewProblem();
        }
    }
}