using System.IO;
using System.Text;
using System.Windows;
using FigmaToXaml.Services;
using Microsoft.Win32;

namespace FigmaToXaml;

public partial class MainWindow : Window
{
    private readonly FigmaXamlConverter _converter = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void LoadJsonButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "JsonExport JSON 열기",
            Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        JsonInputTextBox.Text = File.ReadAllText(dialog.FileName, Encoding.UTF8);
        StatusTextBlock.Text = $"JSON을 불러왔습니다: {Path.GetFileName(dialog.FileName)}";
    }

    private void ConvertButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            XamlOutputTextBox.Text = _converter.ConvertJson(JsonInputTextBox.Text);
            StatusTextBlock.Text = "XAML 변환이 완료되었습니다.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"변환 실패: {ex.Message}";
            MessageBox.Show(this, ex.Message, "변환 실패", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyXamlButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(XamlOutputTextBox.Text))
        {
            StatusTextBlock.Text = "복사할 XAML이 없습니다.";
            return;
        }

        Clipboard.SetText(XamlOutputTextBox.Text);
        StatusTextBlock.Text = "XAML을 클립보드에 복사했습니다.";
    }

    private void SaveXamlButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(XamlOutputTextBox.Text))
        {
            StatusTextBlock.Text = "저장할 XAML이 없습니다.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "XAML 저장",
            Filter = "XAML files (*.xaml)|*.xaml|All files (*.*)|*.*",
            FileName = "FigmaExport.xaml",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, XamlOutputTextBox.Text, Encoding.UTF8);
        StatusTextBlock.Text = $"XAML을 저장했습니다: {Path.GetFileName(dialog.FileName)}";
    }
}
