using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Windows.Threading;
using System.Threading;
using System.IO;

namespace ASCIIficator
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Bitmap currImage; //загруженное неконвертированное изображение
        string currASCIIImage; // текущее изображение, конвертированое в ascii
        List<ASCIIAnimationFrame> currASCIIAnimation; //сет кадров текущей анимации
        int sizeModifier = 2; // модификатор размера при конвертации; чем больше, тем мельче
        bool stopFlag = false; //флаг для выхода из цикла проигрывания анимации

        public MainWindow()
        {
            InitializeComponent();
        }

        //диалоговое окно загрузки картинки
        private void load_button_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            NoImageButtonsBlock();
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Open Image";
            dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (dlg.ShowDialog() == true)
            {
                LoadingActivity(true);
                if (dlg.FileName.EndsWith("gif"))
                {   
                    LoadAndPlay(
                        () => {
                                currASCIIAnimation = Converter.GetASCIIAnimationFrames(dlg.OpenFile());
                                Converter.ConvertAnimation(currASCIIAnimation, sizeModifier);
                              },
                        () => PlayASCIIAnimation(currASCIIAnimation));
                }
                else
                {
                    LoadAndPlay(
                        () => {
                                currImage = Converter.LoadImage(dlg.OpenFile());
                                currASCIIImage = (Converter.ConvertImage(currImage, sizeModifier));
                              },
                        () =>  DrawASCIIImage(currASCIIImage));
                }
            }
        }

        //вывод статическое картинки
        void DrawASCIIImage(String asciiImage)
        {
            stopFlag = false;
            DispatcherAction((ThreadStart)(() =>
            {
                LoadingActivity(false);
                textBlock.Text = asciiImage;
            }));
        }

        //вывод анимированной картинки
        void PlayASCIIAnimation(List<ASCIIAnimationFrame> animation)
        {
            stopFlag = false;
            while (!stopFlag)
            {
                foreach (ASCIIAnimationFrame frame in animation)
                {
                    if (stopFlag) break;
                    DispatcherAction((ThreadStart)(() =>
                        {
                            LoadingActivity(false);
                            //DrawASCIIImage(frame.asciiFrame);   
                            textBlock.Text = frame.asciiFrame;
                        }));
                    Thread.Sleep(frame.delay);
                }
            }
            DispatcherAction((ThreadStart)(() =>
            //richTextBox.Document.Blocks.Clear()
            textBlock.Text = ""
            ));
        }

        //увеличение картинки
        private void incr_button_Click(object sender, RoutedEventArgs e)
        {
            if (sizeModifier > 1)
            {
                sizeModifier--;
                ReDraw();
                decr_button.IsEnabled = true;

            } else
            {
                incr_button.IsEnabled = false;
            }
        }
        //уменьшение картинки
        private void decr_button_Click(object sender, RoutedEventArgs e)
        {
            if (sizeModifier < 10)
            {
                sizeModifier++;
                ReDraw();
                incr_button.IsEnabled = true;
            } else
            {
                decr_button.IsEnabled = false;
            }
        }

        //метод позволяет обратиться к интерфейсу из вторичных потоков
        void DispatcherAction(Delegate action)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        //метод запускающий асинхронную задачу в отдельном потоке и продолжение к ней
        //напр. - загрузка, конвертация картинки и продолжение - вывод картинки
        void LoadAndPlay(Action first, Action second)
        {
            Task task = new Task(() => first.Invoke());
            task.ContinueWith(t => second.Invoke());
            task.Start();
        }

        //сброс всех полей и флагов
        void Reset()
        {
            stopFlag = true;
            textBlock.Text = "";
            currImage = null;
            currASCIIImage = null;
            currASCIIAnimation = null;
            sizeModifier = 2;
        }

        //блокировка кнопок во время загрузки и конвертации картинок
        void LoadingActivity(bool activity)
        {
            load_button.IsEnabled = !activity;
            incr_button.IsEnabled = !activity;
            decr_button.IsEnabled = !activity;
            save_button.IsEnabled = !activity;

            if (activity) Task.Factory.StartNew(() => LoadingAnimation());
        }
        //блокировка кнопок когда картинка не загружена
        void NoImageButtonsBlock()
        {
            incr_button.IsEnabled = false;
            decr_button.IsEnabled = false;
            save_button.IsEnabled = false;
        }

        //сообщения загрузки, чтобы понимать что приложение не висит
        void LoadingAnimation()
        {
            while(stopFlag)
            {
                DispatcherAction((ThreadStart)(() =>
                //richTextBox.AppendText("\n" + "Loading...")
                textBlock.Text += "\n Loading..."
                ));
                Thread.Sleep(1000);
            }
        }

        //перерисовка картинки
        void ReDraw()
        {
            if (currImage != null)
            {
                stopFlag = true;
                LoadingActivity(true);
                LoadAndPlay(
                     () => currASCIIImage = (Converter.ConvertImage(currImage, sizeModifier)),
                     () => DrawASCIIImage(currASCIIImage));
            }
            if (currASCIIAnimation != null)
            {
                stopFlag = true;
                LoadingActivity(true);
                LoadAndPlay(
                     () => Converter.ConvertAnimation(currASCIIAnimation, sizeModifier),
                     () => PlayASCIIAnimation(currASCIIAnimation));
            }
        }

        //диалоговое окно сохранения картинки
        private void save_button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Save ASCII Image";
            dlg.DefaultExt = ".txt";
            dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            if (dlg.ShowDialog() == true)
            {
                using (StreamWriter sw = new StreamWriter(dlg.OpenFile(), System.Text.Encoding.Default))
                {
                    SaveASCIIImage(sw);
                }
            }
        }
        void SaveASCIIImage(StreamWriter sw)
        {
            if (currASCIIImage != null)
            {
                foreach (string str in currASCIIImage.Split('\n'))
                {
                    sw.WriteLine(str);
                }

            };
            if (currASCIIAnimation !=null)
            {
                foreach(ASCIIAnimationFrame frame in currASCIIAnimation)
                {
                    foreach(string str in frame.asciiFrame.Split('\n'))
                    {
                        sw.WriteLine(str);
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NoImageButtonsBlock();
        }
    }
}
