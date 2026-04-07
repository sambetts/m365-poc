import { useState } from 'react';
import Layout from './components/Layout';
import ScriptList from './components/ScriptList';
import ScriptEditor from './components/ScriptEditor';
import BotControl from './components/BotControl';
import './App.css';

type View = 'list' | 'editor' | 'bot';

export default function App() {
  const [view, setView] = useState<View>('list');
  const [editingScriptId, setEditingScriptId] = useState<string | null>(null);

  const goToList = () => {
    setEditingScriptId(null);
    setView('list');
  };

  return (
    <Layout>
      <nav style={{ marginBottom: 20, display: 'flex', gap: 12 }}>
        <button onClick={goToList} style={navBtn(view === 'list' || view === 'editor')}>
          Scripts
        </button>
        <button onClick={() => setView('bot')} style={navBtn(view === 'bot')}>
          Bot Control
        </button>
      </nav>

      {view === 'list' && (
        <ScriptList
          onEdit={(id) => {
            setEditingScriptId(id);
            setView('editor');
          }}
          onNew={() => {
            setEditingScriptId(null);
            setView('editor');
          }}
        />
      )}

      {view === 'editor' && (
        <ScriptEditor
          scriptId={editingScriptId}
          onSaved={goToList}
          onCancel={goToList}
        />
      )}

      {view === 'bot' && <BotControl />}
    </Layout>
  );
}

function navBtn(active: boolean): React.CSSProperties {
  return {
    background: active ? '#6264a7' : '#eee',
    color: active ? '#fff' : '#333',
    border: 'none',
    borderRadius: 4,
    padding: '8px 20px',
    cursor: 'pointer',
    fontWeight: 600,
  };
}
