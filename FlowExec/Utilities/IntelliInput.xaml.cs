using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
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

namespace FlowExec
{
    public partial class IntelliInput : UserControl
    {
        #region TextBox 基本依赖属性

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                name: "Text",
                propertyType: typeof(string),
                ownerType: typeof(IntelliInput),
                new FrameworkPropertyMetadata(
                    defaultValue: string.Empty,
                    flags: FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, // 支持双向绑定
                    propertyChangedCallback: OnTextChanged
                )
            );

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (IntelliInput)d;
            // 避免递归更新
            if (control._textBox.Text != control.Text)
            {
                control._textBox.Text = control.Text;
            }
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (IntelliInput)d;
            if ((bool)e.NewValue)
            {
                control.FocusTextBox();
            }
        }

        public static new readonly DependencyProperty IsFocusedProperty =
          DependencyProperty.Register(
              "IsFocused",
              typeof(bool),
              typeof(IntelliInput),
              new FrameworkPropertyMetadata(
                  false,
                  FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                  OnIsFocusedChanged));

        public new bool IsFocused
        {
            get { return (bool)GetValue(IsFocusedProperty); }
            set { SetValue(IsFocusedProperty, value); }
        }

        public event TextChangedEventHandler? TextChanged;
        public new event KeyEventHandler? PreviewKeyDown;

        #endregion

        #region IntelliInput 依赖属性

        // 补全建议列表
        public IEnumerable<string> CompletionItems
        {
            get => (IEnumerable<string>)GetValue(CompletionItemsProperty);
            set => SetValue(CompletionItemsProperty, value);
        }

        public static readonly DependencyProperty CompletionItemsProperty =
            DependencyProperty.Register("CompletionItems", typeof(IEnumerable<string>),
                typeof(IntelliInput), new PropertyMetadata(Array.Empty<string>()));

        // 历史记录文件路径
        public string HistoryFilePath
        {
            get => (string)GetValue(HistoryFilePathProperty);
            set => SetValue(HistoryFilePathProperty, value);
        }

        public static readonly DependencyProperty HistoryFilePathProperty =
            DependencyProperty.Register("HistoryFilePath", typeof(string),
                typeof(IntelliInput), new PropertyMetadata("History.txt"));

        #endregion

        private readonly List<string> _history = new List<string>();
        private int _historyIndex = -1;
        private List<string> _currentCompletions = new List<string>();
        private int _completionIndex = -1;
        private string _originalInput = "";
        private string _prefix = ""; // 当前词块之前的部分
        private string _suffix = ""; // 当前词块之后的部分
        private string _lastWord = ""; // 当前词块
        private int _lastWordStartIndex = 0; // 当前词块在文本中的起始位置
        private int _lastWordLength = 0; // 当前词块的长度

        public IntelliInput()
        {
            InitializeComponent();

            this.Loaded += (sender, e) =>
            {
                LoadHistory();
            };
            this.Unloaded += (sender, e) =>
            {
                SaveHistory();
            };

            _textBox.PreviewKeyDown += (sender, e) =>
            {
                PreviewKeyDown?.Invoke(this, e);
            };
            _textBox.TextChanged += (sender, e) =>
            {
                // 更新依赖属性值
                Text = _textBox.Text;
                // 触发用户控件暴露的事件
                TextChanged?.Invoke(sender, e);
            };

            _textBox.PreviewKeyDown += _textBox_PreviewKeyDown;
        }

        private void FocusTextBox()
        {
            // 使用Dispatcher确保在UI线程上执行
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _textBox.Focus();
                Keyboard.Focus(_textBox);
                // 可选：全选文本
                _textBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void _textBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                    HandleTabCompletion(e);
                    break;

                case Key.Up:
                    NavigateHistory(-1, e);
                    break;

                case Key.Down:
                    NavigateHistory(1, e);
                    break;

                case Key.Enter:
                    if (_textBox.SelectedText.Length == 0)
                    {
                        AddToHistory(Text);
                        ResetCompletionState();
                    }
                    else
                    {
                        var targetIndex = _textBox.CaretIndex + _textBox.SelectedText.Length;
                        _textBox.CaretIndex = targetIndex;
                        _textBox.Select(targetIndex, 0);
                    }
                    break;

                case Key.Escape:
                    ResetCompletionState();
                    if (!string.IsNullOrEmpty(_originalInput))
                    {
                        Text = _originalInput;
                        _textBox.CaretIndex = _originalInput.Length;
                    }
                    break;

                case Key.Right:
                    HandleRightArrowKey();
                    break;

                case Key.Space:
                    ResetCompletionState();
                    break;

                case Key.Back:
                case Key.Delete:
                    // 处理删除键时重置补全状态
                    if (_textBox.SelectedText.Length > 0)
                    {
                        _textBox.SelectedText = "";
                        e.Handled = true;
                    }
                    ResetCompletionState();
                    break;

                default:
                    // 用户输入新字符时重置补全状态
                    if (!e.Key.IsModifierKey())
                    {
                        ResetCompletionState();
                    }
                    break;
            }

            // Ctrl + Backspace 删除
            if (e.Key == Key.Back && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                DeletePreviousWord();
            }
        }

        private void HandleRightArrowKey()
        {
            // 如果有选中文本（补全部分），按右箭头键时移动到文本末尾
            if (_textBox.SelectionLength > 0)
            {
                _textBox.CaretIndex = Text.Length;
                _textBox.SelectionLength = 0;
            }
        }

        #region 自动补全功能

        private void HandleTabCompletion(KeyEventArgs e)
        {
            e.Handled = true;

            // 获取当前输入
            var input = Text ?? "";
            var caretIndex = _textBox.CaretIndex;

            // 如果当前没有激活补全，尝试初始化补全状态
            if (_completionIndex == -1)
            {
                // 空输入时不触发补全
                if (string.IsNullOrWhiteSpace(input))
                {
                    SystemSounds.Beep.Play();
                    return;
                }

                _originalInput = input;

                // 根据光标位置解析输入
                ParseInputAtCaret(input, caretIndex);

                // 没有可补全的词块时播放提示音
                if (string.IsNullOrEmpty(_lastWord))
                {
                    SystemSounds.Beep.Play();
                    return;
                }

                _currentCompletions = GetCompletions(_lastWord).ToList();

                if (_currentCompletions.Count == 0)
                {
                    // 没有补全项时播放提示音
                    SystemSounds.Beep.Play();
                    return;
                }

                _completionIndex = 0;
                ApplyCompletion(_currentCompletions[0]);
                return;
            }

            // 循环选择补全项
            if (_currentCompletions.Count == 0) return;

            // 确定方向
            var direction = (e.KeyboardDevice.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? -1 : 1;

            _completionIndex = (_completionIndex + direction + _currentCompletions.Count) % _currentCompletions.Count;
            ApplyCompletion(_currentCompletions[_completionIndex]);
        }

        // 根据光标位置解析输入
        private void ParseInputAtCaret(string input, int caretIndex)
        {
            // 重置状态
            _prefix = "";
            _suffix = "";
            _lastWord = "";
            _lastWordStartIndex = 0;
            _lastWordLength = 0;

            // 使用CommandLine.Parse方法分割输入
            var parts = Parse(input);

            if (parts.Count == 0)
            {
                // 整个输入作为当前词块
                _lastWord = input;
                _lastWordStartIndex = 0;
                _lastWordLength = input.Length;
                return;
            }

            // 找到光标所在的词块
            int currentPartEnd = 0;
            string currentWord = "";
            int currentWordStart = 0;

            foreach (var part in parts)
            {
                // 计算词块的结束位置
                currentPartEnd = currentWordStart + part.Length;

                // 检查光标是否在此词块范围内
                if (caretIndex >= currentWordStart && caretIndex <= currentPartEnd)
                {
                    currentWord = part;
                    break;
                }

                // 移动到下一个词块
                currentWordStart = currentPartEnd + 1; // +1 为空格
            }

            // 如果没找到词块，使用最后一个词块
            if (string.IsNullOrEmpty(currentWord) && parts.Count > 0)
            {
                currentWord = parts.Last();
                currentWordStart = input.LastIndexOf(currentWord, StringComparison.Ordinal);
            }

            if (string.IsNullOrEmpty(currentWord)) return;

            // 记录当前词块信息
            _lastWord = currentWord;
            _lastWordStartIndex = currentWordStart;
            _lastWordLength = currentWord.Length;

            // 计算前缀（当前词块之前的所有内容）
            if (_lastWordStartIndex > 0)
            {
                _prefix = input.Substring(0, _lastWordStartIndex);
            }

            // 计算后缀（当前词块之后的所有内容）
            int suffixStart = _lastWordStartIndex + _lastWordLength;
            if (suffixStart < input.Length)
            {
                _suffix = input.Substring(suffixStart);
            }
        }

        private IEnumerable<string> GetCompletions(string lastWord)
        {
            return CompletionItems?
                .Where(item => item.StartsWith(lastWord, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item)
                .ToList() ?? Enumerable.Empty<string>();
        }

        private void ApplyCompletion(string completion)
        {
            // 应用补全：前缀 + 补全内容 + 后缀
            string fullText = _prefix + completion + _suffix;
            Text = fullText;

            // 设置光标位置在补全内容之后
            int newCaretIndex = _prefix.Length + completion.Length;
            _textBox.CaretIndex = newCaretIndex;

            // 如果补全项比原始词块长，选中补全部分
            if (completion.Length > _lastWord.Length)
            {
                _textBox.SelectionStart = _prefix.Length + _lastWord.Length;
                _textBox.SelectionLength = completion.Length - _lastWord.Length;
            }
        }

        private void ResetCompletionState()
        {
            _completionIndex = -1;
            _currentCompletions.Clear();
            _originalInput = "";
            _prefix = "";
            _suffix = "";
            _lastWord = "";
            _lastWordStartIndex = 0;
            _lastWordLength = 0;
            _textBox.SelectionLength = 0; // 清除选中状态
        }

        #endregion

        #region 历史记录功能

        private void LoadHistory()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    _history.AddRange(File.ReadAllLines(HistoryFilePath).Reverse());
                }
            }
            catch { /* 忽略加载错误 */ }
        }

        private void SaveHistory()
        {
            try
            {
                if (_history.Count > 0)
                {
                    File.WriteAllLines(HistoryFilePath, _history.Take(100)); // 最多保存100条
                }
            }
            catch { /* 忽略保存错误 */ }
        }

        private void AddToHistory(string item)
        {
            if (string.IsNullOrWhiteSpace(item)) return;

            // 移除重复项
            _history.RemoveAll(x => string.Equals(x, item, StringComparison.OrdinalIgnoreCase));

            // 添加到顶部
            _history.Insert(0, item);
        }

        private void NavigateHistory(int direction, KeyEventArgs e)
        {
            e.Handled = true;
            ResetCompletionState(); // 导航历史时重置补全状态

            if (_history.Count == 0) return;

            // 初始化索引
            if (_historyIndex == -1)
            {
                // 保存当前输入作为"最新"项
                if (!string.IsNullOrWhiteSpace(Text))
                {
                    _historyIndex = 0;
                    _history.Insert(0, Text);
                }
                else
                {
                    _historyIndex = -1;
                }
            }

            // 计算新索引
            _historyIndex = Math.Clamp(_historyIndex + direction, 0, _history.Count - 1);

            // 显示历史记录
            Text = _history[_historyIndex];
            _textBox.CaretIndex = Text.Length;
        }


        #endregion

        #region 快速删除

        // 词块分隔符
        private const string DefaultDelimiters = "/\\.,;:|!@#$%^&*()+=[]{}<>\"\'~`? \t\n\r";

        private bool IsDelimiter(char c) => DefaultDelimiters.Contains(c);

        private void DeletePreviousWord()
        {
            int caretIndex = _textBox.CaretIndex;
            if (caretIndex == 0) return;

            string text = _textBox.Text;
            int startIndex = caretIndex;
            bool isFirstDelimiter = true;

            while (startIndex > 0)
            {
                startIndex--;
                if (IsDelimiter(text[startIndex]))
                {
                    if (!isFirstDelimiter)
                    {
                        startIndex++;
                        break;
                    }
                }
                else if (isFirstDelimiter)
                {
                    isFirstDelimiter = false;
                }
            }

            // 执行删除操作
            _textBox.Text = text.Remove(startIndex, caretIndex - startIndex);
            _textBox.CaretIndex = startIndex;
        }

        #endregion

        #region 命令行分割

        private static List<string> Parse(string input)
        {
            List<string> tokens = new List<string>();
            StringBuilder currentToken = new StringBuilder();
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool escapeNext = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (escapeNext)
                {
                    // 转义字符：直接添加下一个字符（无论是什么）
                    currentToken.Append(c);
                    escapeNext = false;
                    continue;
                }

                switch (c)
                {
                    case '\\':
                        // 设置转义标志（下一个字符将被直接添加）
                        escapeNext = true;
                        break;

                    case '\'':
                        if (!inDoubleQuote)
                            inSingleQuote = !inSingleQuote; // 切换单引号状态
                        else
                            currentToken.Append(c); // 双引号内的单引号作为普通字符
                        break;

                    case '"':
                        if (!inSingleQuote)
                            inDoubleQuote = !inDoubleQuote; // 切换双引号状态
                        else
                            currentToken.Append(c); // 单引号内的双引号作为普通字符
                        break;

                    case ' ':
                        if (inSingleQuote || inDoubleQuote)
                        {
                            // 引号内的空格保留
                            currentToken.Append(c);
                        }
                        else if (currentToken.Length > 0)
                        {
                            // 结束当前令牌
                            tokens.Add(currentToken.ToString());
                            currentToken.Clear();
                        }
                        break;

                    default:
                        // 所有其他字符直接添加到当前令牌
                        currentToken.Append(c);
                        break;
                }
            }

            // 处理最后一个令牌
            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            return tokens;
        }

        public List<string> Parse()
        {
            return Parse(Text);
        }

        #endregion

    }

    public static class KeyExtensions
    {
        public static bool IsModifierKey(this Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LWin || key == Key.RWin;
        }
    }
}
