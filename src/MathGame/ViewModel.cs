using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace MathGame
{
    public class Player
    {
        public string Name { get; set; }
        public TimeSpan BestTime { get; set; }
    }

    public class ProblemResult
    {
        public string Status { get; set; }
        public string Problem { get; set; }
    }

    public class ViewModel
    {
        public ObservableValue<string> Problem { get; } = new ObservableValue<string>();
        public ObservableValue<string> Solution { get; } = new ObservableValue<string>();
        public ObservableValue<int> ProblemCnt { get; } = new ObservableValue<int>();
        public ObservableValue<int> ProblemCntCorrect { get; } = new ObservableValue<int>();
        public ObservableValue<string> ProblemCntCorrectPercentage { get; } = new ObservableValue<string>("(0%)");
        public ObservableValue<int> ProblemCntWrong { get; } = new ObservableValue<int>();
        public ObservableValue<string> ProblemCntWrongPercentage { get; } = new ObservableValue<string>("(0%)");
        public ObservableValue<string> Time { get; } = new ObservableValue<string>();

        private Dictionary<int, int> _failCounts = new Dictionary<int, int>();

        private readonly Stopwatch _sw = new Stopwatch();
        private readonly Timer _timer = new Timer(100);

        public ObservableCollection<ProblemResult> ProblemHistory1 { get; } = new ObservableCollection<ProblemResult>();
        public ObservableCollection<ProblemResult> ProblemHistory2 { get; } = new ObservableCollection<ProblemResult>();

        private int _x;
        private int _y;
        private Random _gen = new Random();

        public ViewModel()
        {
            NewGame();
            _timer.AutoReset = true;
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            NewProblem();
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            Time.Value = ElapsedTimeString;
        }

        private TimeSpan ComputeTime()
        {
            return _sw.Elapsed + AddedTime;
        }

        private TimeSpan AddedTime => TimeSpan.FromSeconds(ProblemCntWrong.Value * 30);

        private string ElapsedTimeString => ComputeTime().ToString(@"m\:ss");

        private void NewGame()
        {
            ProblemCnt.Value = 0;
            ProblemCntCorrect.Value = 0;
            ProblemCntWrong.Value = 0;
            ProblemCntCorrectPercentage.Value = "(0%)";
            ProblemCntWrongPercentage.Value = "(0%)";
            _sw.Reset();
            ProblemHistory1.Clear();
            ProblemHistory2.Clear();
            foreach (var i in Enumerable.Range(0, 11))
            {
                _failCounts[i] = 0;
            }
        }

        private void NewProblem()
        {
            Solution.Value = "";
            _x = _gen.Next(0, 11);
            _y = _gen.Next(0, 11);
            Problem.Value = _x + " * " + _y;
        }

        public string ResultSummary =>
            $"Din tid blev {ElapsedTimeString} ({_sw.Elapsed:m\\:ss} + {AddedTime:m\\:ss}) och du fick {100 * ProblemCntCorrect.Value / ProblemCnt.Value:D}% rätt!";

        public string DetailedResult => GetDetailedResults();

        private string GetDetailedResults()
        {
            return string.Join(Environment.NewLine, _failCounts.OrderBy(kv => kv.Key).Select(GetDetailedResult));
        }

        private string GetDetailedResult(KeyValuePair<int, int> kv)
        {
            return $"{kv.Key}: {kv.Value} fel";
        }

        public async void CheckAnswer()
        {
            if (string.IsNullOrEmpty(Solution.Value))
                return;

            _sw.Start();
            ProblemCnt.Value += 1;
            var problemResult = new ProblemResult();
            problemResult.Problem = Problem.Value;
            if (int.Parse(Solution.Value) == _x * _y)
            {
                ProblemCntCorrect.Value += 1;
                problemResult.Status = "R";
            }
            else
            {
                ProblemCntWrong.Value += 1;
                problemResult.Status = "V";
                MessageBox.Show("Ajdå! Det blev fel... Du förlorar 30 sekunder! Rätt svar är " + _x * _y, "Fel svar!", MessageBoxButton.OK, MessageBoxImage.Error);
                _failCounts[_x]++;
                if (_y != _x)
                    _failCounts[_y]++;
            }

            ProblemCntCorrectPercentage.Value = $"({100 * ProblemCntCorrect.Value / ProblemCnt.Value:D}%)";
            ProblemCntWrongPercentage.Value = $"({100 * ProblemCntWrong.Value / ProblemCnt.Value:D}%)";

            if (ProblemCnt.Value % 2 == 1)
                ProblemHistory1.Add(problemResult);
            else
                ProblemHistory2.Add(problemResult);

            if (ProblemCnt.Value == 100)
            {
                _sw.Stop();
                if (!await ShowWindow())
                    Environment.Exit(0);

                NewGame();
            }

            NewProblem();
        }

        private async Task<bool> ShowWindow()
        {
            var t = new TaskCompletionSource<bool>();
            var currentWindow = new ResultDialog(this, t);
            currentWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            currentWindow.Owner = Application.Current.MainWindow;
            currentWindow.Show();
            currentWindow.Focus();
            var b = await t.Task;
            currentWindow.Close();
            return b;
        }
    }
}