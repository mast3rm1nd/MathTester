using System;
using System.Collections.Generic;
using System.Linq;
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

using System.Diagnostics;
using System.Threading;

namespace MathTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            askedQuestions.Add(MathQuestion.GetDevisionQuestion());
        }

        static int secondsToAnswer;
        static int qustionsCount;
        static Random rnd = new Random();
        static Stopwatch sw;
        static List<MathQuestion> askedQuestions = new List<MathQuestion>();
        static bool multiplication, devision, addition, subtraction;
        static bool isGameStarted = false;

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {


            if (!(int.TryParse(SecondsToAnswer_TextBox.Text, out secondsToAnswer) &&
                int.TryParse(QuestionsCount_TextBox.Text, out qustionsCount)))
            {
                MessageBox.Show("Введите корректные значения!");
                return;
            }

            if(qustionsCount < 1 || secondsToAnswer < 1)
            {
                MessageBox.Show("Введите корректные значения!");
                return;
            }

            multiplication = (bool)IsMultiplication_CheckBox.IsChecked;
            devision = (bool)IsDevision_CheckBox.IsChecked;
            addition = (bool)IsAddition_CheckBox.IsChecked;
            subtraction = (bool)IsSubtraction_CheckBox.IsChecked;

            if (!(multiplication || devision || addition || subtraction))
            {
                MessageBox.Show("Выберите хотя бы один вид тестирования!");
                return;
            }

            Answer_TextBox.Focus();

            isGameStarted = true;            

            QuestionsAskingThread = new Thread(new ThreadStart(AskQuestions));
            QuestionsAskingThread.IsBackground = true;
            QuestionsAskingThread.Start();
            Debug.WriteLine("Поток задавания вопросов запущен");
            

            //sw = new Stopwatch();
            //sw.Start();            
        }

        void SetUI(bool unblocked)
        {
            Dispatcher.BeginInvoke(new Action(() =>
                {
                    QuestionsCount_TextBox.IsEnabled = unblocked;
                    SecondsToAnswer_TextBox.IsEnabled = unblocked;

                    IsAddition_CheckBox.IsEnabled = unblocked;
                    IsDevision_CheckBox.IsEnabled = unblocked;
                    IsMultiplication_CheckBox.IsEnabled = unblocked;
                    IsSubtraction_CheckBox.IsEnabled = unblocked;

                    Start_Button.IsEnabled = unblocked;

                    Debug.WriteLine("UI: " + unblocked);
                }));

            if(unblocked)
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Question_Label.Content = "";
                    //TimeRemainded_Label.Content = "";
                }));
        }

        static Thread CounterRefreshingThread;
        static Thread QuestionsAskingThread;
        void RefreshCounter()
        {
            Debug.WriteLine("RefreshCounter() запущен");
            sw = new Stopwatch();
            sw.Start();

            
            var secondsLeft = secondsToAnswer;
            var prevSecondsLeft = secondsLeft;

            while(sw.ElapsedMilliseconds <= secondsToAnswer * 1000)
            {
                var elapsed = sw.ElapsedMilliseconds;
                secondsLeft = secondsToAnswer - (int)(elapsed / 1000);
                

                if(secondsLeft != prevSecondsLeft)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        TimeRemainded_Label.Content = secondsLeft.ToString();
                        Debug.WriteLine(
                            String.Format("RefreshCounter(): изменение секунд. Было {0}, стало {1}. Миллисекунд прошло {2}",
                            prevSecondsLeft,
                            secondsLeft,
                            elapsed));
                            }));

                    prevSecondsLeft = secondsLeft;
                }                
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                TimeRemainded_Label.Content = "";
            }));
        }

        void AskQuestions()
        {
            Debug.WriteLine(String.Format("Запущен поток задавания вопросов"));

            askedQuestions.Clear();

            SetUI(false);

            for (int i = 0; i < qustionsCount; i++)
            {                
                //var question = MathQuestion.GetRandomQuestion(multiplication, devision, addition, subtraction);
                MathQuestion question;

                do
                {
                    question = MathQuestion.GetRandomQuestion(multiplication, devision, addition, subtraction);
                }
                while (askedQuestions.Exists(x => x.Question == question.Question));

                askedQuestions.Add(question);

                Debug.WriteLine(String.Format("Задан вопрос {0}. Всего задано {1}", question.Question ,askedQuestions.Count));

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    Question_Label.Content = question.Question;
                    TimeRemainded_Label.Content = secondsToAnswer.ToString();
                }));

                CounterRefreshingThread = new Thread(new ThreadStart(RefreshCounter));
                CounterRefreshingThread.IsBackground = true;
                CounterRefreshingThread.Start();

                while (askedQuestions.Last().UserAnswer == null) // ждём пока юзер не даст ответ
                {
                    if (sw != null)
                    if (sw.ElapsedMilliseconds >= secondsToAnswer * 1000) // если не успевает
                    {
                        CounterRefreshingThread.Abort();
                        
                        Debug.WriteLine(String.Format("Юзер не успел дать ответ. Прошло {0} мс. Было дано {1} мс",
                            sw.ElapsedMilliseconds,
                            secondsToAnswer * 1000
                            ));

                        sw = null;

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Answer_TextBox.Text = "";
                        }));

                        break;
                    }

                    Thread.Sleep(20);
                }
            }

            SetUI(true);
            isGameStarted = false;

            
            MessageBox.Show(GetResultsTable(), "Результаты", MessageBoxButton.OK, MessageBoxImage.Information);

            CounterRefreshingThread.Abort();
            sw = null;
            QuestionsAskingThread.Abort();
        }

        string GetResultsTable()
        {
            var results = "";
            var fails = 0;

            double miscalc;
            double totalMiscalc = 0;

            foreach(var question in askedQuestions)
            {
                if (question.UserAnswer == null)
                {
                    results += String.Format("{0} = {1} (Ответа не было)", question.Question, question.RightAnswer) + Environment.NewLine;
                    fails++;
                    continue;
                }

                miscalc = Math.Abs(question.RightAnswer - (double)question.UserAnswer);

                if(miscalc == 0)
                {
                    results += String.Format("{0} = {1} (Ответ верный)", question.Question, question.RightAnswer) + Environment.NewLine;
                    continue;
                }

                results += String.Format("{0} = {1} (Вы ответили {2}, ошибившись на {3})",
                    question.Question, question.RightAnswer, question.UserAnswer, miscalc) + Environment.NewLine;

                totalMiscalc += miscalc;
            }

            results += Environment.NewLine;

            if (fails != 0)
                results += String.Format("Не отвечено: {0}", fails) + Environment.NewLine;

            if(totalMiscalc != 0)
                results += String.Format("Общая погрешность: {0}", totalMiscalc);

            return results;
        }


        class MathQuestion
        {
            public string Question { get; set; }
            public double RightAnswer { get; set; }
            public double? UserAnswer { get; set; }

            public static MathQuestion GetRandomQuestion(bool multiplication, bool devision, bool addition, bool subtraction)
            {
                int type;
                while(true)
                {
                    type = rnd.Next(4);

                    switch (type)
                    {
                        case 0: if (multiplication) return GetMultiplicationQuestion(); break;
                        case 1: if (devision) return GetDevisionQuestion(); break;
                        case 2: if (addition) return GetAdditionQuestion(); break;
                        case 3: if (subtraction) return GetSubtractionQuestion(); break;
                    }
                }
            }


            public static MathQuestion GetAdditionQuestion()
            {
                var firstNum = rnd.Next(4, 100);
                var secondNum = rnd.Next(4, 100);

                var question = String.Format("{0} + {1}", firstNum, secondNum);
                double rightAnswer = firstNum + secondNum;

                return new MathQuestion { Question = question, RightAnswer = rightAnswer };
            }

            public static MathQuestion GetSubtractionQuestion()
            {
                var firstNum = rnd.Next(4, 100);
                var secondNum = rnd.Next(4, 100);

                var question = String.Format("{0} - {1}", firstNum, secondNum);
                double rightAnswer = firstNum - secondNum;

                return new MathQuestion { Question = question, RightAnswer = rightAnswer };
            }

            public static MathQuestion GetMultiplicationQuestion()
            {
                var firstNum = rnd.Next(4, 10);
                var secondNum = rnd.Next(4, 10);

                var question = String.Format("{0} * {1}", firstNum, secondNum);
                double rightAnswer = firstNum * secondNum;

                return new MathQuestion { Question = question, RightAnswer = rightAnswer };
            }

            public static MathQuestion GetDevisionQuestion()
            {
                var firstNum = rnd.Next(4, 10);
                var secondNum = rnd.Next(4, 10);

                var question = String.Format("{0} / {1}", firstNum * secondNum, firstNum);                

                return new MathQuestion { Question = question, RightAnswer = secondNum };
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            double answer;
            if (e.Key == Key.Enter && isGameStarted)
                if(double.TryParse(Answer_TextBox.Text, out answer))
                {
                    askedQuestions.Last().UserAnswer = answer;
                    Answer_TextBox.Text = "";

                    Debug.WriteLine(String.Format("Засчитан ответ {0}. Прошло {1} мс", answer, sw.ElapsedMilliseconds));

                    TimeRemainded_Label.Content = "";
                    CounterRefreshingThread.Abort();
                    //sw = null;
                }
        }



    }
}
