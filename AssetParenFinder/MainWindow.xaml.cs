using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AssetParenFinder {

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {

        #region Constructors

        public MainWindow() {
            InitializeComponent();
            //PathViewer = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Replace(@"file:\", String.Empty)).Parent.Parent.Parent.FullName + @"\AssetViewer";
            BindingOperations.EnableCollectionSynchronization(Results, new object());
            var filepath = "assets.xml";
            if (!File.Exists(filepath)) {
                var r = MessageBox.Show("asset.xml nicht gefunden. Bitte manuell suchen.", "Fehler", MessageBoxButton.OK);
                var ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == true) {
                    Original = XDocument.Load(ofd.FileName);
                }
            }
            else {
                Original = XDocument.Load(filepath);
            }
        }

        #endregion Constructors

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Properties

        public bool IsbtnBackEnabled {
            get {
                return _isbtnBackEnabled;
            }
            set {
                if (_isbtnBackEnabled != value) {
                    _isbtnBackEnabled = value;
                    RaisePropertyChanged(nameof(IsbtnBackEnabled));
                }
            }
        }

        public bool IsbtnForwardEnabled {
            get {
                return _isbtnForwardEnabled;
            }
            set {
                if (_isbtnForwardEnabled != value) {
                    _isbtnForwardEnabled = value;
                    RaisePropertyChanged(nameof(IsbtnForwardEnabled));
                }
            }
        }

        public ObservableCollection<SearchResult> Results { get; set; } = new ObservableCollection<SearchResult>();
        public XDocument Original { get; }
        public bool IsRunning { get; private set; }
        public string LastSearch { get; set; }
        public Stack<string> BackStack { get; set; } = new Stack<string>();
        public Stack<string> ForwardStack { get; set; } = new Stack<string>();

        #endregion Properties

        #region Methods

        public void RaisePropertyChanged([CallerMemberName]string name = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public async Task ActiveStartFinding() {
            var id = tbID.Text;
            if (!string.IsNullOrWhiteSpace(id) && !IsRunning) {
                if (LastSearch != null) {
                    BackStack.Push(LastSearch);
                    ForwardStack.Clear();
                    IsbtnBackEnabled = true;
                    IsbtnForwardEnabled = false;
                }
                await StartFinding();
            }
        }

        #endregion Methods

        #region Fields

        private bool _isbtnBackEnabled;
        private bool _isbtnForwardEnabled;

        #endregion Fields

        private async void BtnFind_Click(object sender, RoutedEventArgs e) {
            await ActiveStartFinding();
        }

        private async Task StartFinding() {
            var id = tbID.Text;
            if (!string.IsNullOrWhiteSpace(id) && !IsRunning) {
                IsRunning = true;
                LastSearch = id;
                Results.Clear();
                await Task.Run(() => {
                    var links = Original.Root.XPathSelectElements($"//*[text()={id}]").ToArray();
                    if (links.Length > 0) {
                        for (var i = 0; i < links.Length; i++) {
                            var element = links[i];
                            var result = new SearchResult();
                            result.FoundedNodes.Add(element);
                            while (element.Name.LocalName != "Asset" || !element.HasElements) {
                                element = element.Parent;
                            }
                            if (element != null) {
                                result.Asset = element;
                                var old = Results.FirstOrDefault(r => r.Asset == result.Asset);
                                if (old != null) {
                                    old.Add(result);
                                }
                                else {
                                    Results.Add(result);
                                }
                            }
                        }
                    }
                });
                IsRunning = false;
            }
        }

        private void LbResults_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (lbResults.SelectedItem is SearchResult result) {
                Browser.NavigateToString("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" + result.Asset.ToString());
            }
        }

        private async void TbID_PreviewKeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                await ActiveStartFinding();
            }
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e) {
            if (sender is MenuItem menu && menu.Tag is SearchResult result) {
                tbID.Text = result.Guid;
                await ActiveStartFinding();
            }
        }

        private async void BtnBack_Click(object sender, RoutedEventArgs e) {
            var item = BackStack.Pop();
            if (BackStack.Count == 0) {
                IsbtnBackEnabled = false;
            }
            ForwardStack.Push(LastSearch);
            IsbtnForwardEnabled = true;
            tbID.Text = item;
            await StartFinding();
        }

        private async void BtnForward_Click(object sender, RoutedEventArgs e) {
            var item = ForwardStack.Pop();
            if (ForwardStack.Count == 0) {
                IsbtnForwardEnabled = false;
            }
            BackStack.Push(LastSearch);
            IsbtnBackEnabled = true;
            tbID.Text = item;
            await StartFinding();
        }
    }
}