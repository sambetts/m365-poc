import '../../App.css';
import { Message } from '@microsoft/microsoft-graph-types';
import { useState } from 'react';
import { ExampleAppGraphLoader } from '../../services/ExampleAppGraphLoader';

function Emails(props: { messages: Message[], loader: ExampleAppGraphLoader }) {
  const [selectedEmail, setSelectedEmail] = useState<Message | null>(null);
  const [loadingEmail, setLoadingEmail] = useState<boolean>(false);

  const handleEmailClick = async (message: Message) => {
    if (!message.id) return;
    
    setLoadingEmail(true);
    try {
      const fullEmail = await props.loader.loadFullEmail(message.id);
      console.log('Full email loaded:', fullEmail);
      console.log('Email body:', fullEmail.body);
      setSelectedEmail(fullEmail);
    } catch (error) {
      console.error('Error loading full email:', error);
      // Fallback to the partial message data
      setSelectedEmail(message);
    } finally {
      setLoadingEmail(false);
    }
  };

  const formatDate = (dateString: string | null | undefined) => {
    if (!dateString) return 'Unknown date';
    const date = new Date(dateString);
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  };

  const getEmailPreview = (body: string | undefined, maxLength: number = 100) => {
    if (!body) return '';
    const plainText = body.replace(/<[^>]*>/g, '');
    return plainText.length > maxLength 
      ? plainText.substring(0, maxLength) + '...' 
      : plainText;
  };

  return (
    <div style={{ display: 'flex', gap: '1rem', height: '600px' }}>
      {/* Email List */}
      <div style={{ 
        flex: selectedEmail ? '0 0 400px' : '1',
        overflowY: 'auto',
        borderRight: selectedEmail ? '1px solid #e0e0e0' : 'none',
        paddingRight: selectedEmail ? '1rem' : '0',
        transition: 'flex 0.3s ease'
      }}>
        <ul style={{ margin: 0 }}>
          {props.messages.map((m: Message, index: number) => {
            const isSelected = selectedEmail?.id === m.id;
            return (
              <li key={m.id || index} style={{ marginBottom: '0.5rem' }}>
                <button 
                  type="button" 
                  onClick={() => handleEmailClick(m)}
                  disabled={loadingEmail}
                  style={{ 
                    background: isSelected ? '#f0f6ff' : '#fff',
                    border: isSelected ? '1px solid #0078d4' : '1px solid #e0e0e0',
                    borderRadius: '8px',
                    padding: '1rem',
                    font: 'inherit',
                    cursor: 'pointer',
                    textAlign: 'left',
                    width: '100%',
                    transition: 'all 0.2s ease',
                    boxShadow: isSelected ? '0 2px 8px rgba(0, 120, 212, 0.15)' : '0 1px 3px rgba(0, 0, 0, 0.05)'
                  }}
                  onMouseEnter={(e) => {
                    if (!isSelected) {
                      e.currentTarget.style.borderColor = '#ccc';
                      e.currentTarget.style.boxShadow = '0 2px 6px rgba(0, 0, 0, 0.1)';
                    }
                  }}
                  onMouseLeave={(e) => {
                    if (!isSelected) {
                      e.currentTarget.style.borderColor = '#e0e0e0';
                      e.currentTarget.style.boxShadow = '0 1px 3px rgba(0, 0, 0, 0.05)';
                    }
                  }}
                >
                  <div style={{ 
                    display: 'flex', 
                    alignItems: 'center', 
                    marginBottom: '0.5rem',
                    gap: '0.5rem'
                  }}>
                    {m.isRead === false && (
                      <span style={{
                        width: '8px',
                        height: '8px',
                        borderRadius: '50%',
                        backgroundColor: '#0078d4',
                        flexShrink: 0
                      }}></span>
                    )}
                    <strong style={{ 
                      fontSize: '0.95rem',
                      fontWeight: m.isRead === false ? '600' : '500',
                      color: '#333',
                      flex: 1,
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap'
                    }}>
                      {m.subject || '(No subject)'}
                    </strong>
                  </div>
                  <div style={{ 
                    fontSize: '0.85rem', 
                    color: '#666',
                    marginBottom: '0.25rem'
                  }}>
                    {m.sender?.emailAddress?.name || 'Unknown sender'}
                  </div>
                  <div style={{ 
                    fontSize: '0.75rem', 
                    color: '#999',
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center'
                  }}>
                    <span>{formatDate(m.sentDateTime)}</span>
                    {m.hasAttachments && (
                      <span style={{ 
                        fontSize: '1rem',
                        color: '#666'
                      }}>📎</span>
                    )}
                  </div>
                  {!selectedEmail && m.bodyPreview && (
                    <div style={{ 
                      fontSize: '0.8rem', 
                      color: '#888',
                      marginTop: '0.5rem',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap'
                    }}>
                      {getEmailPreview(m.bodyPreview, 80)}
                    </div>
                  )}
                </button>
              </li>
            );
          })}
        </ul>
      </div>

      {/* Email Detail View */}
      {selectedEmail && (
        <div style={{ 
          flex: 1,
          overflowY: 'auto',
          backgroundColor: '#fff',
          borderRadius: '8px',
          border: '1px solid #e0e0e0',
          padding: '1.5rem',
          animation: 'slideIn 0.3s ease'
        }}>
          {loadingEmail ? (
            <div style={{ 
              display: 'flex', 
              justifyContent: 'center', 
              alignItems: 'center', 
              height: '100%',
              color: '#666'
            }}>
              <p>Loading email content...</p>
            </div>
          ) : (
            <>
              <div style={{ marginBottom: '1.5rem' }}>
            <div style={{ 
              display: 'flex', 
              justifyContent: 'space-between',
              alignItems: 'flex-start',
              marginBottom: '1rem'
            }}>
              <h2 style={{ 
                margin: 0,
                fontSize: '1.5rem',
                fontWeight: '600',
                color: '#333',
                flex: 1,
                paddingRight: '1rem'
              }}>
                {selectedEmail.subject || '(No subject)'}
              </h2>
              <button
                onClick={() => setSelectedEmail(null)}
                style={{
                  background: 'none',
                  border: 'none',
                  fontSize: '1.5rem',
                  cursor: 'pointer',
                  color: '#666',
                  padding: '0.25rem',
                  lineHeight: 1,
                  flexShrink: 0
                }}
                title="Close"
              >
                ×
              </button>
            </div>
            
            <div style={{ 
              padding: '1rem',
              backgroundColor: '#f8f9fa',
              borderRadius: '6px',
              marginBottom: '1rem'
            }}>
              <div style={{ 
                display: 'flex',
                alignItems: 'center',
                marginBottom: '0.75rem',
                gap: '0.75rem'
              }}>
                <div style={{
                  width: '40px',
                  height: '40px',
                  borderRadius: '50%',
                  backgroundColor: '#0078d4',
                  color: '#fff',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: '1.2rem',
                  fontWeight: '600',
                  flexShrink: 0
                }}>
                  {(selectedEmail.sender?.emailAddress?.name || 'U').charAt(0).toUpperCase()}
                </div>
                <div style={{ flex: 1 }}>
                  <div style={{ 
                    fontWeight: '600',
                    color: '#333',
                    marginBottom: '0.125rem'
                  }}>
                    {selectedEmail.sender?.emailAddress?.name || 'Unknown sender'}
                  </div>
                  <div style={{ 
                    fontSize: '0.85rem',
                    color: '#666'
                  }}>
                    {selectedEmail.sender?.emailAddress?.address}
                  </div>
                </div>
              </div>
              
              <div style={{ 
                fontSize: '0.85rem',
                color: '#666',
                display: 'grid',
                gap: '0.25rem'
              }}>
                {selectedEmail.toRecipients && selectedEmail.toRecipients.length > 0 && (
                  <div>
                    <strong>To:</strong> {selectedEmail.toRecipients.map(r => r.emailAddress?.name || r.emailAddress?.address).join(', ')}
                  </div>
                )}
                {selectedEmail.ccRecipients && selectedEmail.ccRecipients.length > 0 && (
                  <div>
                    <strong>Cc:</strong> {selectedEmail.ccRecipients.map(r => r.emailAddress?.name || r.emailAddress?.address).join(', ')}
                  </div>
                )}
                <div>
                  <strong>Date:</strong> {formatDate(selectedEmail.sentDateTime)}
                </div>
                {selectedEmail.hasAttachments && (
                  <div>
                    <strong>Attachments:</strong> 📎 {selectedEmail.hasAttachments ? 'Yes' : 'No'}
                  </div>
                )}
              </div>
            </div>
          </div>

          <div style={{ 
            borderTop: '1px solid #e0e0e0',
            paddingTop: '1.5rem',
            lineHeight: '1.6',
            color: '#333'
          }}>
            {selectedEmail.body?.content ? (
              selectedEmail.body.contentType === 'html' ? (
                <div 
                  dangerouslySetInnerHTML={{ __html: selectedEmail.body.content }}
                  style={{
                    fontFamily: 'inherit',
                    fontSize: '0.95rem'
                  }}
                />
              ) : (
                <pre style={{ 
                  whiteSpace: 'pre-wrap',
                  fontFamily: 'inherit',
                  fontSize: '0.95rem',
                  margin: 0
                }}>
                  {selectedEmail.body.content}
                </pre>
              )
            ) : selectedEmail.bodyPreview ? (
              <div style={{ 
                fontFamily: 'inherit',
                fontSize: '0.95rem',
                color: '#666',
                fontStyle: 'italic'
              }}>
                <p><strong>Preview only (full content not available):</strong></p>
                <p>{selectedEmail.bodyPreview}</p>
              </div>
            ) : (
              <div style={{ 
                fontFamily: 'inherit',
                fontSize: '0.95rem',
                color: '#999',
                fontStyle: 'italic',
                padding: '2rem',
                textAlign: 'center'
              }}>
                No content available
                <div style={{ fontSize: '0.8rem', marginTop: '0.5rem' }}>
                  (Body type: {selectedEmail.body?.contentType || 'unknown'})
                </div>
              </div>
            )}
          </div>
            </>
          )}
        </div>
      )}
    </div>
  );
}

export default Emails;
