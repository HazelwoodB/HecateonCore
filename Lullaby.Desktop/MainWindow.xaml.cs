using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Lullaby.Desktop;

public class ChatMessage
{
    public string Text { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public Visibility IsUserVisibility => IsUser ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IsAssistantVisibility => !IsUser ? Visibility.Visible : Visibility.Collapsed;
}

public partial class MainWindow : Window
{
    private ObservableCollection<ChatMessage> _chatMessages = new();
    private bool _isWaitingForResponse = false;
    private string _deviceId = Guid.NewGuid().ToString().Substring(0, 12);
    private int _selectedMood = 0;
    private HttpClient? _httpClient;

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            InitializeApp();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Init error: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void InitializeApp()
    {
        DeviceIdBox.Text = _deviceId;

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://localhost:5001");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);

        ChatInput.KeyDown += ChatInput_KeyDown;
        SleepSlider.ValueChanged += SleepSlider_ValueChanged;
        MessagesList.ItemsSource = _chatMessages;

        LoadDemoChat();
    }

    private void ChatInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SendMessage_Click(null, null);
            e.Handled = true;
        }
    }

    private void SleepSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        SleepLabel.Text = $"{e.NewValue:F1} hours";
    }

    private void LoadDemoChat()
    {
        _chatMessages.Clear();
        _chatMessages.Add(new ChatMessage 
        { 
            Text = "👋 Welcome to Lullaby! I'm your mental health companion.",
            IsUser = false,
            Timestamp = DateTime.Now.AddMinutes(-3)
        });
        _chatMessages.Add(new ChatMessage
        {
            Text = "How does this work?",
            IsUser = true,
            Timestamp = DateTime.Now.AddMinutes(-2)
        });
        _chatMessages.Add(new ChatMessage
        {
            Text = "Chat anytime, track mood & sleep. Everything is private and secure.",
            IsUser = false,
            Timestamp = DateTime.Now.AddMinutes(-1)
        });
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (MessagesList.Items.Count > 0)
        {
            MessagesList.ScrollIntoView(MessagesList.Items[MessagesList.Items.Count - 1]);
        }
    }

    private async void SendMessage_Click(object sender, RoutedEventArgs e)
    {
        var message = ChatInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(message) || _isWaitingForResponse)
            return;

        _chatMessages.Add(new ChatMessage { Text = message, IsUser = true, Timestamp = DateTime.Now });
        ChatInput.Clear();
        ChatInput.Focus();
        ScrollToBottom();
        _isWaitingForResponse = true;

        try
        {
            if (_httpClient == null) throw new Exception("HTTP client not initialized");
            
            var response = await _httpClient.PostAsJsonAsync("/api/chat", new { message = message });

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var responseData = JsonSerializer.Deserialize<JsonElement>(content, options);
                
                string assistantMessage = "I'm here to help.";
                if (responseData.TryGetProperty("response", out var resp))
                    assistantMessage = resp.GetString() ?? assistantMessage;

                _chatMessages.Add(new ChatMessage { Text = assistantMessage, IsUser = false, Timestamp = DateTime.Now });
                ScrollToBottom();
            }
            else
            {
                ShowError($"Error: {response.StatusCode}");
                if (_chatMessages.Count > 0) _chatMessages.RemoveAt(_chatMessages.Count - 1);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
            if (_chatMessages.Count > 0) _chatMessages.RemoveAt(_chatMessages.Count - 1);
        }
        finally
        {
            _isWaitingForResponse = false;
        }
    }

    private void MoodButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int mood))
        {
            _selectedMood = mood;
            var labels = new[] { "", "😢 Awful", "😞 Bad", "😐 Okay", "😌 Good", "😊 Great" };
            MoodLabel.Text = labels[mood];
        }
    }

    private async void LogHealth_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMood == 0)
        {
            MessageBox.Show("Select mood first", "Required", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            if (_httpClient == null) throw new Exception("HTTP client not initialized");

            var response = await _httpClient.PostAsJsonAsync("/api/health/log", new
            {
                mood = _selectedMood,
                sleep = Math.Round(SleepSlider.Value, 1),
                timestamp = DateTime.UtcNow
            });

            if (response.IsSuccessStatusCode)
            {
                HealthStatus.Text = $"✓ {MoodLabel.Text} • 🛏️ {SleepSlider.Value:F1}h";
                MessageBox.Show("✓ Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
    }

    private void ToggleRecoveryCode_Click(object sender, RoutedEventArgs e)
    {
        if (RecoveryCodeBox.Text.Length == 0)
        {
            RecoveryCodeBox.Text = "CHANGE_THIS_RECOVERY_CODE";
            ShowRecoveryBtn.Content = "Hide";
        }
        else
        {
            RecoveryCodeBox.Text = "";
            ShowRecoveryBtn.Content = "Show";
        }
    }

    private void CopyDeviceId_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(DeviceIdBox.Text);
            MessageBox.Show("✓ Copied", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch { }
    }

    private void ShowError(string message)
    {
        ErrorMessage.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }
}