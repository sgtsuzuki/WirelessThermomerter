using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO.Ports; // COMポート通信のための名前空間
using static System.Net.Mime.MediaTypeNames;

namespace WirelessThermomerter
{
    public partial class Form1 : Form
    {

        // 取得データの履歴
        const int MAX_HISTORY = 100;
        Queue<double> tempHistory = new Queue<double>();

        string id;
        string temperature;

        List<string> idList = new List<string>();
        List<string> tempList = new List<string>();

        public Form1()
        {
            InitializeComponent();

            // チャートの表示を初期化
            initChart(chart1);

            // 1秒周期でチャートを再描画
            timer1.Interval = 1000;
            timer1.Enabled = true;

            // 起動時にCOMポートの一覧リストを取得してコンボボックスに設定
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
            if (ports.Length > 0)
            {
                comboBox1.SelectedIndex = 0; // 最初のポートを選択
            }
        }

        private void initChart(Chart chart)
        {

            // チャート全体の背景色を設定
            chart.BackColor = Color.Black;
            chart.ChartAreas[0].BackColor = Color.Transparent;

            // チャート表示エリア周囲の余白をカットする
            chart.ChartAreas[0].InnerPlotPosition.Auto = false;
            chart.ChartAreas[0].InnerPlotPosition.Width = 100; // 100%
            chart.ChartAreas[0].InnerPlotPosition.Height = 90;  // 90%(横軸のメモリラベル印字分の余裕を設ける)
            chart.ChartAreas[0].InnerPlotPosition.X = 8;
            chart.ChartAreas[0].InnerPlotPosition.Y = 0;


            // X,Y軸情報のセット関数を定義
            Action<Axis> setAxis = (axisInfo) =>
            {
                // 軸のメモリラベルのフォントサイズ上限値を制限
                axisInfo.LabelAutoFitMaxFontSize = 8;

                // 軸のメモリラベルの文字色をセット
                axisInfo.LabelStyle.ForeColor = Color.White;

                // 軸タイトルの文字色をセット(今回はTitle未使用なので関係ないが...)
                axisInfo.TitleForeColor = Color.White;

                // 軸の色をセット
                axisInfo.MajorGrid.Enabled = true;
                axisInfo.MajorGrid.LineColor = ColorTranslator.FromHtml("#008242");
                axisInfo.MinorGrid.Enabled = false;
                axisInfo.MinorGrid.LineColor = ColorTranslator.FromHtml("#008242");
            }
            ;

            // X,Y軸の表示方法を定義
            setAxis(chart.ChartAreas[0].AxisY);
            setAxis(chart.ChartAreas[0].AxisX);
            chart.ChartAreas[0].AxisX.MinorGrid.Enabled = true;
            chart.ChartAreas[0].AxisY.Maximum = 500;    // 縦軸の最大値 orignal 100

            chart.AntiAliasing = AntiAliasingStyles.None;

            // 折れ線グラフとして表示
            chart.Series[0].ChartType = SeriesChartType.FastLine;
            // 線の色を指定
            chart.Series[0].Color = ColorTranslator.FromHtml("#00FF00");

            // 凡例を非表示,各値に数値を表示しない
            chart.Series[0].IsVisibleInLegend = false;
            chart.Series[0].IsValueShownAsLabel = false;

            // チャートに表示させる値の履歴を全て0クリア
            while (tempHistory.Count <= MAX_HISTORY)
            {
                tempHistory.Enqueue(0);
            }
        }



        private void showChart(Chart chart)
        {
            //-----------------------
            // チャートに値をセット
            //-----------------------
            chart.Series[0].Points.Clear();
            foreach (int value in tempHistory)
            {
                // データをチャートに追加
                chart.Series[0].Points.Add(new DataPoint(0, value));
            }
        }

        /// <summary>
        /// COMポートの接続ボタンがクリックされたときの処理 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            // 既にCOMポートは開いているか？
            if (serialPort1.IsOpen)
            {
                // MessageBox.Show("COMポートは既に開いています。");
                return; // 既に開いている場合は何もしない
            }

