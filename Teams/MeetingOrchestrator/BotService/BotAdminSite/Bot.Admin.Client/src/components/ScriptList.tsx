import { useEffect, useState } from 'react';
import { useMsal } from '@azure/msal-react';
import { getScripts, deleteScript } from '../api/scriptsApi';
import type { ScriptDto } from '../types';

interface Props {
  onEdit: (id: string) => void;
  onNew: () => void;
}

export default function ScriptList({ onEdit, onNew }: Props) {
  const { instance } = useMsal();
  const [scripts, setScripts] = useState<ScriptDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      setScripts(await getScripts(instance));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to load scripts');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this script?')) return;
    try {
      await deleteScript(instance, id);
      setScripts((prev) => prev.filter((s) => s.id !== id));
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Failed to delete');
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>Scripts</h2>
        <button onClick={onNew} style={primaryBtn}>+ New Script</button>
      </div>

      {error && <p style={{ color: 'red' }}>{error}</p>}
      {loading && <p>Loading…</p>}

      {!loading && scripts.length === 0 && <p>No scripts yet. Create one to get started.</p>}

      <table style={{ width: '100%', borderCollapse: 'collapse', marginTop: 12 }}>
        <thead>
          <tr style={{ borderBottom: '2px solid #ddd', textAlign: 'left' }}>
            <th style={thStyle}>Name</th>
            <th style={thStyle}>Language</th>
            <th style={thStyle}>Paragraphs</th>
            <th style={thStyle}>Updated</th>
            <th style={thStyle}>Actions</th>
          </tr>
        </thead>
        <tbody>
          {scripts.map((s) => (
            <tr key={s.id} style={{ borderBottom: '1px solid #eee' }}>
              <td style={tdStyle}>{s.name}</td>
              <td style={tdStyle}>{s.defaultLanguage}</td>
              <td style={tdStyle}>{s.paragraphs.length}</td>
              <td style={tdStyle}>{new Date(s.updatedAt).toLocaleString()}</td>
              <td style={tdStyle}>
                <button onClick={() => onEdit(s.id)} style={linkBtn}>Edit</button>
                <button onClick={() => handleDelete(s.id)} style={{ ...linkBtn, color: 'red' }}>
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

const primaryBtn: React.CSSProperties = {
  background: '#6264a7',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  padding: '8px 20px',
  cursor: 'pointer',
  fontWeight: 600,
};

const linkBtn: React.CSSProperties = {
  background: 'none',
  border: 'none',
  cursor: 'pointer',
  color: '#6264a7',
  fontWeight: 600,
  marginRight: 8,
};

const thStyle: React.CSSProperties = { padding: '8px 12px' };
const tdStyle: React.CSSProperties = { padding: '8px 12px' };
