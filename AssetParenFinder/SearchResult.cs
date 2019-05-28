using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AssetParenFinder {

    public class SearchResult {

        #region Constructors

        public SearchResult() {
            BindingOperations.EnableCollectionSynchronization(FoundedNodes, new object());
        }

        #endregion Constructors

        #region Properties

        public XElement Asset { get; set; }
        public string SearchParameter { get; set; }
        public string TemplateName => Asset?.Element("Template")?.Value ?? Asset?.XPathSelectElement("Standard/Values/Name")?.Value;
        public string Guid => Asset?.XPathSelectElement("Values/Standard/GUID")?.Value;
        public ObservableCollection<XElement> FoundedNodes { get; set; } = new ObservableCollection<XElement>();

        #endregion Properties

        #region Methods

        public void Add(SearchResult a) {
            foreach (var item in a.FoundedNodes) {
                if (!FoundedNodes.Contains(item)) {
                    FoundedNodes.Add(item);
                }
            }
        }

        #endregion Methods
    }
}