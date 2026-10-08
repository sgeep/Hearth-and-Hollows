using UnityEngine;

namespace Hearthdelve.UI.Localization
{
    /// <summary>
    /// Sets a Super Text Mesh's text from a string table (the UI table unless it's given another, like Credits), and again when
    /// the tables finish loading. Prefabs hold the key, never literal text.
    /// </summary>
    [RequireComponent(typeof(SuperTextMesh))]
    public sealed class LocalizedSuperText : MonoBehaviour
    {
        [SerializeField] string m_Key;
        [SerializeField] string m_Table;

        SuperTextMesh m_Text;
        object[] m_Args;

        public string Key => m_Key;

        public string Table => string.IsNullOrEmpty(m_Table) ? Loc.UITable : m_Table;

        public void Configure(string key) => m_Key = key;

        /// <summary>A key in another table (the Credits table, 4i-C).</summary>
        public void Configure(string table, string key)
        {
            m_Table = table;
            m_Key = key;
        }

        /// <summary>Changes the key and format arguments, and refreshes the text.</summary>
        public void Set(string key, params object[] args)
        {
            m_Key = key;
            m_Args = args;
            Refresh();
        }

        void Awake() => m_Text = GetComponent<SuperTextMesh>();

        void OnEnable()
        {
            Loc.Ready += Refresh;
            Refresh();
        }

        void OnDisable() => Loc.Ready -= Refresh;

        void Refresh()
        {
            if (m_Text == null) m_Text = GetComponent<SuperTextMesh>();
            if (string.IsNullOrEmpty(m_Key))
            {
                m_Text.text = string.Empty;
                return;
            }
            // Until the tables are loaded there is nothing to show (and nothing to block on).
            m_Text.text = Loc.IsReady || Application.platform != RuntimePlatform.WebGLPlayer ? Loc.Get(Table, m_Key, m_Args) : string.Empty;
        }
    }
}
