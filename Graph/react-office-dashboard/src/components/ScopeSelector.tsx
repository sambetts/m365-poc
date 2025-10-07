import React, { useState } from 'react';
import { availableScopes, defaultScopes } from '../authConfig';
import { IPublicClientApplication } from '@azure/msal-browser';

interface ScopeSelectorProps {
  onScopesSelected: (scopes: string[]) => void;
  msalInstance: IPublicClientApplication;
}

export const ScopeSelector: React.FC<ScopeSelectorProps> = ({ onScopesSelected, msalInstance }) => {
  const [selectedScopes, setSelectedScopes] = useState<string[]>(defaultScopes);

  const handleScopeToggle = (scopeValue: string) => {
    setSelectedScopes(prev => {
      if (prev.includes(scopeValue)) {
        return prev.filter(s => s !== scopeValue);
      } else {
        return [...prev, scopeValue];
      }
    });
  };

  const handleConfirm = () => {
    if (selectedScopes.length === 0) {
      alert('Please select at least one scope');
      return;
    }
    
    // Save the selected scopes
    onScopesSelected(selectedScopes);
    
    // Trigger login immediately with the selected scopes
    const loginRequest = {
      scopes: selectedScopes
    };
    
    msalInstance.loginPopup(loginRequest).catch((e: Error) => {
      console.error('Login failed:', e);
    });
  };

  return (
    <div style={{ padding: '20px', maxWidth: '600px', margin: '0 auto' }}>
      <h3>Select Permissions</h3>
      <p style={{ marginBottom: '20px', color: '#666' }}>
        Choose the permissions you want to grant to this application:
      </p>
      
      <div style={{ marginBottom: '20px' }}>
        {availableScopes.map(scope => (
          <div key={scope.value} style={{ marginBottom: '10px', padding: '10px', border: '1px solid #ddd', borderRadius: '4px' }}>
            <label style={{ display: 'flex', alignItems: 'flex-start', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={selectedScopes.includes(scope.value)}
                onChange={() => handleScopeToggle(scope.value)}
                style={{ marginRight: '10px', marginTop: '4px' }}
              />
              <div>
                <div style={{ fontWeight: 'bold' }}>{scope.label}</div>
                <div style={{ fontSize: '0.9em', color: '#666' }}>{scope.description}</div>
                <div style={{ fontSize: '0.8em', color: '#999', marginTop: '4px' }}>Scope: {scope.value}</div>
              </div>
            </label>
          </div>
        ))}
      </div>

      <div style={{ marginTop: '20px' }}>
        <button 
          onClick={handleConfirm}
          style={{ 
            padding: '10px 20px', 
            backgroundColor: '#0078d4', 
            color: 'white', 
            border: 'none', 
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '16px'
          }}
        >
          Sign in with selected permissions
        </button>
      </div>

      <div style={{ marginTop: '20px', padding: '10px', backgroundColor: '#f0f0f0', borderRadius: '4px' }}>
        <strong>Selected scopes:</strong>
        <div style={{ marginTop: '8px', fontFamily: 'monospace', fontSize: '0.9em' }}>
          {selectedScopes.length > 0 ? selectedScopes.join(', ') : 'None selected'}
        </div>
      </div>
    </div>
  );
};
