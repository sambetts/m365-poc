import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import { Link } from 'react-router-dom';

interface Subscription {
  id: string;
  resource: string;
  changeType: string;
  expirationDateTime?: string;
  notificationUrl?: string;
  clientState?: string;
}

export default function SubscriptionsPage() {
  const [subs, setSubs] = useState<Subscription[]>([]);
  const [loading, setLoading] = useState(false);
  const [creating, setCreating] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [upn, setUpn] = useState('');
  const [changeType, setChangeType] = useState('created,updated,deleted');
  const [notificationUrl, setNotificationUrl] = useState<string>('');

  useEffect(() => {
    // default notification URL to current origin (assuming backend served same host)
    setNotificationUrl(`${window.location.origin}/api/notifications`);
  }, []);

  const load = async () => {
    try {
      setLoading(true);
      const res = await fetch('/api/subscriptions');
      if (!res.ok) throw new Error(await res.text());
      const data = await res.json();
      setSubs(data);
    } catch (e:any) {
      toast.error('FAILED TO LOAD', { description: e.message });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const create = async () => {
    if (!upn) { toast.error('UPN REQUIRED'); return; }
    try {
      setCreating(true);
      const res = await fetch('/api/subscriptions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ upn, changeType, notificationUrl })
      });
      if (!res.ok) throw new Error(await res.text());
      toast.success('SUBSCRIPTION CREATED');
      setUpn('');
      await load();
    } catch (e:any) {
      toast.error('CREATE FAILED', { description: e.message });
    } finally {
      setCreating(false);
    }
  };

  const deleteSub = async (id: string) => {
    if (!confirm('Delete subscription?')) return;
    try {
      setDeletingId(id);
      const res = await fetch(`/api/subscriptions/${id}`, { method: 'DELETE' });
      if (!res.ok) throw new Error(await res.text());
      toast.success('DELETED');
      setSubs(prev => prev.filter(s => s.id !== id));
    } catch (e:any) {
      toast.error('DELETE FAILED', { description: e.message });
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <div className='min-h-screen p-6 space-y-6'>
      <header className='flex items-center justify-between'>
        <h1 className='text-xl font-bold'>Calendar Webhook Subscriptions</h1>
        <Link to='/' className='text-sm underline'>Back</Link>
      </header>

      <div className='border p-4 space-y-4 max-w-xl bg-white text-black dark:bg-neutral-900 dark:text-white'>
        <h2 className='font-semibold text-sm'>Create Subscription</h2>
        <div className='space-y-2'>
          <div>
            <label className='block text-xs'>User UPN</label>
            <input className='border px-2 py-1 w-full text-sm bg-white text-black dark:bg-neutral-800 dark:text-white placeholder:text-neutral-400' value={upn} onChange={e=>setUpn(e.target.value)} placeholder='user@contoso.com' />
          </div>
          <div>
            <label className='block text-xs'>Change Type (comma separated)</label>
            <input className='border px-2 py-1 w-full text-sm bg-white text-black dark:bg-neutral-800 dark:text-white placeholder:text-neutral-400' value={changeType} onChange={e=>setChangeType(e.target.value)} placeholder='created,updated,deleted' />
          </div>
          <div>
            <label className='block text-xs'>Notification URL</label>
            <input className='border px-2 py-1 w-full text-sm bg-white text-black dark:bg-neutral-800 dark:text-white placeholder:text-neutral-400' value={notificationUrl} onChange={e=>setNotificationUrl(e.target.value)} />
          </div>
          <button disabled={creating} onClick={create} className='bg-blue-600 text-white text-xs px-3 py-1 disabled:opacity-50'>
            {creating ? 'Creating...' : 'Create'}
          </button>
        </div>
      </div>

      <div className='space-y-2'>
        <h2 className='font-semibold text-sm'>Existing Subscriptions</h2>
        <div className='flex gap-2 items-center text-xs'>
          <button onClick={load} disabled={loading} className='border px-2 py-1 disabled:opacity-50'>Refresh</button>
          {loading && <span>Loading...</span>}
        </div>
        {!loading && subs.length === 0 && <div className='text-xs'>No subscriptions.</div>}
        <div className='overflow-x-auto border bg-white text-black dark:bg-neutral-900 dark:text-white'>
          <table className='text-xs min-w-full'>
            <thead>
              <tr className='bg-gray-100 dark:bg-neutral-800'>
                <th className='p-2 border'>Id</th>
                <th className='p-2 border'>Resource</th>
                <th className='p-2 border'>ChangeType</th>
                <th className='p-2 border'>Expires</th>
                <th className='p-2 border'>Actions</th>
              </tr>
            </thead>
            <tbody>
              {subs.map((s, idx) => (
                <tr key={s.id} className={idx % 2 === 0 ? 'bg-white dark:bg-neutral-900' : 'bg-gray-50 dark:bg-neutral-800'}>
                  <td className='p-2 border'>{s.id}</td>
                  <td className='p-2 border'>{s.resource}</td>
                  <td className='p-2 border'>{s.changeType}</td>
                  <td className='p-2 border'>{s.expirationDateTime ? new Date(s.expirationDateTime).toLocaleString() : ''}</td>
                  <td className='p-2 border'>
                    <button onClick={() => deleteSub(s.id)} disabled={deletingId === s.id} className='text-red-600 hover:underline disabled:opacity-50'>
                      {deletingId === s.id ? 'Deleting...' : 'Delete'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
