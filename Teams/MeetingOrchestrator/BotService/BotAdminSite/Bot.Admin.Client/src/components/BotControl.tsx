import { useEffect, useState } from 'react';
import { useMsal } from '@azure/msal-react';
import { joinCall, startScript } from '../api/botApi';
import { getScripts } from '../api/scriptsApi';
import type { JoinCallResponse, ScriptDto } from '../types';

export default function BotControl() {
  const { instance } = useMsal();

  // Join call state
  const [joinUrl, setJoinUrl] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [joining, setJoining] = useState(false);
  const [joinResult, setJoinResult] = useState<JoinCallResponse | null>(null);

  // Start script state
  const [callId, setCallId] = useState('');
  const [botName, setBotName] = useState('');
  const [selectedScriptId, setSelectedScriptId] = useState('');
  const [scripts, setScripts] = useState<ScriptDto[]>([]);
  const [starting, setStarting] = useState(false);

  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    getScripts(instance)
      .then(setScripts)
      .catch(() => {});
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Auto-fill callId and botName when join succeeds
  useEffect(() => {
    if (joinResult?.callId) {
      setCallId(String(joinResult.callId));
    }
  }, [joinResult]);

  useEffect(() => {
    if (displayName) {
      setBotName(displayName);
    }
  }, [displayName]);

  const handleJoin = async (e: React.FormEvent) => {
    e.preventDefault();
    setJoining(true);
    setError('');
    setSuccess('');
    setJoinResult(null);
    try {
      const result = await joinCall(instance, {
        joinUrl,
        displayName: displayName || undefined,
      });
      setJoinResult(result);
      setSuccess(`Bot joined! Call ID: ${result.callId}`);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Join failed');
    } finally {
      setJoining(false);
    }
  };

  const handleStartScript = async (e: React.FormEvent) => {
    e.preventDefault();
    setStarting(true);
    setError('');
    setSuccess('');
    try {
      await startScript(instance, {
        callId,
        displayName: botName,
        scriptId: selectedScriptId,
      });
      setSuccess('Script started successfully!');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Start script failed');
    } finally {
      setStarting(false);
    }
  };

  return (
    <div>
      <h2>Bot Control</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      {success && <p style={{ color: 'green' }}>{success}</p>}

      {/* --- Join Call --- */}
      <div style={sectionStyle}>
        <h3>1. Join a Teams Meeting</h3>
        <form onSubmit={handleJoin}>
          <div style={fieldRow}>
            <label style={labelStyle}>Teams Meeting Join URL</label>
            <input
              value={joinUrl}
              onChange={(e) => setJoinUrl(e.target.value)}
              required
              style={{ ...inputStyle, width: '100%' }}
              placeholder="https://teams.microsoft.com/l/meetup-join/..."
            />
          </div>
          <div style={fieldRow}>
            <label style={labelStyle}>Display Name (optional)</label>
            <input
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              style={inputStyle}
              placeholder="Bot Speaker"
            />
          </div>
          <button type="submit" disabled={joining} style={primaryBtn}>
            {joining ? 'Joining…' : 'Join Call'}
          </button>
        </form>

        {joinResult && (
          <div style={{ marginTop: 12, padding: 12, background: '#f0f8f0', borderRadius: 6 }}>
            <strong>Call ID:</strong> {String(joinResult.callId)}
            <br />
            <strong>Scenario ID:</strong> {joinResult.scenarioId}
          </div>
        )}
      </div>

      {/* --- Start Script --- */}
      <div style={sectionStyle}>
        <h3>2. Start a Script</h3>
        <form onSubmit={handleStartScript}>
          <div style={fieldRow}>
            <label style={labelStyle}>Call ID</label>
            <input
              value={callId}
              onChange={(e) => setCallId(e.target.value)}
              required
              style={inputStyle}
              placeholder="From step 1 or paste manually"
            />
          </div>
          <div style={fieldRow}>
            <label style={labelStyle}>Bot Display Name</label>
            <input
              value={botName}
              onChange={(e) => setBotName(e.target.value)}
              required
              style={inputStyle}
              placeholder="Must match the name used when joining"
            />
          </div>
          <div style={fieldRow}>
            <label style={labelStyle}>Script</label>
            <select
              value={selectedScriptId}
              onChange={(e) => setSelectedScriptId(e.target.value)}
              required
              style={inputStyle}
            >
              <option value="">-- Select a script --</option>
              {scripts.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.paragraphs.length} paragraphs)
                </option>
              ))}
            </select>
          </div>
          <button type="submit" disabled={starting || !selectedScriptId} style={primaryBtn}>
            {starting ? 'Starting…' : 'Start Script'}
          </button>
        </form>
      </div>
    </div>
  );
}

const sectionStyle: React.CSSProperties = {
  border: '1px solid #ddd',
  borderRadius: 8,
  padding: 20,
  marginBottom: 20,
};
const fieldRow: React.CSSProperties = { marginBottom: 12 };
const labelStyle: React.CSSProperties = { display: 'block', fontWeight: 600, marginBottom: 4 };
const inputStyle: React.CSSProperties = {
  padding: '6px 10px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: '0.95rem',
  minWidth: 250,
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
