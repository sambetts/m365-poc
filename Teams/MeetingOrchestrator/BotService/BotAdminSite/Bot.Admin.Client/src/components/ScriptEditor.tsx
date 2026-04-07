import { useEffect, useState } from 'react';
import { useMsal } from '@azure/msal-react';
import { getScript, createScript, updateScript } from '../api/scriptsApi';
import type { ParagraphDto, ScriptDto } from '../types';

interface Props {
  scriptId: string | null; // null = create new
  onSaved: () => void;
  onCancel: () => void;
}

const emptyParagraph = (): ParagraphDto => ({
  text: '',
  language: undefined,
  pauseBeforeSeconds: 0,
  pauseAfterSeconds: 0,
});

export default function ScriptEditor({ scriptId, onSaved, onCancel }: Props) {
  const { instance } = useMsal();
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [defaultLanguage, setDefaultLanguage] = useState('en-US');
  const [paragraphs, setParagraphs] = useState<ParagraphDto[]>([emptyParagraph()]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (scriptId) {
      getScript(instance, scriptId).then((s) => {
        setName(s.name);
        setDescription(s.description ?? '');
        setDefaultLanguage(s.defaultLanguage);
        setParagraphs(s.paragraphs.length ? s.paragraphs : [emptyParagraph()]);
      });
    }
  }, [scriptId]); // eslint-disable-line react-hooks/exhaustive-deps

  const updateParagraph = (index: number, field: keyof ParagraphDto, value: string | number) => {
    setParagraphs((prev) =>
      prev.map((p, i) => (i === index ? { ...p, [field]: value } : p))
    );
  };

  const addParagraph = () => setParagraphs((prev) => [...prev, emptyParagraph()]);

  const removeParagraph = (index: number) =>
    setParagraphs((prev) => prev.filter((_, i) => i !== index));

  const moveParagraph = (index: number, direction: -1 | 1) => {
    const newIndex = index + direction;
    if (newIndex < 0 || newIndex >= paragraphs.length) return;
    const updated = [...paragraphs];
    [updated[index], updated[newIndex]] = [updated[newIndex], updated[index]];
    setParagraphs(updated);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError('');

    const script: Partial<ScriptDto> = {
      name,
      description: description || undefined,
      defaultLanguage,
      paragraphs: paragraphs.filter((p) => p.text.trim()),
    };

    try {
      if (scriptId) {
        await updateScript(instance, scriptId, script);
      } else {
        await createScript(instance, script);
      }
      onSaved();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Save failed');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div>
      <h2>{scriptId ? 'Edit Script' : 'New Script'}</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}

      <form onSubmit={handleSubmit}>
        <div style={fieldRow}>
          <label style={labelStyle}>Name</label>
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            style={inputStyle}
          />
        </div>

        <div style={fieldRow}>
          <label style={labelStyle}>Description</label>
          <input
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            style={inputStyle}
          />
        </div>

        <div style={fieldRow}>
          <label style={labelStyle}>Default Language</label>
          <input
            value={defaultLanguage}
            onChange={(e) => setDefaultLanguage(e.target.value)}
            style={{ ...inputStyle, width: 120 }}
          />
        </div>

        <h3>Paragraphs</h3>
        {paragraphs.map((p, i) => (
          <div
            key={i}
            style={{
              border: '1px solid #ddd',
              borderRadius: 6,
              padding: 12,
              marginBottom: 12,
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 8 }}>
              <strong>#{i + 1}</strong>
              <div>
                <button type="button" onClick={() => moveParagraph(i, -1)} style={smallBtn} disabled={i === 0}>
                  ▲
                </button>
                <button type="button" onClick={() => moveParagraph(i, 1)} style={smallBtn} disabled={i === paragraphs.length - 1}>
                  ▼
                </button>
                <button type="button" onClick={() => removeParagraph(i)} style={{ ...smallBtn, color: 'red' }}>
                  ✕
                </button>
              </div>
            </div>
            <textarea
              value={p.text}
              onChange={(e) => updateParagraph(i, 'text', e.target.value)}
              rows={3}
              style={{ ...inputStyle, width: '100%', resize: 'vertical' }}
              placeholder="Text to speak…"
            />
            <div style={{ display: 'flex', gap: 12, marginTop: 8 }}>
              <div>
                <label style={smallLabel}>Language override</label>
                <input
                  value={p.language ?? ''}
                  onChange={(e) => updateParagraph(i, 'language', e.target.value || '')}
                  style={{ ...inputStyle, width: 100 }}
                  placeholder="e.g. fr-FR"
                />
              </div>
              <div>
                <label style={smallLabel}>Pause before (s)</label>
                <input
                  type="number"
                  min={0}
                  step={0.5}
                  value={p.pauseBeforeSeconds}
                  onChange={(e) => updateParagraph(i, 'pauseBeforeSeconds', parseFloat(e.target.value) || 0)}
                  style={{ ...inputStyle, width: 80 }}
                />
              </div>
              <div>
                <label style={smallLabel}>Pause after (s)</label>
                <input
                  type="number"
                  min={0}
                  step={0.5}
                  value={p.pauseAfterSeconds}
                  onChange={(e) => updateParagraph(i, 'pauseAfterSeconds', parseFloat(e.target.value) || 0)}
                  style={{ ...inputStyle, width: 80 }}
                />
              </div>
            </div>
          </div>
        ))}

        <button type="button" onClick={addParagraph} style={secondaryBtn}>
          + Add Paragraph
        </button>

        <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
          <button type="submit" disabled={saving} style={primaryBtn}>
            {saving ? 'Saving…' : 'Save'}
          </button>
          <button type="button" onClick={onCancel} style={secondaryBtn}>
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}

const fieldRow: React.CSSProperties = { marginBottom: 12 };
const labelStyle: React.CSSProperties = { display: 'block', fontWeight: 600, marginBottom: 4 };
const smallLabel: React.CSSProperties = { display: 'block', fontSize: '0.8rem', marginBottom: 2 };
const inputStyle: React.CSSProperties = {
  padding: '6px 10px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: '0.95rem',
};
const primaryBtn: React.CSSProperties = {
  background: '#6264a7',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  padding: '8px 20px',
  cursor: 'pointer',
  fontWeight: 600,
};
const secondaryBtn: React.CSSProperties = {
  background: '#eee',
  color: '#333',
  border: '1px solid #ccc',
  borderRadius: 4,
  padding: '8px 20px',
  cursor: 'pointer',
  fontWeight: 600,
};
const smallBtn: React.CSSProperties = {
  background: 'none',
  border: 'none',
  cursor: 'pointer',
  fontSize: '1rem',
  marginLeft: 4,
};
