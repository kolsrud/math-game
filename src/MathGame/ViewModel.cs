using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace MathGame
{
    public class Player
    {
        public string Name { get; set; }
        public long BestTime { get; set; }
    }

    public class ProblemResult
    {
        public string Status { get; set; }
        public string Problem { get; set; }
    }

    public class ViewModel
    {
        public ObservableValue<string> Player { get; } = new ObservableValue<string>();
        public ObservableCollection<string> KnownPlayers { get; } = new ObservableCollection<string>();
        public ObservableValue<string> RecordString { get; } = new ObservableValue<string>();
        public ObservableValue<string> Problem { get; } = new ObservableValue<string>();
        public ObservableValue<string> Solution { get; } = new ObservableValue<string>();
        public ObservableValue<int> ProblemCnt { get; } = new ObservableValue<int>();
        public ObservableValue<int> ProblemCntCorrect { get; } = new ObservableValue<int>();
        public ObservableValue<string> ProblemCntCorrectPercentage { get; } = new ObservableValue<string>("(0%)");
        public ObservableValue<int> ProblemCntWrong { get; } = new ObservableValue<int>();
        public ObservableValue<string> ProblemCntWrongPercentage { get; } = new ObservableValue<string>("(0%)");
        public ObservableValue<string> Time { get; } = new ObservableValue<string>();

        private Dictionary<int, int> _failCounts = new Dictionary<int, int>();
        private Dictionary<int, int> _numberCount = new Dictionary<int, int>();
        private Dictionary<int, TimeSpan> _timePerNumber = new Dictionary<int, TimeSpan>();

        private readonly Stopwatch _sw = new Stopwatch();
        private readonly Stopwatch _currentSw = new Stopwatch();
        private readonly Timer _timer = new Timer(100);

        public ObservableCollection<ProblemResult> ProblemHistory1 { get; } = new ObservableCollection<ProblemResult>();
        public ObservableCollection<ProblemResult> ProblemHistory2 { get; } = new ObservableCollection<ProblemResult>();

        private int _x;
        private int _y;
        private Random _gen = new Random();

        public ViewModel()
        {
            GetKnownPlayers().ToList().ForEach(KnownPlayers.Add);
            RunGame();
        }

        private void RunGame()
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
                _numberCount[i] = 0;
                _timePerNumber[i] = TimeSpan.Zero;
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
            var n = _numberCount[kv.Key];
            var t = _timePerNumber[kv.Key];
            var tPerN = TimeSpan.FromTicks(t.Ticks / n);
            return $"{kv.Key}: {kv.Value} fel (antal problem: {n}, tid spenderad: {FormatTime(t)}, tid per problem: {FormatTime(tPerN)})";
        } 

        public async void CheckAnswer()
        {
            if (string.IsNullOrEmpty(Solution.Value))
                return;

            _sw.Start();
            ProblemCnt.Value += 1;
            var problemResult = new ProblemResult();
            _currentSw.Stop();
            _numberCount[_x]++;
            _timePerNumber[_x] += _currentSw.Elapsed;
            if (_y != _x)
            {
                _numberCount[_y]++;
                _timePerNumber[_y] += _currentSw.Elapsed;
            }

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
                CheckRecord();
                if (!await ResultDialog.ShowWindow(this))
                    Environment.Exit(0);

                NewGame();
            }

            _currentSw.Restart();
            NewProblem();
        }

        private const string SaveFileName = "MathGameResults.json";

        private string GetSaveFileContents()
        {
            if (!File.Exists(SaveFileName))
                File.WriteAllText(SaveFileName, "[]");

            return File.ReadAllText(SaveFileName);
        }

        private IEnumerable<string> GetKnownPlayers()
        {
            var recordsTxt = GetSaveFileContents();
            var playerRecords = JsonSerializer.Deserialize<Player[]>(recordsTxt);
            return playerRecords.Select(p => p.Name);
        }

        private void CheckRecord()
        {
            var recordsTxt = GetSaveFileContents();
            var playerRecords = JsonSerializer.Deserialize<Player[]>(recordsTxt);
            var oldBest = playerRecords.FirstOrDefault(p => p.Name == Player.Value);

            var newTime = ComputeTime();
            if (oldBest == null)
            {
                playerRecords = playerRecords.Append(new Player { Name = Player.Value, BestTime = newTime.Ticks }).ToArray();
                RecordString.Value = $"Det är första gång du spelar. Du fick tiden {FormatTime(newTime)}. Bra jobbat!";
            }
            else if (oldBest.BestTime > newTime.Ticks)
            {
                var t = TimeSpan.FromTicks(oldBest.BestTime);
                RecordString.Value = $"NYTT REKORD! Ditt förra rekord var {FormatTime(t)} och nu fick du {FormatTime(newTime)}. Det är {FormatTime(t - newTime)} snabbare!";
                oldBest.BestTime = newTime.Ticks;
            }
            else
            {
                var t = TimeSpan.FromTicks(oldBest.BestTime);
                RecordString.Value = $"Inget nytt rekord denna gång. Du var {FormatTime(newTime - t)} långsammare än ditt rekord på {FormatTime(t)}.";
            }

            File.WriteAllText(SaveFileName, JsonSerializer.Serialize(playerRecords));
        }

        private static string FormatTime(TimeSpan t)
        {
            return ((int) Math.Floor(t.TotalMinutes)) + ":" + t.ToString(@"ss\.f");
        }
    }
}