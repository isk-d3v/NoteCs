using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SqlViewer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    class MainForm : Form
    {
        private TabControl tabControl;
        private TabPage tabText;
        private TabPage tabGrid;
        private RichTextBox rtbContent;
        private DataGridView dgvData;
        private ToolStripMenuItem _saveMenuItem;
        private bool _darkMode = false;
        private CheckBox chkWordWrap;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private Panel searchPanel;
        private TextBox txtSearch;
        private Button btnSearchNext;
        private Button btnSearchPrev;
        private CheckBox chkMatchCase;
        private Label lblMatches;
        private int _searchStart = 0;
        private string _currentExt = "";
        private string _currentPath = "";
        private string _savedContent = "";
        private bool _isDirty = false;
        private bool _suppressTextChanged = false;
        private System.Windows.Forms.Timer _autosaveTimer;
        private CheckBox chkAutosave;
        private bool _autosaveEnabled = false;
        private Panel lineNumberPanel;
        private MenuStrip menuStrip;
        private VScrollBar vScrollBar;

        public MainForm()
        {
            Text = "File Viewer";
            Size = new Size(1000, 680);
            MinimumSize = new Size(640, 420);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            BuildTitleBar();
            BuildSearchBar();
            BuildTabs();
            BuildStatusBar();

            _autosaveTimer = new System.Windows.Forms.Timer();
            _autosaveTimer.Interval = 1000;
            _autosaveTimer.Tick += (s, e) =>
            {
                _autosaveTimer.Stop();
                if (_isDirty && _currentPath != "")
                {
                    SaveFile();
                    lblStatus.Text = "[Autosaved]  " + lblStatus.Text;
                }
            };
        }

        

        void BuildTitleBar()
        {
            menuStrip = new MenuStrip();
            menuStrip.BackColor = Color.FromArgb(45, 45, 48);
            menuStrip.ForeColor = Color.White;
            menuStrip.Renderer = new DarkMenuRenderer();

            
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");

            ToolStripMenuItem openItem = new ToolStripMenuItem("Open...");
            openItem.ShortcutKeys = Keys.Control | Keys.O;
            openItem.Click += (s, e) => OpenFile();

            _saveMenuItem = new ToolStripMenuItem("Save");
            _saveMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            _saveMenuItem.Click += (s, e) => SaveFile();

            ToolStripMenuItem clearItem = new ToolStripMenuItem("Clear");
            clearItem.Click += (s, e) => ClearAll();

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => Close();

            fileMenu.DropDownItems.Add(openItem);
            fileMenu.DropDownItems.Add(_saveMenuItem);
            fileMenu.DropDownItems.Add(clearItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);

            
            ToolStripMenuItem editMenu = new ToolStripMenuItem("Edit");

            ToolStripMenuItem copyItem = new ToolStripMenuItem("Copy All");
            copyItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.C;
            copyItem.Click += (s, e) => CopyAll();

            ToolStripMenuItem findItem = new ToolStripMenuItem("Find...");
            findItem.ShortcutKeys = Keys.Control | Keys.F;
            findItem.Click += (s, e) => FocusSearch();

            editMenu.DropDownItems.Add(copyItem);
            editMenu.DropDownItems.Add(findItem);

            
            ToolStripMenuItem viewMenu = new ToolStripMenuItem("View");

            ToolStripMenuItem wordWrapItem = new ToolStripMenuItem("Word Wrap");
            wordWrapItem.CheckOnClick = true;
            wordWrapItem.CheckedChanged += (s, e) =>
            {
                rtbContent.WordWrap = wordWrapItem.Checked;
                if (chkWordWrap != null) chkWordWrap.Checked = wordWrapItem.Checked;
            };
            chkWordWrap = new CheckBox();

            viewMenu.DropDownItems.Add(wordWrapItem);

            
            ToolStripMenuItem settingsMenu = new ToolStripMenuItem("Settings");

            ToolStripMenuItem autosaveItem = new ToolStripMenuItem("Autosave on change");
            autosaveItem.CheckOnClick = true;
            autosaveItem.CheckedChanged += (s, e) =>
            {
                _autosaveEnabled = autosaveItem.Checked;
                if (_autosaveEnabled) _autosaveTimer.Start();
                else _autosaveTimer.Stop();
                if (chkAutosave != null) chkAutosave.Checked = autosaveItem.Checked;
            };
            chkAutosave = new CheckBox();

            settingsMenu.DropDownItems.Add(autosaveItem);

            ToolStripMenuItem darkModeItem = new ToolStripMenuItem("Dark Mode");
            darkModeItem.CheckOnClick = true;
            darkModeItem.CheckedChanged += (s, e) =>
            {
                _darkMode = darkModeItem.Checked;
                ApplyTheme();
            };
            settingsMenu.DropDownItems.Add(darkModeItem);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(editMenu);
            menuStrip.Items.Add(viewMenu);
            menuStrip.Items.Add(settingsMenu);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
        }

        

        void ApplyTheme()
        {
            Color bg = _darkMode ? Color.FromArgb(30, 30, 30) : Color.White;
            Color fg = _darkMode ? Color.FromArgb(220, 220, 220) : Color.Black;
            Color panelBg = _darkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(240, 240, 240);
            Color searchBg = _darkMode ? Color.FromArgb(50, 50, 52) : SystemColors.Info;
            Color statusBg = _darkMode ? Color.FromArgb(37, 37, 38) : SystemColors.Control;
            Color gridBg = _darkMode ? Color.FromArgb(30, 30, 30) : Color.White;
            Color gridHeader = _darkMode ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
            Color gridFg = _darkMode ? Color.FromArgb(220, 220, 220) : Color.Black;
            Color gridLine = _darkMode ? Color.FromArgb(60, 60, 60) : SystemColors.ControlDark;

            rtbContent.BackColor = bg;
            rtbContent.ForeColor = fg;

            lineNumberPanel.BackColor = panelBg;
            lineNumberPanel.Invalidate();

            searchPanel.BackColor = searchBg;
            foreach (Control c in searchPanel.Controls)
            {
                if (c is Label l) { l.ForeColor = fg; }
                if (c is TextBox t) { t.BackColor = bg; t.ForeColor = fg; }
                if (c is Button b) { b.BackColor = panelBg; b.ForeColor = fg; }
                if (c is CheckBox ch) { ch.BackColor = searchBg; ch.ForeColor = fg; }
            }

            statusStrip.BackColor = statusBg;
            lblStatus.ForeColor = _darkMode ? Color.FromArgb(180, 180, 180) : SystemColors.ControlText;

            dgvData.BackgroundColor = gridBg;
            dgvData.DefaultCellStyle.BackColor = gridBg;
            dgvData.DefaultCellStyle.ForeColor = gridFg;
            dgvData.DefaultCellStyle.SelectionBackColor = _darkMode ? Color.FromArgb(62, 62, 66) : SystemColors.Highlight;
            dgvData.DefaultCellStyle.SelectionForeColor = _darkMode ? Color.White : SystemColors.HighlightText;
            dgvData.ColumnHeadersDefaultCellStyle.BackColor = gridHeader;
            dgvData.ColumnHeadersDefaultCellStyle.ForeColor = gridFg;
            dgvData.ColumnHeadersDefaultCellStyle.SelectionBackColor = gridHeader;
            dgvData.GridColor = gridLine;
            dgvData.EnableHeadersVisualStyles = false;

            BackColor = _darkMode ? Color.FromArgb(37, 37, 38) : SystemColors.Control;

            if (rtbContent.Text.Length > 0)
            {
                string reloadMode = GetModeForExt(_currentExt);
                if (reloadMode != "plain" && reloadMode != "csv" && reloadMode != "db" && reloadMode != "xlsx")
                {
                    LoadTextFile(_currentPath, reloadMode);
                }
                else
                {
                    switch (_currentExt)
                    {
                        case ".csv":
                            HighlightCsv(rtbContent, rtbContent.Text);
                            break;
                        default:
                            bool wasSuppressed = _suppressTextChanged;
                            _suppressTextChanged = true;
                            int cp = rtbContent.SelectionStart;
                            int cl = rtbContent.SelectionLength;
                            rtbContent.SelectAll();
                            rtbContent.SelectionColor = fg;
                            rtbContent.SelectionBackColor = bg;
                            rtbContent.SelectionStart = 0;
                            rtbContent.Select(cp, cl);
                            _suppressTextChanged = wasSuppressed;
                            break;
                    }
                    if (_isDirty) MarkModifiedLines();
                }
            }
        }

        

        void BuildSearchBar()
        {
            searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = SystemColors.Info,
                Padding = new Padding(4, 4, 4, 4),
                Visible = false
            };

            Label lblFind = new Label { Text = "Find:", Left = 6, Top = 9, AutoSize = true };

            txtSearch = new TextBox { Left = 42, Top = 6, Width = 200, Height = 22 };
            txtSearch.TextChanged += (s, e) => { _searchStart = 0; UpdateMatchCount(); };
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SearchNext(); }
                if (e.KeyCode == Keys.Escape) HideSearch();
            };

            btnSearchNext = new Button { Text = "Next", Width = 60, Height = 24, Left = 250, Top = 6 };
            btnSearchNext.Click += (s, e) => SearchNext();

            btnSearchPrev = new Button { Text = "Prev", Width = 60, Height = 24, Left = 318, Top = 6 };
            btnSearchPrev.Click += (s, e) => SearchPrev();

            chkMatchCase = new CheckBox { Text = "Match case", Left = 390, Top = 9, AutoSize = true };
            chkMatchCase.CheckedChanged += (s, e) => { _searchStart = 0; UpdateMatchCount(); };

            lblMatches = new Label { Left = 500, Top = 9, AutoSize = true, ForeColor = Color.Gray };

            Button btnClose = new Button { Text = "X", Width = 24, Height = 24, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => HideSearch();

            searchPanel.Controls.Add(lblFind);
            searchPanel.Controls.Add(txtSearch);
            searchPanel.Controls.Add(btnSearchNext);
            searchPanel.Controls.Add(btnSearchPrev);
            searchPanel.Controls.Add(chkMatchCase);
            searchPanel.Controls.Add(lblMatches);
            searchPanel.Controls.Add(btnClose);
            Controls.Add(searchPanel);
        }

        

        void BuildTabs()
        {
            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabText = new TabPage("Source");
            tabGrid = new TabPage("Table View");

            Panel editorContainer = new Panel { Dock = DockStyle.Fill };

            lineNumberPanel = new Panel
            {
                Width = 48,
                Dock = DockStyle.Left,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            lineNumberPanel.Paint += (s, e) => DrawLineNumbers(e.Graphics);

            vScrollBar = new VScrollBar { Dock = DockStyle.Right, Width = 17 };
            vScrollBar.Scroll += (s, e) =>
            {
                NativeMethods.SetScrollPos(rtbContent.Handle, 1, vScrollBar.Value, true);
                NativeMethods.SendMessage(rtbContent.Handle, 0x115, (IntPtr)(4 + (vScrollBar.Value << 16)), IntPtr.Zero);
                lineNumberPanel.Invalidate();
            };

            rtbContent = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10f),
                ScrollBars = RichTextBoxScrollBars.Horizontal,
                WordWrap = false,
                ReadOnly = false,
                DetectUrls = false,
                BackColor = Color.White
            };
            rtbContent.AllowDrop = true;
            rtbContent.DragEnter += OnDragEnter;
            rtbContent.DragDrop += OnDragDrop;
            rtbContent.KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.F) { e.SuppressKeyPress = true; FocusSearch(); }
                if (e.KeyCode == Keys.Escape && searchPanel.Visible) HideSearch();
                if (e.Control && !e.Shift && e.KeyCode == Keys.S) { e.SuppressKeyPress = true; SaveFile(); }
            };
            rtbContent.TextChanged += OnTextChanged;
            rtbContent.VScroll += (s, e) => { SyncScrollBar(); lineNumberPanel.Invalidate(); };
            rtbContent.MouseWheel += (s, e) => { SyncScrollBar(); lineNumberPanel.Invalidate(); };
            rtbContent.KeyUp += (s, e) => { SyncScrollBar(); lineNumberPanel.Invalidate(); };
            rtbContent.Click += (s, e) => lineNumberPanel.Invalidate();
            rtbContent.HandleCreated += (s, e) =>
            {
                NativeMethods.RECT rc = new NativeMethods.RECT
                {
                    left = 2,
                    top = 0,
                    right = rtbContent.ClientSize.Width - 2,
                    bottom = rtbContent.ClientSize.Height
                };
                NativeMethods.SendMessageRect(rtbContent.Handle, 0x400 + 68, IntPtr.Zero, ref rc);
            };

            editorContainer.Controls.Add(rtbContent);
            editorContainer.Controls.Add(vScrollBar);
            editorContainer.Controls.Add(lineNumberPanel);
            tabText.Controls.Add(editorContainer);

            dgvData = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            tabGrid.Controls.Add(dgvData);

            tabControl.TabPages.Add(tabText);
            tabControl.TabPages.Add(tabGrid);

            AllowDrop = true;
            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.F) FocusSearch();
                if (e.Control && !e.Shift && e.KeyCode == Keys.S) SaveFile();
            };

            Controls.Add(tabControl);
        }

        

        void BuildStatusBar()
        {
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel("Ready. Supported: .sql  .db  .csv  .json  .xlsx");
            lblStatus.Spring = true;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            statusStrip.Items.Add(lblStatus);
            Controls.Add(statusStrip);
        }

        

        void OnTextChanged(object sender, EventArgs e)
        {
            if (_suppressTextChanged || _savedContent == null) return;
            if (!_isDirty)
            {
                _isDirty = true;
                _saveMenuItem.Enabled = true;
                if (!Text.StartsWith("*")) Text = "* " + Text;
            }
            MarkModifiedLines();
            if (_autosaveEnabled && _currentPath != "")
            {
                _autosaveTimer.Stop();
                _autosaveTimer.Start();
            }
        }

        void MarkModifiedLines()
        {
            if (_suppressTextChanged) return;
            _suppressTextChanged = true;

            int caretPos = rtbContent.SelectionStart;
            int caretLen = rtbContent.SelectionLength;

            string[] currentLines = rtbContent.Text.Split('\n');
            string[] savedLines = _savedContent.Split('\n');

            int charOffset = 0;
            for (int i = 0; i < currentLines.Length; i++)
            {
                string cur = currentLines[i].TrimEnd('\r');
                string saved = i < savedLines.Length ? savedLines[i].TrimEnd('\r') : null;
                bool changed = saved == null || cur != saved;
                int lineLen = currentLines[i].Length;

                rtbContent.Select(charOffset, lineLen);
                rtbContent.SelectionBackColor = changed
                    ? Color.FromArgb(0, 255, 100)
                    : rtbContent.BackColor;

                charOffset += lineLen + 1;
            }

            rtbContent.Select(caretPos, caretLen);
            _suppressTextChanged = false;
        }

        

        void SaveFile()
        {
            if (_currentPath == "")
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Title = "Save File";
                    dlg.Filter = "All supported files|*.sql;*.csv;*.json|SQL Files (*.sql)|*.sql|CSV Files (*.csv)|*.csv|JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                    dlg.FileName = "file.sql";
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    _currentPath = dlg.FileName;
                }
            }

            try
            {
                File.WriteAllText(_currentPath, rtbContent.Text, Encoding.UTF8);
                _savedContent = rtbContent.Text;
                _isDirty = false;
                _saveMenuItem.Enabled = false;

                _suppressTextChanged = true;
                int caretPos = rtbContent.SelectionStart;
                int caretLen = rtbContent.SelectionLength;
                rtbContent.SelectAll();
                rtbContent.SelectionBackColor = rtbContent.BackColor;
                rtbContent.Select(caretPos, caretLen);
                _suppressTextChanged = false;

                FileInfo fi = new FileInfo(_currentPath);
                Text = "File Viewer - " + fi.Name;
                lblStatus.Text = "Saved: " + fi.FullName + "  |  " + rtbContent.Text.Split('\n').Length + " lines";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        

        void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        void OnDragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0) LoadFile(files[0]);
        }

        void OpenFile()
        {
            if (_isDirty)
            {
                DialogResult r = MessageBox.Show(
                    "You have unsaved changes. Open a new file anyway?",
                    "Unsaved changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (r != DialogResult.Yes) return;
            }
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open File";
                dlg.Filter =
                    "All supported files|*.sql;*.db;*.csv;*.json;*.xlsx;*.py;*.cs;*.cpp;*.c;*.h;*.hpp;*.js;*.ts;*.jsx;*.tsx;*.html;*.htm;*.css;*.xml;*.yaml;*.yml;*.toml;*.ini;*.cfg;*.md;*.txt;*.sh;*.bat;*.ps1;*.rb;*.go;*.rs;*.java;*.kt;*.swift;*.php;*.lua;*.r;*.m;*.vb;*.fs;*.dart;*.scala|" +
                    "Database|*.sql;*.db|" +
                    "Data|*.csv;*.json;*.xlsx;*.xml;*.yaml;*.yml;*.toml|" +
                    "Python (*.py)|*.py|" +
                    "C# (*.cs)|*.cs|" +
                    "C/C++ (*.c;*.cpp;*.h;*.hpp)|*.c;*.cpp;*.h;*.hpp|" +
                    "JavaScript/TypeScript|*.js;*.ts;*.jsx;*.tsx|" +
                    "Web (*.html;*.css)|*.html;*.htm;*.css|" +
                    "Config/Markup|*.xml;*.yaml;*.yml;*.toml;*.ini;*.cfg;*.md|" +
                    "Scripts|*.sh;*.bat;*.ps1|" +
                    "Other languages|*.rb;*.go;*.rs;*.java;*.kt;*.swift;*.php;*.lua;*.r;*.vb;*.fs;*.dart;*.scala|" +
                    "Text files (*.txt)|*.txt|" +
                    "All Files (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                    LoadFile(dlg.FileName);
            }
        }

        void LoadFile(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("File not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _currentExt = Path.GetExtension(path).ToLowerInvariant();
            _currentPath = path;
            _isDirty = false;
            _saveMenuItem.Enabled = false;
            lblStatus.Text = "Loading...";
            Application.DoEvents();

            try
            {
                FileInfo fi = new FileInfo(path);
                Text = "File Viewer - " + fi.Name;

                switch (_currentExt)
                {
                    case ".sql": LoadTextFile(path, "sql"); break;
                    case ".json": LoadTextFile(path, "json"); break;
                    case ".csv": LoadCsv(path); break;
                    case ".db": LoadSqliteDb(path); break;
                    case ".xlsx": LoadXlsx(path); break;
                    case ".py": LoadTextFile(path, "python"); break;
                    case ".cs":
                    case ".java":
                    case ".kt":
                    case ".swift":
                    case ".dart":
                    case ".scala":
                    case ".vb":
                    case ".fs": LoadTextFile(path, "csharp"); break;
                    case ".cpp":
                    case ".c":
                    case ".h":
                    case ".hpp": LoadTextFile(path, "cpp"); break;
                    case ".js":
                    case ".ts":
                    case ".jsx":
                    case ".tsx": LoadTextFile(path, "js"); break;
                    case ".html":
                    case ".htm": LoadTextFile(path, "html"); break;
                    case ".css": LoadTextFile(path, "css"); break;
                    case ".xml": LoadTextFile(path, "xml"); break;
                    case ".yaml":
                    case ".yml": LoadTextFile(path, "yaml"); break;
                    case ".toml":
                    case ".ini":
                    case ".cfg": LoadTextFile(path, "toml"); break;
                    case ".md": LoadTextFile(path, "md"); break;
                    case ".sh":
                    case ".bat":
                    case ".ps1": LoadTextFile(path, "shell"); break;
                    case ".rb": LoadTextFile(path, "ruby"); break;
                    case ".go": LoadTextFile(path, "go"); break;
                    case ".rs": LoadTextFile(path, "rust"); break;
                    case ".php": LoadTextFile(path, "php"); break;
                    case ".lua": LoadTextFile(path, "lua"); break;
                    case ".r":
                    case ".m": LoadTextFile(path, "r"); break;
                    default: LoadTextFile(path, "plain"); break;
                }

                _searchStart = 0;
                UpdateMatchCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read file:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Failed to load file.";
            }
        }

        void LoadTextFile(string path, string mode)
        {
            _suppressTextChanged = true;
            Encoding enc = DetectEncoding(path);
            string content = File.ReadAllText(path, enc);
            _savedContent = content;
            FileInfo fi = new FileInfo(path);
            int lines = content.Split('\n').Length;

            ClearGrid();
            tabControl.SelectedTab = tabText;

            switch (mode)
            {
                case "sql": HighlightSql(rtbContent, content); break;
                case "json": HighlightJson(rtbContent, content); break;
                case "python": HighlightPython(rtbContent, content); break;
                case "csharp": HighlightCSharp(rtbContent, content); break;
                case "cpp": HighlightCpp(rtbContent, content); break;
                case "js": HighlightJs(rtbContent, content); break;
                case "html": HighlightHtml(rtbContent, content); break;
                case "css": HighlightCss(rtbContent, content); break;
                case "xml": HighlightXml(rtbContent, content); break;
                case "yaml": HighlightYaml(rtbContent, content); break;
                case "toml": HighlightToml(rtbContent, content); break;
                case "md": HighlightMarkdown(rtbContent, content); break;
                case "shell": HighlightShell(rtbContent, content); break;
                case "ruby": HighlightRuby(rtbContent, content); break;
                case "go": HighlightGo(rtbContent, content); break;
                case "rust": HighlightRust(rtbContent, content); break;
                case "php": HighlightPhp(rtbContent, content); break;
                case "lua": HighlightLua(rtbContent, content); break;
                case "r": HighlightR(rtbContent, content); break;
                default:
                    rtbContent.Text = content;
                    rtbContent.SelectAll();
                    rtbContent.SelectionColor = rtbContent.ForeColor;
                    rtbContent.SelectionBackColor = rtbContent.BackColor;
                    rtbContent.SelectionFont = new Font("Consolas", 10f);
                    rtbContent.SelectionStart = 0;
                    break;
            }

            rtbContent.ReadOnly = (_currentExt == ".xlsx" || _currentExt == ".db");
            _suppressTextChanged = false;
            lblStatus.Text = fi.Name + "  |  " + lines + " lines  |  " + FormatSize(fi.Length) + "  |  " + enc.EncodingName;
        }

        void LoadCsv(string path)
        {
            _suppressTextChanged = true;
            Encoding enc = DetectEncoding(path);
            string[] lines = File.ReadAllLines(path, enc);
            FileInfo fi = new FileInfo(path);
            string content = File.ReadAllText(path, enc);
            _savedContent = content;
            HighlightCsv(rtbContent, content);
            rtbContent.ReadOnly = false;

            DataTable dt = new DataTable();
            if (lines.Length == 0) { lblStatus.Text = "Empty CSV."; _suppressTextChanged = false; return; }

            string[] headers = SplitCsvLine(lines[0]);
            foreach (string h in headers) dt.Columns.Add(h.Trim('"', ' '));

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                string[] cells = SplitCsvLine(lines[i]);
                DataRow row = dt.NewRow();
                for (int j = 0; j < dt.Columns.Count && j < cells.Length; j++)
                    row[j] = cells[j].Trim('"');
                dt.Rows.Add(row);
            }

            dgvData.DataSource = dt;
            tabGrid.Text = "Table View (" + dt.Rows.Count + " rows)";
            tabControl.SelectedTab = tabGrid;
            _suppressTextChanged = false;
            lblStatus.Text = fi.Name + "  |  " + (lines.Length - 1) + " rows  |  " + dt.Columns.Count + " columns  |  " + FormatSize(fi.Length);
        }

        void LoadSqliteDb(string path)
        {
            Type connType = null;
            try
            {
                Assembly sqliteAsm = Assembly.Load("System.Data.SQLite");
                connType = sqliteAsm.GetType("System.Data.SQLite.SQLiteConnection");
            }
            catch { }

            if (connType == null)
            {
                _suppressTextChanged = true;
                string raw = "SQLite .db file detected.\n\nTo view table data, add System.Data.SQLite.dll next to the .exe.\n\nShowing raw hex preview:\n\n";
                byte[] bytes = File.ReadAllBytes(path);
                StringBuilder sb = new StringBuilder(raw);
                for (int i = 0; i < Math.Min(bytes.Length, 512); i++)
                {
                    if (i % 16 == 0 && i > 0) sb.AppendLine();
                    sb.Append(bytes[i].ToString("X2") + " ");
                }
                _savedContent = sb.ToString();
                rtbContent.Text = sb.ToString();
                rtbContent.SelectAll();
                rtbContent.SelectionColor = Color.Black;
                rtbContent.SelectionFont = new Font("Consolas", 10f);
                rtbContent.SelectionStart = 0;
                rtbContent.ReadOnly = true;
                tabControl.SelectedTab = tabText;
                _suppressTextChanged = false;
                lblStatus.Text = Path.GetFileName(path) + "  |  " + FormatSize(new FileInfo(path).Length) + "  |  SQLite (System.Data.SQLite.dll not found)";
                return;
            }

            string connStr = "Data Source=" + path + ";Version=3;Read Only=True;";
            using (IDbConnection conn = (IDbConnection)Activator.CreateInstance(connType, connStr))
            {
                conn.Open();
                IDbCommand cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                List<string> tables = new List<string>();
                using (IDataReader r = cmd.ExecuteReader())
                    while (r.Read()) tables.Add(r.GetString(0));

                StringBuilder sb = new StringBuilder();
                foreach (string t in tables)
                {
                    sb.AppendLine("-- Table: " + t);
                    IDbCommand sc = conn.CreateCommand();
                    sc.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='" + t + "';";
                    object schema = sc.ExecuteScalar();
                    if (schema != null) sb.AppendLine(schema.ToString());
                    sb.AppendLine();
                }
                _suppressTextChanged = true;
                _savedContent = sb.ToString();
                HighlightSql(rtbContent, sb.ToString());
                rtbContent.ReadOnly = true;

                if (tables.Count > 0)
                {
                    IDbCommand dc = conn.CreateCommand();
                    dc.CommandText = "SELECT * FROM [" + tables[0] + "] LIMIT 500;";
                    IDataReader dr = dc.ExecuteReader();
                    DataTable dt = new DataTable();
                    dt.Load(dr);
                    dgvData.DataSource = dt;
                    tabGrid.Text = "Table View - " + tables[0];
                    tabControl.SelectedTab = tabGrid;
                }
                _suppressTextChanged = false;
                lblStatus.Text = Path.GetFileName(path) + "  |  " + tables.Count + " table(s)  |  " + FormatSize(new FileInfo(path).Length);
            }
        }

        void LoadXlsx(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            if (data[0] == 0x50 && data[1] == 0x4B)
            {
                DataTable dt = ParseXlsx(path);
                if (dt != null && dt.Columns.Count > 0)
                {
                    dgvData.DataSource = dt;
                    tabGrid.Text = "Table View (" + dt.Rows.Count + " rows)";
                    tabControl.SelectedTab = tabGrid;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("-- Excel file: " + Path.GetFileName(path));
                    sb.AppendLine("-- " + dt.Rows.Count + " rows, " + dt.Columns.Count + " columns");
                    sb.AppendLine();
                    List<string> colNames = new List<string>();
                    foreach (DataColumn c in dt.Columns) colNames.Add(c.ColumnName);
                    sb.AppendLine(string.Join(" | ", colNames));
                    sb.AppendLine(new string('-', 60));
                    int preview = Math.Min(dt.Rows.Count, 50);
                    for (int i = 0; i < preview; i++)
                    {
                        List<string> cells = new List<string>();
                        foreach (DataColumn c in dt.Columns) cells.Add(dt.Rows[i][c].ToString());
                        sb.AppendLine(string.Join(" | ", cells));
                    }
                    if (dt.Rows.Count > 50)
                        sb.AppendLine("... (" + (dt.Rows.Count - 50) + " more rows)");

                    _suppressTextChanged = true;
                    _savedContent = sb.ToString();
                    rtbContent.Text = sb.ToString();
                    rtbContent.SelectAll();
                    rtbContent.SelectionColor = Color.Black;
                    rtbContent.SelectionFont = new Font("Consolas", 10f);
                    rtbContent.SelectionStart = 0;
                    rtbContent.ReadOnly = true;
                    _suppressTextChanged = false;

                    FileInfo fi = new FileInfo(path);
                    lblStatus.Text = fi.Name + "  |  " + dt.Rows.Count + " rows  |  " + dt.Columns.Count + " columns  |  " + FormatSize(fi.Length);
                    return;
                }
            }
            lblStatus.Text = "Could not parse xlsx.";
        }

        

        DataTable ParseXlsx(string path)
        {
            DataTable dt = new DataTable();
            try
            {
                byte[] zipData = File.ReadAllBytes(path);
                string sharedStrings = ExtractZipEntry(zipData, "xl/sharedStrings.xml");
                string sheet1 = ExtractZipEntry(zipData, "xl/worksheets/sheet1.xml");
                if (sheet1 == null) return dt;

                List<string> sst = new List<string>();
                if (sharedStrings != null)
                {
                    foreach (Match si in Regex.Matches(sharedStrings, @"<si>.*?</si>", RegexOptions.Singleline))
                    {
                        StringBuilder val = new StringBuilder();
                        foreach (Match t in Regex.Matches(si.Value, @"<t[^>]*>(.*?)</t>", RegexOptions.Singleline))
                            val.Append(XmlDecode(t.Groups[1].Value));
                        sst.Add(val.ToString());
                    }
                }

                bool headerDone = false;
                foreach (Match row in Regex.Matches(sheet1, @"<row[^>]*>(.*?)</row>", RegexOptions.Singleline))
                {
                    List<string> rowData = new List<string>();
                    foreach (Match cell in Regex.Matches(row.Groups[1].Value, @"<c ([^>]*)>(.*?)</c>", RegexOptions.Singleline))
                    {
                        string attrs = cell.Groups[1].Value;
                        string cellContent = cell.Groups[2].Value;
                        bool isShared = Regex.IsMatch(attrs, @"\bt=""s""");
                        Match vMatch = Regex.Match(cellContent, @"<v>(.*?)</v>");
                        string value = vMatch.Success ? vMatch.Groups[1].Value : "";
                        if (isShared && int.TryParse(value, out int idx) && idx < sst.Count)
                            value = sst[idx];
                        string colRef = Regex.Match(attrs, @"r=""([A-Z]+)\d+""").Groups[1].Value;
                        int colIdx = ColRefToIndex(colRef) - 1;
                        while (rowData.Count <= colIdx) rowData.Add("");
                        rowData[colIdx] = value;
                    }
                    if (!headerDone)
                    {
                        foreach (string h in rowData)
                            dt.Columns.Add(string.IsNullOrEmpty(h) ? "Col" + dt.Columns.Count : h);
                        headerDone = true;
                    }
                    else
                    {
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < dt.Columns.Count && i < rowData.Count; i++) dr[i] = rowData[i];
                        dt.Rows.Add(dr);
                    }
                }
            }
            catch { }
            return dt;
        }

        static int ColRefToIndex(string col)
        {
            int result = 0;
            foreach (char c in col) result = result * 26 + (c - 'A' + 1);
            return result;
        }

        static string ExtractZipEntry(byte[] zip, string entryName)
        {
            try
            {
                int pos = 0;
                while (pos < zip.Length - 30)
                {
                    if (zip[pos] != 0x50 || zip[pos + 1] != 0x4B || zip[pos + 2] != 0x03 || zip[pos + 3] != 0x04) { pos++; continue; }
                    int compression = zip[pos + 8] | (zip[pos + 9] << 8);
                    int compSize = zip[pos + 18] | (zip[pos + 19] << 8) | (zip[pos + 20] << 16) | (zip[pos + 21] << 24);
                    int fnLen = zip[pos + 26] | (zip[pos + 27] << 8);
                    int extraLen = zip[pos + 28] | (zip[pos + 29] << 8);
                    string fn = Encoding.UTF8.GetString(zip, pos + 30, fnLen);
                    int dataStart = pos + 30 + fnLen + extraLen;
                    if (fn == entryName)
                    {
                        byte[] raw = new byte[compSize];
                        Array.Copy(zip, dataStart, raw, 0, compSize);
                        return compression == 0
                            ? Encoding.UTF8.GetString(raw)
                            : Encoding.UTF8.GetString(Inflate(raw));
                    }
                    pos = dataStart + compSize;
                }
            }
            catch { }
            return null;
        }

        static byte[] Inflate(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (MemoryStream output = new MemoryStream())
            {
                var deflate = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress);
                byte[] buf = new byte[4096];
                int read;
                while ((read = deflate.Read(buf, 0, buf.Length)) > 0) output.Write(buf, 0, read);
                return output.ToArray();
            }
        }

        static string XmlDecode(string s) =>
            s.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
             .Replace("&quot;", "\"").Replace("&apos;", "'");

        static string[] SplitCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuote = false;
            StringBuilder current = new StringBuilder();
            foreach (char c in line)
            {
                if (c == '"') { inQuote = !inQuote; current.Append(c); }
                else if (c == ',' && !inQuote) { result.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }
            result.Add(current.ToString());
            return result.ToArray();
        }

        

        void ClearGrid()
        {
            dgvData.DataSource = null;
            tabGrid.Text = "Table View";
        }

        void ClearAll()
        {
            _suppressTextChanged = true;
            rtbContent.Clear();
            _suppressTextChanged = false;
            ClearGrid();
            _currentPath = "";
            _savedContent = "";
            _isDirty = false;
            _saveMenuItem.Enabled = false;
            lblStatus.Text = "Ready.";
            Text = "File Viewer";
            lblMatches.Text = "";
        }

        void CopyAll()
        {
            if (tabControl.SelectedTab == tabText && rtbContent.Text.Length > 0)
            {
                Clipboard.SetText(rtbContent.Text);
            }
            else if (tabControl.SelectedTab == tabGrid && dgvData.DataSource != null)
            {
                DataTable dt = (DataTable)dgvData.DataSource;
                StringBuilder sb = new StringBuilder();
                List<string> cols = new List<string>();
                foreach (DataColumn c in dt.Columns) cols.Add(c.ColumnName);
                sb.AppendLine(string.Join("\t", cols));
                foreach (DataRow row in dt.Rows)
                {
                    List<string> cells = new List<string>();
                    foreach (DataColumn c in dt.Columns) cells.Add(row[c].ToString());
                    sb.AppendLine(string.Join("\t", cells));
                }
                Clipboard.SetText(sb.ToString());
            }
        }

        void FocusSearch()
        {
            searchPanel.Visible = true;
            txtSearch.Focus();
            txtSearch.SelectAll();
            _searchStart = 0;
            UpdateMatchCount();
        }

        void HideSearch()
        {
            searchPanel.Visible = false;
            rtbContent.Focus();
            rtbContent.SelectionLength = 0;
        }

        void SearchNext()
        {
            string term = txtSearch.Text;
            if (term.Length == 0 || rtbContent.Text.Length == 0) return;
            StringComparison sc = chkMatchCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int idx = rtbContent.Text.IndexOf(term, _searchStart, sc);
            if (idx < 0 && _searchStart > 0)
            {
                idx = rtbContent.Text.IndexOf(term, 0, sc);
                if (idx >= 0) lblMatches.ForeColor = Color.DarkOrange;
            }
            else lblMatches.ForeColor = Color.Gray;
            if (idx >= 0) { rtbContent.Select(idx, term.Length); rtbContent.ScrollToCaret(); _searchStart = idx + term.Length; }
            else { lblMatches.Text = "Not found"; lblMatches.ForeColor = Color.Red; }
        }

        void SearchPrev()
        {
            string term = txtSearch.Text;
            if (term.Length == 0 || rtbContent.Text.Length == 0) return;
            StringComparison sc = chkMatchCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int from = _searchStart - term.Length - 1;
            if (from < 0) from = rtbContent.Text.Length - 1;
            int idx = rtbContent.Text.LastIndexOf(term, from, sc);
            if (idx < 0)
            {
                idx = rtbContent.Text.LastIndexOf(term, rtbContent.Text.Length - 1, sc);
                if (idx >= 0) lblMatches.ForeColor = Color.DarkOrange;
            }
            else lblMatches.ForeColor = Color.Gray;
            if (idx >= 0) { rtbContent.Select(idx, term.Length); rtbContent.ScrollToCaret(); _searchStart = idx; }
            else { lblMatches.Text = "Not found"; lblMatches.ForeColor = Color.Red; }
        }

        void UpdateMatchCount()
        {
            string term = txtSearch.Text;
            if (term.Length == 0 || rtbContent.Text.Length == 0) { lblMatches.Text = ""; return; }
            StringComparison sc = chkMatchCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int count = 0, idx = 0;
            while (true)
            {
                idx = rtbContent.Text.IndexOf(term, idx, sc);
                if (idx < 0) break;
                count++; idx += term.Length;
            }
            lblMatches.Text = count == 0 ? "No matches" : count + " match" + (count > 1 ? "es" : "");
            lblMatches.ForeColor = count == 0 ? Color.Red : Color.Gray;
        }

        

        void SyncScrollBar()
        {
            if (vScrollBar == null || rtbContent == null) return;
            NativeMethods.SCROLLINFO si = new NativeMethods.SCROLLINFO();
            si.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(si);
            si.fMask = 0x17;
            NativeMethods.GetScrollInfo(rtbContent.Handle, 1, ref si);
            if (si.nMax <= 0) return;
            vScrollBar.Minimum = si.nMin;
            vScrollBar.Maximum = si.nMax;
            vScrollBar.SmallChange = Math.Max(1, (int)si.nPage / 10);
            vScrollBar.LargeChange = Math.Max(1, (int)si.nPage);
            vScrollBar.Value = Math.Min(si.nPos, Math.Max(0, si.nMax - (int)si.nPage + 1));
        }

        void DrawLineNumbers(Graphics g)
        {
            Color numBg = _darkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(240, 240, 240);
            Color numBorder = _darkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(200, 200, 200);
            Color numFg = _darkMode ? Color.FromArgb(130, 130, 140) : Color.FromArgb(130, 130, 150);
            g.Clear(numBg);
            g.DrawLine(new Pen(numBorder), lineNumberPanel.Width - 1, 0, lineNumberPanel.Width - 1, lineNumberPanel.Height);
            if (rtbContent == null || rtbContent.Lines.Length == 0) return;

            Font font = new Font("Consolas", 10f);
            int totalLines = rtbContent.Lines.Length;
            for (int i = 0; i < totalLines; i++)
            {
                int charIndex = rtbContent.GetFirstCharIndexFromLine(i);
                if (charIndex < 0) break;
                Point pos = rtbContent.GetPositionFromCharIndex(charIndex);
                if (pos.Y + font.Height < 0) continue;
                if (pos.Y > lineNumberPanel.Height) break;
                string num = (i + 1).ToString();
                SizeF sz = g.MeasureString(num, font);
                g.DrawString(num, font, new SolidBrush(numFg), lineNumberPanel.Width - sz.Width - 4, pos.Y);
            }
            font.Dispose();
        }

        

        static Encoding DetectEncoding(string path)
        {
            byte[] bom = new byte[4];
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                fs.Read(bom, 0, 4);
            if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) return Encoding.UTF8;
            if (bom[0] == 0xFF && bom[1] == 0xFE) return Encoding.Unicode;
            if (bom[0] == 0xFE && bom[1] == 0xFF) return Encoding.BigEndianUnicode;
            return Encoding.Default;
        }

        static string GetModeForExt(string ext)
        {
            switch (ext)
            {
                case ".sql": return "sql";
                case ".json": return "json";
                case ".csv": return "csv";
                case ".db": return "db";
                case ".xlsx": return "xlsx";
                case ".py": return "python";
                case ".cs":
                case ".java":
                case ".kt":
                case ".swift":
                case ".dart":
                case ".scala":
                case ".vb":
                case ".fs": return "csharp";
                case ".cpp": case ".c": case ".h": case ".hpp": return "cpp";
                case ".js": case ".ts": case ".jsx": case ".tsx": return "js";
                case ".html": case ".htm": return "html";
                case ".css": return "css";
                case ".xml": return "xml";
                case ".yaml": case ".yml": return "yaml";
                case ".toml": case ".ini": case ".cfg": return "toml";
                case ".md": return "md";
                case ".sh": case ".bat": case ".ps1": return "shell";
                case ".rb": return "ruby";
                case ".go": return "go";
                case ".rs": return "rust";
                case ".php": return "php";
                case ".lua": return "lua";
                case ".r": case ".m": return "r";
                default: return "plain";
            }
        }

        static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1048576) return (bytes / 1024.0).ToString("F1") + " KB";
            return (bytes / 1048576.0).ToString("F1") + " MB";
        }

        

        static void InitRtb(RichTextBox rtb, string text)
        {
            rtb.SuspendLayout();
            rtb.Text = text;
            rtb.SelectAll();
            rtb.SelectionColor = rtb.ForeColor;
            rtb.SelectionBackColor = rtb.BackColor;
            rtb.SelectionFont = new Font("Consolas", 10f);
        }

        static void ApplyKeywords(RichTextBox rtb, string text, string[] keywords, Color color, FontStyle style = FontStyle.Bold)
        {
            foreach (string kw in keywords)
            {
                int idx = 0;
                while (true)
                {
                    idx = text.IndexOf(kw, idx, StringComparison.Ordinal);
                    if (idx < 0) break;
                    bool prev = idx == 0 || (!char.IsLetterOrDigit(text[idx - 1]) && text[idx - 1] != '_');
                    bool next = idx + kw.Length >= text.Length || (!char.IsLetterOrDigit(text[idx + kw.Length]) && text[idx + kw.Length] != '_');
                    if (prev && next) { rtb.Select(idx, kw.Length); rtb.SelectionColor = color; rtb.SelectionFont = new Font("Consolas", 10f, style); }
                    idx += kw.Length;
                }
            }
        }

        static void ApplyPatterns(RichTextBox rtb, string text, string[] patterns, Color color, FontStyle style = FontStyle.Regular)
        {
            foreach (string p in patterns)
                foreach (Match m in Regex.Matches(text, p, RegexOptions.Multiline))
                { rtb.Select(m.Index, m.Length); rtb.SelectionColor = color; rtb.SelectionFont = new Font("Consolas", 10f, style); }
        }

        static void FinishRtb(RichTextBox rtb)
        {
            rtb.SelectionStart = 0; rtb.SelectionLength = 0; rtb.ResumeLayout();
        }

        

        static void HighlightSql(RichTextBox rtb, string sql)
        {
            string[] keywords = {
                "SELECT","FROM","WHERE","INSERT","INTO","VALUES","UPDATE","SET","DELETE",
                "CREATE","TABLE","DROP","ALTER","ADD","INDEX","PRIMARY","KEY","FOREIGN",
                "REFERENCES","JOIN","LEFT","RIGHT","INNER","OUTER","ON","AS","AND","OR",
                "NOT","NULL","IS","IN","LIKE","ORDER","BY","GROUP","HAVING","DISTINCT",
                "UNION","ALL","LIMIT","OFFSET","BEGIN","COMMIT","ROLLBACK","TRANSACTION",
                "DATABASE","USE","CONSTRAINT","DEFAULT","AUTO_INCREMENT","UNIQUE","CHECK",
                "TRUNCATE","EXISTS","CASE","WHEN","THEN","ELSE","END","IF","REPLACE",
                "BETWEEN","ASC","DESC","PROCEDURE","FUNCTION","TRIGGER","VIEW","DECLARE",
                "INT","VARCHAR","TEXT","DATETIME","DATE","BOOLEAN","FLOAT","DOUBLE","BIGINT"
            };
            rtb.SuspendLayout();
            rtb.Text = sql;
            rtb.SelectAll();
            rtb.SelectionColor = rtb.ForeColor;
            rtb.SelectionBackColor = rtb.BackColor;
            rtb.SelectionFont = new Font("Consolas", 10f);
            foreach (string kw in keywords)
            {
                int idx = 0;
                while (true)
                {
                    idx = sql.IndexOf(kw, idx, StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) break;
                    bool prev = idx == 0 || (!char.IsLetterOrDigit(sql[idx - 1]) && sql[idx - 1] != '_');
                    bool next = idx + kw.Length >= sql.Length || (!char.IsLetterOrDigit(sql[idx + kw.Length]) && sql[idx + kw.Length] != '_');
                    if (prev && next) { rtb.Select(idx, kw.Length); rtb.SelectionColor = Color.FromArgb(0, 0, 180); rtb.SelectionFont = new Font("Consolas", 10f, FontStyle.Bold); }
                    idx += kw.Length;
                }
            }
            foreach (Match m in Regex.Matches(sql, @"'[^']*'")) { rtb.Select(m.Index, m.Length); rtb.SelectionColor = Color.FromArgb(163, 21, 21); rtb.SelectionFont = new Font("Consolas", 10f); }
            foreach (Match m in Regex.Matches(sql, @"--[^\r\n]*")) { rtb.Select(m.Index, m.Length); rtb.SelectionColor = Color.FromArgb(0, 128, 0); rtb.SelectionFont = new Font("Consolas", 10f, FontStyle.Italic); }
            foreach (Match m in Regex.Matches(sql, @"/\*[\s\S]*?\*/")) { rtb.Select(m.Index, m.Length); rtb.SelectionColor = Color.FromArgb(0, 128, 0); rtb.SelectionFont = new Font("Consolas", 10f, FontStyle.Italic); }
            foreach (Match m in Regex.Matches(sql, @"\b\d+(\.\d+)?\b")) { rtb.Select(m.Index, m.Length); rtb.SelectionColor = Color.FromArgb(9, 134, 88); rtb.SelectionFont = new Font("Consolas", 10f); }
            rtb.SelectionStart = 0; rtb.SelectionLength = 0; rtb.ResumeLayout();
        }

        static void HighlightJson(RichTextBox rtb, string json)
        {
            rtb.SuspendLayout();
            rtb.Text = json;
            rtb.SelectAll();
            rtb.SelectionColor = rtb.ForeColor;
            rtb.SelectionBackColor = rtb.BackColor;
            rtb.SelectionFont = new Font("Consolas", 10f);
            foreach (Match m in Regex.Matches(json, "\"(.*?)\"\\s*:"))
            { rtb.Select(m.Index, m.Groups[1].Index - m.Index + m.Groups[1].Length + 2); rtb.SelectionColor = Color.FromArgb(0, 100, 160); rtb.SelectionFont = new Font("Consolas", 10f, FontStyle.Bold); }
            foreach (Match m in Regex.Matches(json, @":\s*""([^""]*)"""))
            { rtb.Select(m.Index + m.Value.IndexOf('"'), m.Value.Length - m.Value.IndexOf('"')); rtb.SelectionColor = Color.FromArgb(163, 21, 21); rtb.SelectionFont = new Font("Consolas", 10f); }
            foreach (Match m in Regex.Matches(json, @":\s*(-?\d+(\.\d+)?)"))
            { rtb.Select(m.Index + m.Value.IndexOf(m.Groups[1].Value), m.Groups[1].Length); rtb.SelectionColor = Color.FromArgb(9, 134, 88); rtb.SelectionFont = new Font("Consolas", 10f); }
            foreach (Match m in Regex.Matches(json, @"\b(true|false|null)\b"))
            { rtb.Select(m.Index, m.Length); rtb.SelectionColor = Color.FromArgb(0, 0, 180); rtb.SelectionFont = new Font("Consolas", 10f, FontStyle.Bold); }
            rtb.SelectionStart = 0; rtb.SelectionLength = 0; rtb.ResumeLayout();
        }

        static void HighlightCsv(RichTextBox rtb, string csv)
        {
            rtb.SuspendLayout();
            rtb.Text = csv;
            rtb.SelectAll();
            rtb.SelectionColor = rtb.ForeColor;
            rtb.SelectionBackColor = rtb.BackColor;
            rtb.SelectionFont = new Font("Consolas", 10f);
            string[] lines = csv.Split('\n');
            if (lines.Length > 0) { rtb.Select(0, lines[0].Length); rtb.SelectionColor = Color.FromArgb(0, 0, 180); rtb.SelectionFont = new Font("Consolas", 10f, FontStyle.Bold); }
            int offset = lines.Length > 0 ? lines[0].Length + 1 : 0;
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i]; int col = 0, pos = 0;
                while (pos <= line.Length)
                {
                    int comma = line.IndexOf(',', pos);
                    int end = comma < 0 ? line.Length : comma;
                    rtb.Select(offset + pos, end - pos);
                    rtb.SelectionColor = col % 2 == 0 ? Color.FromArgb(80, 80, 80) : Color.FromArgb(50, 100, 150);
                    if (comma < 0) break;
                    pos = comma + 1; col++;
                }
                offset += line.Length + 1;
            }
            rtb.SelectionStart = 0; rtb.SelectionLength = 0; rtb.ResumeLayout();
        }

        static void HighlightPython(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"False","None","True","and","as","assert","async","await",
                "break","class","continue","def","del","elif","else","except","finally","for",
                "from","global","if","import","in","is","lambda","nonlocal","not","or","pass",
                "raise","return","try","while","with","yield"}, Color.FromArgb(86, 156, 214));
            ApplyKeywords(rtb, code, new[]{"int","str","float","bool","list","dict","tuple","set",
                "bytes","type","len","range","print","input","open","super","self","cls",
                "property","staticmethod","classmethod","abs","all","any","enumerate","zip",
                "map","filter","sorted","reversed","isinstance","issubclass","hasattr","getattr",
                "setattr","delattr"}, Color.FromArgb(78, 201, 176));
            ApplyPatterns(rtb, code, new[] { @"#[^\n]*" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"(""""""[\s\S]*?""""""|'''[\s\S]*?''')", @"""[^""]*""", @"'[^']*'" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"@\w+" }, Color.FromArgb(220, 220, 170));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightCSharp(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"abstract","as","base","bool","break","byte","case","catch",
                "char","checked","class","const","continue","decimal","default","delegate","do",
                "double","else","enum","event","explicit","extern","false","finally","fixed","float",
                "for","foreach","goto","if","implicit","in","int","interface","internal","is","lock",
                "long","namespace","new","null","object","operator","out","override","params","private",
                "protected","public","readonly","ref","return","sbyte","sealed","short","sizeof",
                "stackalloc","static","string","struct","switch","this","throw","true","try","typeof",
                "uint","ulong","unchecked","unsafe","ushort","using","virtual","void","volatile","while",
                "var","async","await","partial","yield","record","init","with","global","file",
                "fun","val","when","companion","data","sealed","inner","open","suspend","inline",
                "crossinline","noinline","reified","lateinit","by","get","set","constructor",
                "import","package","extends","implements","instanceof","final","synchronized",
                "transient","native","strictfp","throws","super","interface","enum","assert"
                }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"
            ApplyPatterns(rtb, code, new[] { @"/\*[\s\S]*?\*/" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"""[^""\\]*(?:\\.[^""\\]*)*""", @"@""[^""]*""" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"'[^'\\]'", @"'\\.'", @"'\\u[0-9a-fA-F]{4}'" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"\[.*?\]" }, Color.FromArgb(220, 220, 170));
            ApplyPatterns(rtb, code, new[] { @"[A-Z][a-zA-Z0-9_]+" }, Color.FromArgb(78, 201, 176));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?[fFdDlLuU]?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightCpp(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"auto","break","case","char","const","continue","default",
                "do","double","else","enum","extern","float","for","goto","if","inline","int","long",
                "register","restrict","return","short","signed","sizeof","static","struct","switch",
                "typedef","union","unsigned","void","volatile","while","bool","true","false","nullptr",
                "class","private","protected","public","new","delete","this","virtual","override",
                "final","explicit","template","typename","namespace","using","operator","friend",
                "constexpr","noexcept","decltype","static_assert","thread_local"
                }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"#\s*(include|define|undef|ifdef|ifndef|if|elif|else|endif|pragma|error|warning)[^\n]*" }, Color.FromArgb(155, 155, 100));
            ApplyPatterns(rtb, code, new[] { @"
            ApplyPatterns(rtb, code, new[] { @"/\*[\s\S]*?\*/" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"""[^""\\]*(?:\\.[^""\\]*)*""" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"'[^'\\]'", @"'\\.'", @"'\\u[0-9a-fA-F]{4}'" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"[A-Z_][A-Z0-9_]{2,}" }, Color.FromArgb(220, 220, 170));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?([uUlLfF]*)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightJs(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"break","case","catch","class","const","continue","debugger",
                "default","delete","do","else","export","extends","false","finally","for","function",
                "if","import","in","instanceof","let","new","null","return","super","switch","this",
                "throw","true","try","typeof","undefined","var","void","while","with","yield","async",
                "await","of","from","static","get","set","implements","interface","package","private",
                "protected","public","enum","type","namespace","declare","abstract","as","readonly",
                "keyof","infer","never","unknown","any","string","number","boolean","object","symbol",
                "bigint"}, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"
            ApplyPatterns(rtb, code, new[] { @"/\*[\s\S]*?\*/" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"""[^""\\]*(?:\\.[^""\\]*)*""", @"'[^'\\]*(?:\\.[^'\\]*)*'", @"`[^`]*`" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"[A-Z][a-zA-Z0-9_]+" }, Color.FromArgb(78, 201, 176));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?(n)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightHtml(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyPatterns(rtb, code, new[] { @"<!--[\s\S]*?-->" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"<!DOCTYPE[^>]*>" }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"</?[a-zA-Z][a-zA-Z0-9]*" }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"[a-zA-Z-]+=(?="")" }, Color.FromArgb(156, 220, 254));
            ApplyPatterns(rtb, code, new[] { @"""[^""]*""" }, Color.FromArgb(206, 145, 120));
            FinishRtb(rtb);
        }

        static void HighlightCss(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyPatterns(rtb, code, new[] { @"/\*[\s\S]*?\*/" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"[.#]?[a-zA-Z][a-zA-Z0-9_-]*\s*\{" }, Color.FromArgb(215, 186, 125));
            ApplyPatterns(rtb, code, new[] { @"[a-z-]+(?=\s*:)" }, Color.FromArgb(156, 220, 254));
            ApplyPatterns(rtb, code, new[] { @":\s*[^;{]+(?=;)" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"#[0-9a-fA-F]{3,8}", @"\d+(\.\d+)?(px|em|rem|%|vh|vw|pt|cm)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightXml(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyPatterns(rtb, code, new[] { @"<!--[\s\S]*?-->" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"<\?xml[^?]*\?>" }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"</?\w[\w:.-]*" }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"[\w:]+(?=\s*=)" }, Color.FromArgb(156, 220, 254));
            ApplyPatterns(rtb, code, new[] { @"""[^""]*""" }, Color.FromArgb(206, 145, 120));
            FinishRtb(rtb);
        }

        static void HighlightYaml(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyPatterns(rtb, code, new[] { @"#[^\n]*" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"^\s*[\w\-]+(?=\s*:)", @"^\s*-\s+[\w\-]+" }, Color.FromArgb(156, 220, 254));
            ApplyPatterns(rtb, code, new[] { @":\s+.*$" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"\b(true|false|null|yes|no|on|off)\b" }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightToml(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyPatterns(rtb, code, new[] { @"#[^\n]*", @";[^\n]*" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"^\s*\[[^\]]+\]" }, Color.FromArgb(215, 186, 125));
            ApplyPatterns(rtb, code, new[] { @"^\s*[\w\-\.]+(?=\s*=)" }, Color.FromArgb(156, 220, 254));
            ApplyPatterns(rtb, code, new[] { @"""[^""]*""", @"'[^']*'" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"\b(true|false)\b" }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightMarkdown(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyPatterns(rtb, code, new[] { @"^#{1,6}\s.+$" }, Color.FromArgb(86, 156, 214), FontStyle.Bold);
            ApplyPatterns(rtb, code, new[] { @"\*\*[^*]+\*\*" }, Color.FromArgb(220, 220, 170), FontStyle.Bold);
            ApplyPatterns(rtb, code, new[] { @"\*[^*]+\*", @"_[^_]+_" }, Color.FromArgb(206, 145, 120), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"`[^`]+`" }, Color.FromArgb(78, 201, 176));
            ApplyPatterns(rtb, code, new[] { @"```[\s\S]*?```" }, Color.FromArgb(78, 201, 176));
            ApplyPatterns(rtb, code, new[] { @"^\s*[-*+]\s", @"^\s*\d+\.\s" }, Color.FromArgb(215, 186, 125));
            ApplyPatterns(rtb, code, new[] { @"\[([^\]]+)\]\([^\)]*\)" }, Color.FromArgb(156, 220, 254));
            ApplyPatterns(rtb, code, new[] { @"^>.*$" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            FinishRtb(rtb);
        }

        static void HighlightShell(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"if","then","else","elif","fi","for","while","do","done",
                "case","esac","function","return","exit","echo","read","export","local","source",
                "alias","unset","shift","break","continue","true","false","in",
                "param","begin","process","end","foreach","switch","default","where",
                "rem","set","call","goto","pause","start","cls"}, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"#[^\n]*", @"::.*$", @"REM[^\n]*" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"""[^""]*""", @"'[^']*'" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"\$\{?[\w]+\}?", @"%[\w]+%" }, Color.FromArgb(156, 220, 254));
            ApplyPatterns(rtb, code, new[] { @"\d+" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightRuby(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"__ENCODING__","__LINE__","__FILE__","BEGIN","END","alias",
                "and","begin","break","case","class","def","defined?","do","else","elsif","end",
                "ensure","false","for","if","in","module","next","nil","not","or","redo","rescue",
                "retry","return","self","super","then","true","undef","unless","until","when","while",
                "yield","attr_accessor","attr_reader","attr_writer","require","include","extend",
                "raise","puts","print","p","lambda","proc","new","initialize","protected","private","public"
                }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"#[^\n]*" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"""[^""\\]*(?:\\.[^""\\]*)*""", @"'[^'\\]*(?:\\.[^'\\]*)*'" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @":[a-zA-Z_]\w*" }, Color.FromArgb(215, 186, 125));
            ApplyPatterns(rtb, code, new[] { @"@{1,2}[a-zA-Z_]\w*" }, Color.FromArgb(156, 220, 254));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightGo(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"break","case","chan","const","continue","default","defer",
                "else","fallthrough","for","func","go","goto","if","import","interface","map","package",
                "range","return","select","struct","switch","type","var",
                "bool","byte","complex64","complex128","error","float32","float64",
                "int","int8","int16","int32","int64","rune","string","uint","uint8",
                "uint16","uint32","uint64","uintptr","true","false","nil","iota",
                "make","new","len","cap","close","copy","delete","append","panic","recover","print","println"
                }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"
            ApplyPatterns(rtb, code, new[] { @"/\*[\s\S]*?\*/" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"""[^""\\]*(?:\\.[^""\\]*)*""", @"`[^`]*`" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"[A-Z][a-zA-Z0-9_]+" }, Color.FromArgb(78, 201, 176));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightRust(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"as","async","await","break","const","continue","crate",
                "dyn","else","enum","extern","false","fn","for","if","impl","in","let","loop","match",
                "mod","move","mut","pub","ref","return","self","Self","static","struct","super",
                "trait","true","type","union","unsafe","use","where","while","abstract","become",
                "box","do","final","macro","override","priv","typeof","unsized","virtual","yield",
                "bool","i8","i16","i32","i64","i128","isize","u8","u16","u32","u64","u128","usize",
                "f32","f64","char","str","String","Vec","Option","Result","Some","None","Ok","Err",
                "println!","print!","panic!","vec!","format!","assert!","todo!","unimplemented!"
                }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"
            ApplyPatterns(rtb, code, new[] { @"/\*[\s\S]*?\*/" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"""[^""\\]*(?:\\.[^""\\]*)*""\s*", @"r#*""[\s\S]*?""#*" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"#\[.*?\]", @"#!\[.*?\]" }, Color.FromArgb(220, 220, 170));
            ApplyPatterns(rtb, code, new[] { @"'\w'", @"'\.'" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?(_\w+)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightPhp(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"abstract","and","array","as","break","callable","case",
                "catch","class","clone","const","continue","declare","default","die","do","echo",
                "else","elseif","empty","enddeclare","endfor","endforeach","endif","endswitch",
                "endwhile","eval","exit","extends","final","finally","fn","for","foreach","function",
                "global","goto","if","implements","include","include_once","instanceof","insteadof",
                "interface","isset","list","match","namespace","new","null","or","print","private",
                "protected","public","readonly","require","require_once","return","static","switch",
                "throw","trait","try","unset","use","var","while","yield","true","false","null"
                }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"
            ApplyPatterns(rtb, code, new[] { @"/\*[\s\S]*?\*/" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"""[^""\\]*(?:\\.[^""\\]*)*""", @"'[^'\\]*(?:\\.[^'\\]*)*'" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"\$[a-zA-Z_]\w*" }, Color.FromArgb(156, 220, 254));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightLua(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"and","break","do","else","elseif","end","false","for",
                "function","goto","if","in","local","nil","not","or","repeat","return","then",
                "true","until","while","print","type","pairs","ipairs","next","select","tonumber",
                "tostring","rawget","rawset","require","pcall","xpcall","error","assert","table",
                "string","math","io","os","coroutine","_G","_VERSION"
                }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"--\[\[[\s\S]*?\]\]", @"--[^\n]*" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"""[^""\\]*(?:\\.[^""\\]*)*""", @"'[^'\\]*(?:\\.[^'\\]*)*'", @"\[\[[\s\S]*?\]\]" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }

        static void HighlightR(RichTextBox rtb, string code)
        {
            InitRtb(rtb, code);
            ApplyKeywords(rtb, code, new[]{"if","else","repeat","while","function","for","in","next",
                "break","TRUE","FALSE","NULL","NA","NA_integer_","NA_real_","NA_complex_","NA_character_",
                "Inf","NaN","return","library","require","source","print","cat",
                "c","list","vector","data.frame","matrix","array","factor","paste","paste0",
                "sprintf","which","length","nrow","ncol","dim","str","summary","class","typeof"
                }, Color.FromArgb(86, 156, 214));
            ApplyPatterns(rtb, code, new[] { @"#[^\n]*" }, Color.FromArgb(106, 153, 85), FontStyle.Italic);
            ApplyPatterns(rtb, code, new[] { @"""[^""]*""", @"'[^']*'" }, Color.FromArgb(206, 145, 120));
            ApplyPatterns(rtb, code, new[] { @"<-|->|<<-|->>|:=" }, Color.FromArgb(215, 186, 125));
            ApplyPatterns(rtb, code, new[] { @"\d+(\.\d+)?(L|i)?" }, Color.FromArgb(181, 206, 168));
            FinishRtb(rtb);
        }
    }

    

    class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(62, 62, 66)), e.Item.ContentRectangle);
            else if (e.Item.Owner is ToolStripDropDown)
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(37, 37, 38)), e.Item.ContentRectangle);
            else
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 48)), e.Item.ContentRectangle);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.FromArgb(220, 220, 220);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            e.Graphics.DrawLine(new Pen(Color.FromArgb(70, 70, 74)), 4, y, e.Item.Width - 4, y);
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(45, 45, 48)), e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
    }

    class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(62, 62, 66);
        public override Color MenuItemBorder => Color.FromArgb(62, 62, 66);
        public override Color MenuBorder => Color.FromArgb(70, 70, 74);
        public override Color ToolStripDropDownBackground => Color.FromArgb(37, 37, 38);
        public override Color ImageMarginGradientBegin => Color.FromArgb(37, 37, 38);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(37, 37, 38);
        public override Color ImageMarginGradientEnd => Color.FromArgb(37, 37, 38);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(62, 62, 66);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(62, 62, 66);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(62, 62, 66);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(62, 62, 66);
        public override Color MenuStripGradientBegin => Color.FromArgb(45, 45, 48);
        public override Color MenuStripGradientEnd => Color.FromArgb(45, 45, 48);
        public override Color SeparatorDark => Color.FromArgb(70, 70, 74);
        public override Color SeparatorLight => Color.FromArgb(70, 70, 74);
    }

    static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessage")]
        public static extern IntPtr SendMessageRect(IntPtr hWnd, int Msg, IntPtr wParam, ref RECT lParam);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct RECT { public int left, top, right, bottom; }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct SCROLLINFO
        {
            public int cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }
    }
}