            if (comboBox1.SelectedItem != null)
            {
                string selectedPortNumber = comboBox1.SelectedItem.ToString();
                try
                {
                    serialPort1.PortName = selectedPortNumber; // 選択されたCOMポートを設定
                    serialPort1.Open();
                    serialPort1.DiscardInBuffer(); // 受信バッファをクリア
                    idList.Clear(); // IDリストをクリア
                    tempList.Clear(); // 温度リストをクリア

                }
                catch (Exception ex)
                {
                    MessageBox.Show("COMポートの接続に失敗しました: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("COMポートを選択してください。");
            }

        }

        // COMポート受信時の処理
        // "475,28.75\r\n"
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // データ受信処理
                //using (var serialPort1 = (SerialPort)sender)
                //{
                string data = serialPort1.ReadLine();
                Console.WriteLine("受信データ: " + data);
                //string data = serialPort1.ReadExisting();

                // 受信データをチェック 1,25.5 の様な形式なのでカンマ区切りで分離
                string[] parts = data.Split(',');
                if (parts.Length == 2)
                {
                    id = parts[0].Trim(); // ID部分
                    temperature = parts[1].Trim(); // 温度部分

                }
                else
                {
                    Console.WriteLine("受信データの形式が不正です: " + data);
                    return; // 不正なデータは無視
                }

                idList.Add(id); // IDをリストに追加
                tempList.Add(temperature);  // 温度をリストに追加

                this.Invoke(new MethodInvoker(() => countLabel.Text = id)); // IDをラベルに表示
                this.Invoke(new MethodInvoker(() => celsiusLabel.Text = temperature)); // 温度をラベルに表示

                // setChartData(temperature);
                this.Invoke(new MethodInvoker(() => setChartData(temperature)));
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show("データ受信中にエラーが発生しました: " + ex.Message);
            }
        }

        private void serialPort1_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Console.WriteLine("serialPort1_ErrorReceived");
            Console.WriteLine(e.EventType.ToString());
        }

        /// <summary>
        /// COMポートの切断ボタンがクリックされたときの処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            // シリアルを切断
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Close();
                    MessageBox.Show("COMポートを切断しました。");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("COMポートの切断に失敗しました: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("COMポートは開いていません。");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // idListとtempListの内容をCSVに出力する ファイル名は固定で良い
            string filePath = "output.csv"; // 出力ファイル名
            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
                {
                    // ヘッダーを書き込む
                    file.WriteLine("ID,Temperature");
                    // 各IDと温度をCSV形式で書き込む
                    for (int i = 0; i < idList.Count; i++)
                    {
                        file.WriteLine($"{idList[i]},{tempList[i]}");
                    }
                }
                MessageBox.Show("データをCSVファイルに出力しました: " + filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("CSVファイルの出力に失敗しました: " + ex.Message);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ////---------------------------------
            //// CPUの使用率を取得し、履歴に登録
            ////---------------------------------
            //// int value = (int)pc.NextValue();
            //var temp = 0;

            //tempHistory.Enqueue(temp);

            ////------------------------------------------------
            //// 履歴の最大数を超えていたら、古いものを削除する
            ////------------------------------------------------
            //while (tempHistory.Count > MAX_HISTORY)
            //{
            //    tempHistory.Dequeue();
            //}

            ////------------------------------------------------
            //// グラフを再描画する
            ////------------------------------------------------
            //showChart(chart1);
        }


        private void setChartData(string temptureString)
        {
            // temptureString を double値に変換
            double temp = double.Parse(temptureString);
            tempHistory.Enqueue(temp);

            //------------------------------------------------
            // 履歴の最大数を超えていたら、古いものを削除する
            //------------------------------------------------
            while (tempHistory.Count > MAX_HISTORY)
            {
                tempHistory.Dequeue();
            }

            //------------------------------------------------
            // グラフを再描画する
            //------------------------------------------------
            showChart(chart1);

        }
    }

}